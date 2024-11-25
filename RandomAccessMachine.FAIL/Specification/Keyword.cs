namespace RandomAccessMachine.FAIL.Specification;
public enum Keyword
{
    Var,
    Int
}

public static class KeywordExtensions
{
    public static TokenType GetTokenType(this Keyword keyword) => keyword switch
    {
        Keyword.Var => TokenType.Var,
        Keyword.Int => TokenType.Type,
        _ => throw new NotImplementedException(),
    };

    public static string GetTypeName(this Keyword keyword) => keyword switch
    {
        Keyword.Var => "var",
        Keyword.Int => "int",
        _ => throw new NotImplementedException(),
    };
}
