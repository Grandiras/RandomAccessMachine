using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public class Interpreter
{
    public List<Register> Registers { get; private set; } = [new("Acc", 0), new("R1", 0), new("R2", 0), new("R3", 0), new("R4", 0), new("R5", 0)];
    public List<Instruction> Memory { get; private set; } = [];
    public uint MemoryPointer { get; private set; }
    public List<Label> Labels { get; private set; } = [];

    public bool IsRunning { get; private set; }

    public double Speed { get; set; } = 1; // Hz
    public bool IsRealTime { get; set; } = false;

    public event EventHandler? Started;
    public event EventHandler<uint>? Stepped;
    public event EventHandler? Stopped;


    public void LoadProgram(Scope program, uint registerCount = 5)
    {
        Memory = program.Instructions;
        Labels = program.Labels;
        Registers.Clear();
        Registers.Add(new("Acc", 0));
        for (var i = 0; i < registerCount; i++) Registers.Add(new($"R{i + 1}", 0));
        MemoryPointer = 0;
    }

    public async Task Execute(CancellationToken token)
    {
        IsRunning = true;
        Started?.Invoke(this, EventArgs.Empty);

        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1 / Speed));

        while (MemoryPointer < Memory.Count && IsRunning && !token.IsCancellationRequested && (IsRealTime || await periodicTimer.WaitForNextTickAsync(token)))
        {
            Stepped?.Invoke(this, MemoryPointer);
            await ExecuteInstruction(MemoryPointer);
            MemoryPointer++;
        }

        if (!IsRealTime) _ = await periodicTimer.WaitForNextTickAsync(token);

        IsRunning = false;
        Stopped?.Invoke(this, EventArgs.Empty);
    }
    public async Task ExecuteInstruction(uint pointer)
    {
        var instruction = Memory[(int)pointer];

        await Task.CompletedTask;

        if (instruction.OpCode is OpCode.END)
        {
            IsRunning = false;
            return;
        }

        if (instruction.OpCode is OpCode.LOAD or OpCode.ADD or OpCode.SUB or OpCode.MUL or OpCode.DIV)
        {
            var value = instruction.Argument!.Value.Value.Match(
                immediate => immediate.Value,
                address => Registers[(int)address.Value].Value,
                addressPointer => Registers[(int)Registers[(int)addressPointer.Value].Value].Value,
                _ => throw new Exception($"A label reference is not valid for OpCode {instruction.OpCode}!"));

            if (instruction.OpCode is OpCode.LOAD) Registers[0].Value = value;
            else if (instruction.OpCode is OpCode.ADD) Registers[0].Value += value;
            else if (instruction.OpCode is OpCode.SUB) Registers[0].Value = value > Registers[0].Value ? 0 : Registers[0].Value - value;
            else if (instruction.OpCode is OpCode.MUL) Registers[0].Value *= value;
            else if (instruction.OpCode is OpCode.DIV)
            {
                if (value is 0)
                {
                    IsRunning = false;
                    return;
                }
                Registers[0].Value /= value;
            }

            return;
        }

        if (instruction.OpCode is OpCode.STORE)
        {
            var value = instruction.Argument!.Value.Value.Match(
                _ => throw new Exception($"A immediate value is not valid for OpCode {instruction.OpCode}!"),
                address => (int)address.Value,
                addressPointer => (int)Registers[(int)addressPointer.Value].Value,
                _ => throw new Exception($"A label reference is not valid for OpCode {instruction.OpCode}!"));

            Registers[value].Value = Registers[0].Value;

            return;
        }

        if (instruction.OpCode is OpCode.GOTO or OpCode.JZERO or OpCode.JNZERO)
        {
            if (instruction.OpCode is OpCode.JZERO && Registers[0].Value is not 0) return;
            if (instruction.OpCode is OpCode.JNZERO && Registers[0].Value is 0) return;

            MemoryPointer = instruction.Argument!.Value.Value.AsT3.Label.InstructionAddress - 1;

            return;
        }

        throw new Exception($"Unknown OpCode: {instruction.OpCode}");
    }
}
