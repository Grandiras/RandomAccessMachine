using OneOf;
using RandomAccessMachine.Backend.Debugging;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public static class LabelValidator
{
    public static OneOf<Scope, ErrorInfo> Validate(Scope scope)
    {
        // For each label reference in the instructions, check if it exists, and if it does, set the instruction's label property to the label
        for (var i = 0; i < scope.Instructions.Count; i++)
        {
            var instruction = scope.Instructions[i];
            if (instruction.Argument is null || !instruction.Argument.Value.Value.IsT3) continue;

            var label = scope.Labels.Find(l => l.Name == instruction.Argument!.Value.Value.AsT3.Name);
            if (label is null) return new ErrorInfo($"Label '{instruction.Argument!.Value.Value.AsT3.Name}' not found", instruction.Argument!.Value.Token);

            var newInstruction = instruction with { Argument = new Argument(new LabelReference(instruction.Argument!.Value.Value.AsT3.Name, label), instruction.Argument!.Value.Token) };
            scope.Instructions[scope.Instructions.IndexOf(instruction)] = newInstruction;
        }

        return scope;
    }
}
