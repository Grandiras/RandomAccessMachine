namespace RandomAccessMachine.Backend.Specification;
public record struct Scope(List<Instruction> Instructions, List<Label> Labels);
