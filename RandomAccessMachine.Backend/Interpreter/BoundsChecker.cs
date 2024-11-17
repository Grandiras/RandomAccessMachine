using OneOf;
using RandomAccessMachine.Backend.Debugging;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public static class BoundsChecker
{
    public static OneOf<Scope, ErrorInfo> CheckBounds(Scope scope, Interpreter interpreter)
    {
        // for each instruction and calculation, check that the register bounds (e.g. 5 registers, but 6 is accessed) are not violated
        foreach (var instruction in scope.Instructions)
        {
            if (instruction.Argument is not null)
            {
                if (instruction.Argument!.Value.Value.IsT1 && instruction.Argument!.Value.Value.AsT1.Value >= interpreter.Registers.Count)
                    return new ErrorInfo($"Register {instruction.Argument!.Value.Value.AsT1.Value} out of bounds!", instruction.Argument!.Value.Token);
            }
        }

        return scope;
    }
}
