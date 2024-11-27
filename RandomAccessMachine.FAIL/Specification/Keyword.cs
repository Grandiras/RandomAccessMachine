namespace RandomAccessMachine.FAIL.Specification;
public enum Keyword
{
    Var,
    Int,
    Bool,
    While,
}

public static class KeywordExtensions
{
    public static TokenType GetTokenType(this Keyword keyword) => keyword switch
    {
        Keyword.Var => TokenType.Var,
        Keyword.Int => TokenType.Type,
        Keyword.Bool => TokenType.Type,
        Keyword.While => TokenType.While,
        _ => throw new NotImplementedException(),
    };

    public static string GetTypeName(this Keyword keyword) => keyword switch
    {
        Keyword.Var => "var",
        Keyword.Int => "int",
        Keyword.Bool => "bool",
        _ => throw new NotImplementedException(),
    };
}
