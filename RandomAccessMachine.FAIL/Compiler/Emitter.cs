using OneOf;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Helpers;
using RandomAccessMachine.FAIL.Specification;
using System.Text;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Emitter
{
    public static string Emit(Scope scope, Dictionary<Identifier, uint>? registerReservations = null, bool emitEnd = true, string? startOfBlockLabel = null, string? endOfBlockLabel = null)
    {
        var output = new StringBuilder();
        registerReservations ??= [];

        foreach (var statement in scope.Statements)
        {
            _ = output.Append(EmitComment(statement.ToString().Replace("\n", "; ")));

            if (statement is Assignment assignment) _ = output.Append(EmitAssignment(registerReservations, assignment));
            else if (statement is If @if) _ = output.Append(EmitIfElse(registerReservations, @if, startOfBlockLabel, endOfBlockLabel));
            else if (statement is While @while) _ = output.Append(EmitWhileLoop(registerReservations, @while));
            else if (statement is Body body) _ = output.Append(Emit(body.Scope, registerReservations, false, startOfBlockLabel, endOfBlockLabel));
            else if (statement is Break) _ = output.Append(EmitBreak(endOfBlockLabel!));
            else if (statement is Continue) _ = output.Append(EmitContinue(startOfBlockLabel!));
            else _ = output.Append(EmitComment("Unknown statement"));
        }

        if (emitEnd) _ = output.Append(EmitEnd());

        return output.ToString();
    }

    private static string EmitAssignment(Dictionary<Identifier, uint> registerReservations, Assignment assignment)
    {
        var register = GetOrReserveRegister(assignment.Identifier, registerReservations);

        return assignment.Expression.Value.Match(
            number => EmitLoad(number, registerReservations),
            identifier => EmitLoad(identifier, registerReservations),
            binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation)
        ) + EmitStore(register);
    }

    private static string EmitBinaryOperation(Dictionary<Identifier, uint> registerReservations, BinaryOperation operation)
    {
        if (operation.Left.Value.IsT0)
        {
            if (operation.Right.Value.IsT0) return EmitLoad(operation.Left.Value.AsT0, registerReservations) + EmitOperation(operation.Operator, operation.Left.Value.AsT0, operation.Right.Value.AsT0, registerReservations);

            if (operation.Right.Value.IsT1) return EmitLoad(operation.Left.Value.AsT0, registerReservations) + EmitOperation(operation.Operator, operation.Left.Value.AsT0, operation.Right.Value.AsT1, registerReservations);

            var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);

            var right = EmitBinaryOperation(registerReservations, operation.Right.Value.AsT2);
            var operationResult = EmitOperation(operation.Operator, operation.Left.Value.AsT0, Identifier.Temporary, registerReservations);

            FreeRegister(Identifier.Temporary, registerReservations);

            return right + EmitStore(tempRegister) + operationResult;
        }

        if (operation.Left.Value.IsT1)
        {
            if (operation.Right.Value.IsT0) return EmitLoad(operation.Left.Value.AsT1, registerReservations) + EmitOperation(operation.Operator, operation.Left.Value.AsT1, operation.Right.Value.AsT0, registerReservations);

            if (operation.Right.Value.IsT1) return EmitLoad(operation.Left.Value.AsT1, registerReservations) + EmitOperation(operation.Operator, operation.Left.Value.AsT1, operation.Right.Value.AsT1, registerReservations);

            var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);

            var right = EmitBinaryOperation(registerReservations, operation.Right.Value.AsT2);
            var operationResult = EmitOperation(operation.Operator, operation.Left.Value.AsT1, Identifier.Temporary, registerReservations);

            FreeRegister(Identifier.Temporary, registerReservations);

            return right + EmitStore(tempRegister) + operationResult;
        }

        if (operation.Right.Value.IsT0) return EmitBinaryOperation(registerReservations, operation.Left.Value.AsT2) + EmitOperation(operation.Operator, Identifier.Empty, operation.Right.Value.AsT0, registerReservations);

        if (operation.Right.Value.IsT1) return EmitBinaryOperation(registerReservations, operation.Left.Value.AsT2) + EmitOperation(operation.Operator, Identifier.Empty, operation.Right.Value.AsT1, registerReservations);

        {
            var left = EmitBinaryOperation(registerReservations, operation.Left.Value.AsT2);

            var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);

            var right = EmitBinaryOperation(registerReservations, operation.Right.Value.AsT2);
            var operationResult = EmitOperation(operation.Operator, Identifier.Empty, Identifier.Temporary, registerReservations);

            FreeRegister(Identifier.Temporary, registerReservations);

            return left + right + EmitStore(tempRegister) + operationResult;
        }
    }

    private static string EmitIfElse(Dictionary<Identifier, uint> registerReservations, If @if, string? startOfBlockLabel, string? endOfBlockLabel)
    {
        var elseLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var conditionResult = EmitBinaryOperation(registerReservations, @if.Condition.Value.AsT2);
        var conditionJump = EmitJumpIfZero(elseLabel);

        var body = Emit(@if.Body.Scope, registerReservations, false, startOfBlockLabel, endOfBlockLabel);
        var elseBody = @if.ElseBody is not null ? Emit(@if.ElseBody.Scope, registerReservations, false, startOfBlockLabel, endOfBlockLabel) : string.Empty;

        return conditionResult + conditionJump + body + EmitGoto(endLabel) + EmitLabel(elseLabel) + elseBody + EmitLabel(endLabel);
    }

    private static string EmitWhileLoop(Dictionary<Identifier, uint> registerReservations, While @while)
    {
        var startLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var conditionResult = EmitBinaryOperation(registerReservations, @while.Condition.Value.AsT2);
        var conditionJump = EmitJumpIfZero(endLabel);

        var body = Emit(@while.Body.Scope, registerReservations, false, startLabel, endLabel);

        return EmitLabel(startLabel) + conditionResult + conditionJump + body + EmitGoto(startLabel) + EmitLabel(endLabel);
    }
    private static string EmitBreak(string endOfBlockLabel) => EmitGoto(endOfBlockLabel);
    private static string EmitContinue(string startOfBlockLabel) => EmitGoto(startOfBlockLabel);

    private static string EmitLoad(OneOf<Number, Identifier> value, Dictionary<Identifier, uint> registerReservations)
        => value.Match(number => $"LOAD #{number.Value}\n", identifier => $"LOAD {registerReservations[identifier]}\n");

    private static string EmitStore(uint register) => $"STORE {register}\n";

    private static string EmitOperation(BinaryOperator @operator, OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations) => @operator switch
    {
        BinaryOperator.Add => EmitAdd(right, registerReservations),
        BinaryOperator.Subtract => EmitSubtract(right, registerReservations),
        BinaryOperator.Multiply => EmitMultiply(right, registerReservations),
        BinaryOperator.Divide => EmitDivide(right, registerReservations),
        BinaryOperator.Equal => EmitEqual(left, right, registerReservations),
        BinaryOperator.NotEqual => EmitNotEqual(left, right, registerReservations),
        BinaryOperator.GreaterThan => EmitGreaterThan(right, registerReservations),
        BinaryOperator.LessThan => EmitLessThan(left, right, registerReservations),
        BinaryOperator.GreaterThanOrEqual => EmitGreaterThanOrEqual(left, right, registerReservations),
        BinaryOperator.LessThanOrEqual => EmitLessThanOrEqual(left, right, registerReservations),
        _ => string.Empty
    };

    private static string EmitAdd(OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
        => right.Match(number => $"ADD #{number.Value}\n", identifier => $"ADD {registerReservations[identifier]}\n");
    private static string EmitSubtract(OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
        => right.Match(number => $"SUB #{number.Value}\n", identifier => $"SUB {registerReservations[identifier]}\n");
    private static string EmitMultiply(OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
        => right.Match(number => $"MUL #{number.Value}\n", identifier => $"MUL {registerReservations[identifier]}\n");
    private static string EmitDivide(OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
        => right.Match(number => $"DIV #{number.Value}\n", identifier => $"DIV {registerReservations[identifier]}\n");
    private static string EmitEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var checkFailedLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var subtractRightFromLeft = EmitSubtract(right, registerReservations);
        var subtractLeftFromRight = EmitLoad(right, registerReservations) + EmitSubtract(left, registerReservations);

        return subtractRightFromLeft + EmitJumpIfNotZero(checkFailedLabel) + subtractLeftFromRight + EmitJumpIfNotZero(checkFailedLabel) + EmitLoad(new Number(1), registerReservations) + EmitGoto(endLabel) + EmitLabel(checkFailedLabel) + EmitLoad(new Number(0), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitNotEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var equalLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var equal = EmitEqual(left, right, registerReservations);

        return equal + EmitJumpIfNotZero(equalLabel) + EmitLoad(new Number(1), registerReservations) + EmitGoto(endLabel) + EmitLabel(equalLabel) + EmitLoad(new Number(0), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitGreaterThan(OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var endLabel = Guid.NewGuid().ToLabelString();

        var subtractRightFromLeft = EmitSubtract(right, registerReservations);

        return subtractRightFromLeft + EmitJumpIfZero(endLabel) + EmitLoad(new Number(1), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitLessThan(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var equalLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var greaterThan = EmitLoad(right, registerReservations) + EmitGreaterThan(left, registerReservations);
        var equal = EmitEqual(left, right, registerReservations);

        return greaterThan + EmitJumpIfZero(endLabel) + equal + EmitJumpIfNotZero(equalLabel) + EmitLoad(new Number(1), registerReservations) + EmitGoto(endLabel) + EmitLabel(equalLabel) + EmitLoad(new Number(0), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitGreaterThanOrEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var lessThanLabel = Guid.NewGuid().ToLabelString();
        var successLabel = Guid.NewGuid().ToLabelString();

        var lessThan = EmitLessThan(left, right, registerReservations);

        return lessThan + EmitJumpIfNotZero(lessThanLabel) + EmitLoad(new Number(1), registerReservations) + EmitGoto(successLabel) + EmitLabel(lessThanLabel) + EmitLoad(new Number(0), registerReservations) + EmitLabel(successLabel);
    }
    private static string EmitLessThanOrEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations)
    {
        var greaterThanLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var greaterThan = EmitGreaterThan(right, registerReservations);

        return greaterThan + EmitJumpIfNotZero(greaterThanLabel) + EmitLoad(new Number(1), registerReservations) + EmitGoto(endLabel) + EmitLabel(greaterThanLabel) + EmitLoad(new Number(0), registerReservations) + EmitLabel(endLabel);
    }

    private static string EmitEnd() => "END\n";

    private static string EmitComment(string comment) => $"// {comment}\n";

    private static string EmitLabel(string label) => $"{label}:\n";

    private static string EmitGoto(string label) => $"GOTO {label}\n";
    private static string EmitJumpIfZero(string label) => $"JZERO {label}\n";
    private static string EmitJumpIfNotZero(string label) => $"JNZERO {label}\n";

    private static uint ReserveRegister(Identifier identifier, Dictionary<Identifier, uint> registerReservations)
    {
        var register = (uint)registerReservations.Count + 1;
        registerReservations[identifier] = register;
        return register;
    }
    private static uint GetOrReserveRegister(Identifier identifier, Dictionary<Identifier, uint> registerReservations)
        => registerReservations.TryGetValue(identifier, out var register) ? register : ReserveRegister(identifier, registerReservations);
    private static void FreeRegister(Identifier identifier, Dictionary<Identifier, uint> registerReservations) => registerReservations.Remove(identifier);
}
