using OneOf;
using RandomAccessMachine.Backend.Debugging;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public static class BoundsChecker
{
    public static OneOf<Scope, ErrorInfo> CheckBounds(Scope scope, Interpreter interpreter)
    {
        var registerCount = interpreter.Registers.Count;

        // For each instruction and calculation, check that the register bounds are not violated
        foreach (var instruction in scope.Instructions)
        {
            var argument = instruction.Argument;
            if (argument?.Value.IsT1 is true && argument.Value.Value.AsT1.Value >= registerCount)
            {
                return new ErrorInfo($"Register {argument.Value.Value.AsT1.Value} out of bounds!", argument.Value.Token);
            }
        }

        return scope;
    }
}