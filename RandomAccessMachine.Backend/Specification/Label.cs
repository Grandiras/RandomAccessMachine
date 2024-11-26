namespace RandomAccessMachine.Backend.Specification;
public record Label(string Name, uint InstructionAddress, Token Token)
{
    public override string ToString() => $"{Name} at {InstructionAddress}";
}
