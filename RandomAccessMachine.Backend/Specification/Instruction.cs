namespace RandomAccessMachine.Backend.Specification;
public record Instruction(OpCode OpCode, Argument? Argument, Token Token)
{
    public override string ToString() => Argument is null ? $"{OpCode}" : $"{OpCode} {Argument}";
}