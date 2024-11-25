using OneOf;

namespace RandomAccessMachine.Backend.Specification;
public record struct Argument(OneOf<Immediate, Address, AddressPointer, LabelReference> Value, Token Token)
{
    public override readonly string ToString() => Value.Match(
        immediate => immediate.ToString(),
        address => address.ToString(),
        addressPointer => addressPointer.ToString(),
        labelReference => labelReference.ToString()
    );
}

public record struct Immediate(uint Value)
{
    public override readonly string ToString() => "#" + Value.ToString();
}
public record struct Address(uint Value)
{
    public override readonly string ToString() => Value.ToString();
}
public record struct AddressPointer(uint Value)
{
    public override readonly string ToString() => "*" + Value.ToString();
}
public record struct LabelReference(string Name, Label Label)
{
    public override readonly string ToString() => Name;
}