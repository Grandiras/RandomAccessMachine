using OneOf;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;
using System.Text;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Emitter
{
    public static string Emit(Scope scope)
    {
        var output = new StringBuilder();
        var registerReservations = new Dictionary<Identifier, uint>();

        foreach (var statement in scope.Statements)
        {
            _ = output.Append(EmitComment(statement.ToString()));

            if (statement is Assignment assignment)
                _ = output.Append(EmitAssignment(registerReservations, assignment));
        }

        _ = output.Append(EmitEnd());

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
            if (operation.Right.Value.IsT0)
            {
                _ = registerReservations[operation.Right.Value.AsT0];
                return EmitLoad(operation.Left.Value.AsT0, registerReservations) + EmitOperation(operation.Operator, operation.Right.Value.AsT0, registerReservations);
            }

            if (operation.Right.Value.IsT1)
            {
                return EmitLoad(operation.Left.Value.AsT0, registerReservations) + EmitOperation(operation.Operator, operation.Right.Value.AsT1, registerReservations);
            }

            return string.Empty;
        }

        if (operation.Left.Value.IsT1)
        {
            if (operation.Right.Value.IsT0)
            {
                return EmitLoad(operation.Left.Value.AsT1, registerReservations) + EmitOperation(operation.Operator, operation.Right.Value.AsT0, registerReservations);
            }

            if (operation.Right.Value.IsT1)
            {
                return EmitLoad(operation.Left.Value.AsT1, registerReservations) + EmitOperation(operation.Operator, operation.Right.Value.AsT1, registerReservations);
            }

            return string.Empty;
        }

        if (operation.Left.Value.IsT2)
        {
            if (operation.Right.Value.IsT0)
            {
                return EmitBinaryOperation(registerReservations, operation.Left.Value.AsT2) + EmitOperation(operation.Operator, operation.Right.Value.AsT0, registerReservations);
            }

            if (operation.Right.Value.IsT1)
            {
                return EmitBinaryOperation(registerReservations, operation.Left.Value.AsT2) + EmitOperation(operation.Operator, operation.Right.Value.AsT1, registerReservations);
            }

            return string.Empty;
        }

        return string.Empty;
    }

    private static string EmitLoad(OneOf<Number, Identifier> value, Dictionary<Identifier, uint> registerReservations)
        => value.Match(number => $"LOAD #{number.Value}\n", identifier => $"LOAD {registerReservations[identifier]}\n");

    private static string EmitStore(uint register) => $"STORE {register}\n";

    private static string EmitOperation(BinaryOperator @operator, OneOf<Number, Identifier> right, Dictionary<Identifier, uint> registerReservations) => @operator switch
    {
        BinaryOperator.Add => EmitAdd(right, registerReservations),
        BinaryOperator.Subtract => EmitSubtract(right, registerReservations),
        BinaryOperator.Multiply => EmitMultiply(right, registerReservations),
        BinaryOperator.Divide => EmitDivide(right, registerReservations),
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

    private static string EmitEnd() => "END\n";

    private static string EmitComment(string comment) => $"// {comment}\n";

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
