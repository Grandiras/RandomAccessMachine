namespace RandomAccessMachine.Backend.Specification;
public record Instruction(OpCode OpCode, Argument? Argument, Token Token);