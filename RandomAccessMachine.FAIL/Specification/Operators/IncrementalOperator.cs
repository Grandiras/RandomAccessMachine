namespace RandomAccessMachine.FAIL.Specification.Operators;
public enum IncrementalOperator
{
    Increment,
    Decrement
}

public static class IncrementalOperatorExtensions
{
    public static IncrementalOperator? GetIncrementalOperator(this string @operator) => @operator switch
    {
        "++" => IncrementalOperator.Increment,
        "--" => IncrementalOperator.Decrement,
        _ => null
    };

    public static string GetIncrementalOperatorString(this IncrementalOperator @operator) => @operator switch
    {
        IncrementalOperator.Increment => "++",
        IncrementalOperator.Decrement => "--",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
    };

    public static BinaryOperator GetBinaryOperator(this IncrementalOperator @operator) => @operator switch
    {
        IncrementalOperator.Increment => BinaryOperator.Add,
        IncrementalOperator.Decrement => BinaryOperator.Subtract,
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
    };
}