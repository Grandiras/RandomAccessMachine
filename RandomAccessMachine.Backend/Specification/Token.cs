using OneOf;
using OneOf.Types;

namespace RandomAccessMachine.Backend.Specification;
public record struct Token(OneOf<string, uint, Error> Value, TokenType Type, uint LineNumber, uint ColumnNumber, uint Length);

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