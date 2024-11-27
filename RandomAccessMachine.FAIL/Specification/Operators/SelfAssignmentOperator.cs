using RandomAccessMachine.FAIL.Specification.Operators;

namespace RandomAccessMachine.FAIL.Specification;
public enum SelfAssignmentOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

public static class SelfAssignmentOperatorExtensions
{
    public static SelfAssignmentOperator? GetSelfAssignmentOperator(this string @operator) => @operator switch
    {
        "+=" => SelfAssignmentOperator.Add,
        "-=" => SelfAssignmentOperator.Subtract,
        "*=" => SelfAssignmentOperator.Multiply,
        "/=" => SelfAssignmentOperator.Divide,
        _ => null,
    };

    public static string GetSelfAssignmentOperatorString(this SelfAssignmentOperator @operator) => @operator switch
    {
        SelfAssignmentOperator.Add => "+=",
        SelfAssignmentOperator.Subtract => "-=",
        SelfAssignmentOperator.Multiply => "*=",
        SelfAssignmentOperator.Divide => "/=",
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null),
    };

    public static BinaryOperator GetBinaryOperator(this SelfAssignmentOperator @operator) => @operator switch
    {
        SelfAssignmentOperator.Add => BinaryOperator.Add,
        SelfAssignmentOperator.Subtract => BinaryOperator.Subtract,
        SelfAssignmentOperator.Multiply => BinaryOperator.Multiply,
        SelfAssignmentOperator.Divide => BinaryOperator.Divide,
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null),
    };
}