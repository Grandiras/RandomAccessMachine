using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Specification.Operators;

namespace RandomAccessMachine.FAIL.Specification;
public record struct Token(OneOf<string, uint, Keyword, BinaryOperator, SelfAssignmentOperator, IncrementalOperator, Error> Value, TokenType Type, uint LineNumber, uint ColumnNumber, uint Length, string Raw)
{
    public override readonly string ToString()
        => $"{Type} {Value.Match(x => x, x => x.ToString(), x => x.ToString(), x => x.ToString(), x => x.ToString(), x => x.ToString(), x => x.ToString())} (Raw: {Raw}) at {LineNumber}:{ColumnNumber} ({Length})";
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
    EndOfStatement,
    ErrorOrEmpty,
    While,
    LeftCurlyBrace,
    RightCurlyBrace,
    Break,
    Continue,
    If,
    Else,
    SelfAssignment,
    IncrementalOperator,
    LeftSquareBrace,
    RightSquareBrace,
    New,
    FunctionDeclaration,
    Return,
    ReturnDeclaration,
    Comma,
}
