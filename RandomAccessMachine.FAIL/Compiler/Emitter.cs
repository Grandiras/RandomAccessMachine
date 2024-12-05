using OneOf;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Helpers;
using RandomAccessMachine.FAIL.Specification;
using RandomAccessMachine.FAIL.Specification.Operators;
using System.Text;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Emitter
{
    public static string Emit(Scope scope, Dictionary<string, uint>? registerReservations = null, bool emitEnd = true, string? startOfBlockLabel = null, string? endOfBlockLabel = null)
    {
        var output = new StringBuilder();
        registerReservations ??= [];

        foreach (var statement in scope.Statements.Where(x => x is not FunctionDeclaration)) _ = output.Append(EmitStatement(statement, registerReservations, startOfBlockLabel, endOfBlockLabel));

        if (emitEnd) _ = output.Append(EmitEnd());

        foreach (var register in scope.Where(x => x is Assignment assignment && assignment.IsInitial).Select(x => (Assignment)x))
        {
            if (register.Identifier.IsT0) _ = registerReservations.Remove(register.Identifier.AsT0.Name);
            else if (register.Identifier.IsT1)
            {
                for (var i = 0; i < register.Expression.Value.AsT4.Type.Name.AsT0.Size; i++)
                {
                    var tempIdentifier = new Identifier($"{register.Identifier.AsT1.Identifier.Name}{i}", default);
                    _ = registerReservations.Remove(tempIdentifier.Name);
                }
            }
        }

        return output.ToString();
    }

    private static string EmitStatement(Statement statement, Dictionary<string, uint> registerReservations, string? startOfBlockLabel = null, string? endOfBlockLabel = null)
    {
        var output = new StringBuilder();

        _ = output.Append(EmitComment(statement.ToString()!.Replace("\n", "; ")));

        if (statement is Assignment assignment) _ = output.Append(EmitAssignment(registerReservations, assignment));
        else if (statement is If @if) _ = output.Append(EmitIfElse(registerReservations, @if, startOfBlockLabel, endOfBlockLabel));
        else if (statement is While @while) _ = output.Append(EmitWhileLoop(registerReservations, @while));
        else if (statement is Body body) _ = output.Append(Emit(body.Scope, registerReservations, false, startOfBlockLabel, endOfBlockLabel));
        else if (statement is Break) _ = output.Append(EmitBreak(endOfBlockLabel!));
        else if (statement is Continue) _ = output.Append(EmitContinue(startOfBlockLabel!));
        else if (statement is Return @return) _ = output.Append(EmitReturn(registerReservations, @return));
        else if (statement is FunctionCall functionCall) _ = output.Append(EmitFunctionCall(registerReservations, functionCall));
        else if (statement is Expression expression && expression.Value.IsT5) _ = output.Append(EmitFunctionCall(registerReservations, expression.Value.AsT5));
        else _ = output.Append(EmitComment("Unknown statement"));

        return output.ToString();
    }

    private static string EmitAssignment(Dictionary<string, uint> registerReservations, Assignment assignment)
    {
        var expression = assignment.Expression.Value.Match(
            number => EmitLoad(number, registerReservations),
            identifier => EmitLoad(identifier, registerReservations),
            binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation),
            arrayAccessor => EmitArrayAccessor(registerReservations, arrayAccessor),
            typeInitialization => EmitTypeInitialization(registerReservations, typeInitialization, assignment.Identifier.AsT0),
            functionCall => EmitFunctionCall(registerReservations, functionCall)
        );

        if (assignment.Identifier.IsT1)
        {
            var index = assignment.Identifier.AsT1.Index.Value.Match(
                number => EmitLoad(number, registerReservations),
                identifier => EmitLoad(identifier, registerReservations),
                binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation),
                arrayAccessor => EmitArrayAccessor(registerReservations, arrayAccessor),
                typeInitialization => EmitTypeInitialization(registerReservations, typeInitialization, Identifier.Empty),
                functionCall => EmitFunctionCall(registerReservations, functionCall));

            var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);
            var tempIdentifier = new Identifier($"{assignment.Identifier.AsT1.Identifier.Name}{0}", default);

            return index + EmitAdd(new Number(registerReservations[tempIdentifier.Name], default), registerReservations) + EmitStore(tempRegister) + expression + EmitPointerStore(Identifier.Temporary, registerReservations);
        }

        var register = GetOrReserveRegister(assignment.Identifier.AsT0, registerReservations);
        var store = EmitStore(register);

        return expression + store;
    }

    private static string EmitBinaryOperation(Dictionary<string, uint> registerReservations, BinaryOperation operation)
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

    private static string EmitArrayAccessor(Dictionary<string, uint> registerReservations, ArrayAccessor arrayAccessor)
    {
        var index = arrayAccessor.Index.Value.Match(
            number => EmitLoad(number, registerReservations),
            identifier => EmitLoad(identifier, registerReservations),
            binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation),
            arrayAccessor => EmitArrayAccessor(registerReservations, arrayAccessor),
            typeInitialization => EmitTypeInitialization(registerReservations, typeInitialization, Identifier.Empty),
            functionCall => EmitFunctionCall(registerReservations, functionCall));

        var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);
        var tempIdentifier = new Identifier($"{arrayAccessor.Identifier.Name}{0}", default);

        return index + EmitAdd(new Number(registerReservations[tempIdentifier.Name], default), registerReservations) + EmitStore(tempRegister) + EmitPointerLoad(Identifier.Temporary, registerReservations);
    }

    private static string EmitTypeInitialization(Dictionary<string, uint> registerReservations, TypeInitialization typeInitialization, OneOf<Identifier, ArrayAccessor> identifier)
    {
        if (typeInitialization.Type.Name.IsT0)
        {
            var load = EmitLoad(new Number(0, default), registerReservations);

            for (var i = 0; i < typeInitialization.Type.Name.AsT0.Size; i++)
            {
                var register = ReserveRegister(new Identifier($"{identifier.Match(identifier => identifier.Name, arrayAccessor => arrayAccessor.Identifier.Name)}{i}", default), registerReservations);
                load += EmitStore(register);
            }

            return load;
        }

        var tempRegister = ReserveRegister(Identifier.Temporary, registerReservations);
        return EmitLoad(new Number(0, default), registerReservations) + EmitStore(tempRegister);
    }

    private static string EmitIfElse(Dictionary<string, uint> registerReservations, If @if, string? startOfBlockLabel, string? endOfBlockLabel)
    {
        var elseLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var conditionResult = EmitBinaryOperation(registerReservations, @if.Condition.Value.AsT2);
        var conditionJump = EmitJumpIfZero(elseLabel);

        var body = EmitStatement(@if.Body, registerReservations, startOfBlockLabel, endOfBlockLabel);
        var elseBody = @if.ElseBody is not null ? EmitStatement(@if.ElseBody, registerReservations, startOfBlockLabel, endOfBlockLabel) : string.Empty;

        return conditionResult + conditionJump + body + EmitGoto(endLabel) + EmitLabel(elseLabel) + elseBody + EmitLabel(endLabel);
    }

    private static string EmitWhileLoop(Dictionary<string, uint> registerReservations, While @while)
    {
        var startLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var conditionResult = EmitBinaryOperation(registerReservations, @while.Condition.Value.AsT2);
        var conditionJump = EmitJumpIfZero(endLabel);

        var body = EmitStatement(@while.Body, registerReservations, startLabel, endLabel);

        return EmitLabel(startLabel) + conditionResult + conditionJump + body + EmitGoto(startLabel) + EmitLabel(endLabel);
    }
    private static string EmitBreak(string endOfBlockLabel) => EmitGoto(endOfBlockLabel);
    private static string EmitContinue(string startOfBlockLabel) => EmitGoto(startOfBlockLabel);

    private static string EmitReturn(Dictionary<string, uint> registerReservations, Return @return)
    {
        var expression = @return.Expression.Value.Match(
            number => EmitLoad(number, registerReservations),
            identifier => EmitLoad(identifier, registerReservations),
            binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation),
            arrayAccessor => EmitArrayAccessor(registerReservations, arrayAccessor),
            typeInitialization => EmitTypeInitialization(registerReservations, typeInitialization, new Identifier("return", default)),
            functionCall => EmitFunctionCall(registerReservations, functionCall)
        );

        return expression;
    }
    private static string EmitFunctionCall(Dictionary<string, uint> registerReservations, FunctionCall functionCall) // TODO: Add support for early returns
    {
        var comment = EmitComment(functionCall.Function!.ToString().Replace("\n", "; "));

        foreach (var (argument, index) in functionCall.Arguments.Select((x, i) => (x, i)))
        {
            var expression = ((Expression)argument).Value.Match(
                number => EmitLoad(number, registerReservations),
                identifier => EmitLoad(identifier, registerReservations),
                binaryOperation => EmitBinaryOperation(registerReservations, binaryOperation),
                arrayAccessor => EmitArrayAccessor(registerReservations, arrayAccessor),
                typeInitialization => EmitTypeInitialization(registerReservations, typeInitialization, new Identifier($"arg{index}", default)),
                functionCall => EmitFunctionCall(registerReservations, functionCall)
            );

            var argumentDefinition = (ArgumentDefinition)functionCall.Function.Arguments[index];
            var argumentRegister = GetOrReserveRegister(new Identifier($"{argumentDefinition.Identifier.Name}", default, argumentDefinition.Type), registerReservations);

            comment += expression + EmitStore(argumentRegister);
        }

        var body = Emit(functionCall.Function!.Body.Scope, registerReservations, false);

        return comment + body;
    }

    private static string EmitLoad(OneOf<Number, Identifier> value, Dictionary<string, uint> registerReservations)
        => value.Match(number => $"LOAD #{number.Value}\n", identifier => $"LOAD {registerReservations[identifier.Name]}\n");
    private static string EmitPointerLoad(Identifier identifier, Dictionary<string, uint> registerReservations)
        => $"LOAD *{registerReservations[identifier.Name]}\n";

    private static string EmitStore(uint register) => $"STORE {register}\n";
    private static string EmitPointerStore(Identifier identifier, Dictionary<string, uint> registerReservations) => $"STORE *{registerReservations[identifier.Name]}\n";

    private static string EmitOperation(BinaryOperator @operator, OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations) => @operator switch
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
        BinaryOperator.LessThanOrEqual => EmitLessThanOrEqual(right, registerReservations),
        _ => string.Empty
    };

    private static string EmitAdd(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
        => right.Match(number => $"ADD #{number.Value}\n", identifier => $"ADD {registerReservations[identifier.Name]}\n");
    private static string EmitSubtract(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
        => right.Match(number => $"SUB #{number.Value}\n", identifier => $"SUB {registerReservations[identifier.Name]}\n");
    private static string EmitMultiply(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
        => right.Match(number => $"MUL #{number.Value}\n", identifier => $"MUL {registerReservations[identifier.Name]}\n");
    private static string EmitDivide(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
        => right.Match(number => $"DIV #{number.Value}\n", identifier => $"DIV {registerReservations[identifier.Name]}\n");
    private static string EmitEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var checkFailedLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var subtractRightFromLeft = EmitSubtract(right, registerReservations);
        var subtractLeftFromRight = EmitLoad(right, registerReservations) + EmitSubtract(left, registerReservations);

        return subtractRightFromLeft + EmitJumpIfNotZero(checkFailedLabel) + subtractLeftFromRight + EmitJumpIfNotZero(checkFailedLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitGoto(endLabel) + EmitLabel(checkFailedLabel) + EmitLoad(new Number(0, default), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitNotEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var equalLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var equal = EmitEqual(left, right, registerReservations);

        return equal + EmitJumpIfNotZero(equalLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitGoto(endLabel) + EmitLabel(equalLabel) + EmitLoad(new Number(0, default), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitGreaterThan(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var endLabel = Guid.NewGuid().ToLabelString();

        var subtractRightFromLeft = EmitSubtract(right, registerReservations);

        return subtractRightFromLeft + EmitJumpIfZero(endLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitLessThan(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var equalLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var greaterThan = EmitLoad(right, registerReservations) + EmitGreaterThan(left, registerReservations);
        var equal = EmitEqual(left, right, registerReservations);

        return greaterThan + EmitJumpIfZero(endLabel) + equal + EmitJumpIfNotZero(equalLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitGoto(endLabel) + EmitLabel(equalLabel) + EmitLoad(new Number(0, default), registerReservations) + EmitLabel(endLabel);
    }
    private static string EmitGreaterThanOrEqual(OneOf<Number, Identifier> left, OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var lessThanLabel = Guid.NewGuid().ToLabelString();
        var successLabel = Guid.NewGuid().ToLabelString();

        var lessThan = EmitLessThan(left, right, registerReservations);

        return lessThan + EmitJumpIfNotZero(lessThanLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitGoto(successLabel) + EmitLabel(lessThanLabel) + EmitLoad(new Number(0, default), registerReservations) + EmitLabel(successLabel);
    }
    private static string EmitLessThanOrEqual(OneOf<Number, Identifier> right, Dictionary<string, uint> registerReservations)
    {
        var greaterThanLabel = Guid.NewGuid().ToLabelString();
        var endLabel = Guid.NewGuid().ToLabelString();

        var greaterThan = EmitGreaterThan(right, registerReservations);

        return greaterThan + EmitJumpIfNotZero(greaterThanLabel) + EmitLoad(new Number(1, default), registerReservations) + EmitGoto(endLabel) + EmitLabel(greaterThanLabel) + EmitLoad(new Number(0, default), registerReservations) + EmitLabel(endLabel);
    }

    private static string EmitEnd() => "END\n";

    private static string EmitComment(string comment) => $"// {comment}\n";

    private static string EmitLabel(string label) => $"{label}:\n";

    private static string EmitGoto(string label) => $"GOTO {label}\n";
    private static string EmitJumpIfZero(string label) => $"JZERO {label}\n";
    private static string EmitJumpIfNotZero(string label) => $"JNZERO {label}\n";

    private static uint ReserveRegister(Identifier identifier, Dictionary<string, uint> registerReservations)
    {
        var register = (uint)registerReservations.Count + 1;
        registerReservations[identifier.Name] = register;
        return register;
    }
    private static uint GetOrReserveRegister(Identifier identifier, Dictionary<string, uint> registerReservations)
        => registerReservations.TryGetValue(identifier.Name, out var register) ? register : ReserveRegister(identifier, registerReservations);
    private static void FreeRegister(Identifier identifier, Dictionary<string, uint> registerReservations) => registerReservations.Remove(identifier.Name);
}
