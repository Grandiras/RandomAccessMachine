using OneOf;
using OneOf.Types;

namespace RandomAccessMachine.FAIL.Specification;
public record struct Token(OneOf<string, uint, Keyword, BinaryOperator, Error> Value, TokenType Type, uint LineNumber, uint ColumnNumber, uint Length, string Raw)
{
    public override readonly string ToString()
        => $"{Type} {Value.Match(x => x, x => x.ToString(), x => x.ToString(), x => x.ToString(), x => x.ToString())} (Raw: {Raw}) at {LineNumber}:{ColumnNumber} ({Length})";
}

public enum TokenType
{
    BinaryOperator,
    Assignment,
    Var,
    Type,
    LeftParenthesis,
    RightParenthesis,
    Number,
    Identifier,
    EndOfLine,
    Error,
    While,
    LeftCurlyBrace,
    RightCurlyBrace,
    Break,
    Continue,
}
