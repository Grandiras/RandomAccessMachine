using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Debugging;
public record struct ErrorInfo(string Message, Token Token)
{
    public override readonly string ToString() => $"Error at line {Token.LineNumber}, column {Token.ColumnNumber}: {Message}";
}