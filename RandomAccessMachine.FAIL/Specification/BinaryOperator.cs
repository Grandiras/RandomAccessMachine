namespace RandomAccessMachine.FAIL.Specification;
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

public static class BinaryOperatorExtensions
{
    public static BinaryOperator? GetBinaryOperator(this char character) => character switch
    {
        '+' => BinaryOperator.Add,
        '-' => BinaryOperator.Subtract,
        '*' => BinaryOperator.Multiply,
        '/' => BinaryOperator.Divide,
        _ => null,
    };

    public static string GetBinaryOperatorString(this BinaryOperator @operator) => @operator switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null),
    };
}
