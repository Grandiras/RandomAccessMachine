namespace RandomAccessMachine.FAIL.Specification;
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Equal,
    NotEqual,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
}

public static class BinaryOperatorExtensions
{
    public static BinaryOperator? GetBinaryOperator(this string @operator) => @operator switch
    {
        "+" => BinaryOperator.Add,
        "-" => BinaryOperator.Subtract,
        "*" => BinaryOperator.Multiply,
        "/" => BinaryOperator.Divide,
        "==" => BinaryOperator.Equal,
        "!=" => BinaryOperator.NotEqual,
        ">" => BinaryOperator.GreaterThan,
        "<" => BinaryOperator.LessThan,
        ">=" => BinaryOperator.GreaterThanOrEqual,
        "<=" => BinaryOperator.LessThanOrEqual,
        _ => null,
    };

    public static string GetBinaryOperatorString(this BinaryOperator @operator) => @operator switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.LessThan => "<",
        BinaryOperator.GreaterThanOrEqual => ">=",
        BinaryOperator.LessThanOrEqual => "<=",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null),
    };

    public static BinaryOperator[] GetDotCalculations() => [BinaryOperator.Multiply, BinaryOperator.Divide];
    public static BinaryOperator[] GetStrokeCalculations() => [BinaryOperator.Add, BinaryOperator.Subtract];
    public static BinaryOperator[] GetTestOperations() => [BinaryOperator.Equal, BinaryOperator.NotEqual, BinaryOperator.GreaterThan, BinaryOperator.LessThan, BinaryOperator.GreaterThanOrEqual, BinaryOperator.LessThanOrEqual];

    public static bool IsDotCalculation(this BinaryOperator @operator) => @operator is BinaryOperator.Multiply or BinaryOperator.Divide;
    public static bool IsStrokeCalculation(this BinaryOperator @operator) => @operator is BinaryOperator.Add or BinaryOperator.Subtract;
    public static bool IsTestOperation(this BinaryOperator @operator) => @operator is BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.GreaterThan or BinaryOperator.LessThan or BinaryOperator.GreaterThanOrEqual or BinaryOperator.LessThanOrEqual;
}
