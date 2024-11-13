using OneOf;

namespace RandomAccessMachine.Backend.Specification;
public record struct Argument(OneOf<Immediate, Address, AddressPointer, LabelReference> Value, Token Token);

public record struct Immediate(uint Value);
public record struct Address(uint Value);
public record struct AddressPointer(uint Value);
public record struct LabelReference(string Name, Label Label);