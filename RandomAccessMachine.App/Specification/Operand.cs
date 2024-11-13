global using Operand = OneOf.OneOf<RandomAccessMachine.App.Specification.Immediate, RandomAccessMachine.App.Specification.Address, RandomAccessMachine.App.Specification.AddressPointer, RandomAccessMachine.App.Specification.Label>;

namespace RandomAccessMachine.App.Specification;

public record struct Immediate(uint Value);
public record struct Address(uint Value);
public record struct AddressPointer(uint Value);
public record struct Label(string Name, uint InstructionPointer);