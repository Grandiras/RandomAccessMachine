using RandomAccessMachine.FAIL.ElementTree;

namespace RandomAccessMachine.FAIL.Specification;
public record struct Scope(List<Statement> Statements)
{
    public override readonly string ToString() => string.Join("\n", Statements);
}
