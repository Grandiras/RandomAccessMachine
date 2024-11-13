using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public class Interpreter(Scope program, uint registerCount = 5)
{
    public List<uint> Registers { get; private set; } = Enumerable.Repeat(0u, (int)registerCount + 1).ToList();
    public List<Instruction> Memory { get; private set; } = program.Instructions;
    public uint MemoryPointer { get; private set; }
    public List<Label> Labels { get; private set; } = program.Labels;

    public bool IsRunning { get; private set; }


    public void ResetState()
    {
        Registers.ForEach(r => r = 0);
        MemoryPointer = 0;
    }

    public void Execute(CancellationToken token)
    {
        IsRunning = true;

        while (MemoryPointer < Memory.Count && IsRunning && !token.IsCancellationRequested)
        {
            ExecuteInstruction(MemoryPointer);
            MemoryPointer++;
        }

        IsRunning = false;
    }
    public void ExecuteInstruction(uint pointer)
    {
        var instruction = Memory[(int)pointer];

        if (instruction.OpCode is OpCode.END)
        {
            IsRunning = false;
            return;
        }

        if (instruction.OpCode is OpCode.LOAD or OpCode.ADD or OpCode.SUB or OpCode.MUL or OpCode.DIV)
        {
            var value = instruction.Argument!.Value.Value.Match(
                immediate => immediate.Value,
                address => Registers[(int)address.Value],
                addressPointer => Registers[(int)Registers[(int)addressPointer.Value]],
                _ => throw new Exception($"A label reference is not valid for OpCode {instruction.OpCode}!"));

            if (instruction.OpCode is OpCode.LOAD) Registers[0] = value;
            else if (instruction.OpCode is OpCode.ADD) Registers[0] += value;
            else if (instruction.OpCode is OpCode.SUB) Registers[0] -= value;
            else if (instruction.OpCode is OpCode.MUL) Registers[0] *= value;
            else if (instruction.OpCode is OpCode.DIV) Registers[0] /= value;

            return;
        }

        if (instruction.OpCode is OpCode.STORE)
        {
            var value = instruction.Argument!.Value.Value.Match(
                _ => throw new Exception($"A immediate value is not valid for OpCode {instruction.OpCode}!"),
                address => (int)address.Value,
                addressPointer => (int)Registers[(int)addressPointer.Value],
                _ => throw new Exception($"A label reference is not valid for OpCode {instruction.OpCode}!"));

            Registers[value] = Registers[0];

            return;
        }

        if (instruction.OpCode is OpCode.GOTO or OpCode.JZERO or OpCode.JNZERO)
        {
            if (instruction.OpCode is OpCode.JZERO && Registers[0] is not 0) return;
            if (instruction.OpCode is OpCode.JNZERO && Registers[0] is 0) return;

            MemoryPointer = instruction.Argument!.Value.Value.AsT3.Label.InstructionAddress;

            return;
        }

        throw new Exception($"Unknown OpCode: {instruction.OpCode}");
    }
}
