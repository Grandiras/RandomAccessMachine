using OneOf;
using OneOf.Types;

namespace RandomAccessMachine.Backend.Specification;
public record struct Token(OneOf<string, uint, Error> Value, TokenType Type, uint LineNumber, uint ColumnNumber, uint Length)
{
    public override readonly string ToString() => $"{Type} {Value.Match(x => x, x => x.ToString(), x => x.ToString())} at {LineNumber}:{ColumnNumber} ({Length})";
}

public enum TokenType
{
    Label,
    OpCode,
    LabelReference,
    Address,
    Immediate,
    AddressPointer,
    Faulty
}