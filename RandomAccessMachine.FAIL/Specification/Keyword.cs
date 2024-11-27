﻿namespace RandomAccessMachine.FAIL.Specification;
public enum Keyword
{
    Var,
    Int,
    Bool,
    If,
    Else,
    While,
    Break,
    Continue,
}

public static class KeywordExtensions
{
    public static TokenType GetTokenType(this Keyword keyword) => keyword switch
    {
        Keyword.Var => TokenType.Var,
        Keyword.Int => TokenType.Type,
        Keyword.Bool => TokenType.Type,
        Keyword.If => TokenType.If,
        Keyword.Else => TokenType.Else,
        Keyword.While => TokenType.While,
        Keyword.Break => TokenType.Break,
        Keyword.Continue => TokenType.Continue,
        _ => throw new NotImplementedException(),
    };

    public static string GetTrueSpelling(this Keyword keyword) => keyword switch
    {
        Keyword.Var => "var",
        Keyword.Int => "int",
        Keyword.Bool => "bool",
        Keyword.If => "if",
        Keyword.Else => "else",
        Keyword.While => "while",
        Keyword.Break => "break",
        Keyword.Continue => "continue",
        _ => "",
    };

    public static bool IsSpelledCorrectly(this string spelling) => spelling switch
    {
        "var" => true,
        "int" => true,
        "bool" => true,
        "if" => true,
        "else" => true,
        "while" => true,
        "break" => true,
        "continue" => true,
        _ => false,
    };

    public static string GetTypeName(this Keyword keyword) => keyword switch
    {
        Keyword.Var => "var",
        Keyword.Int => "int",
        Keyword.Bool => "bool",
        _ => throw new NotImplementedException(),
    };
}
