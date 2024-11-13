using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Debugging;
public record struct ErrorInfo(string Message, Token Token)
{
    public override string ToString() => $"Error at line {Token.LineNumber}, column {Token.ColumnNumber}: {Message}";
}
