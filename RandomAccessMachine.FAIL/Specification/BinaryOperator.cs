namespace RandomAccessMachine.FAIL.Specification;
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Equal,
    GreaterThan,
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
        BinaryOperator.Equal => "==",
        BinaryOperator.GreaterThan => ">",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null),
    };
}
