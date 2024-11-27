using RandomAccessMachine.FAIL.ElementTree;
using System.Collections;

namespace RandomAccessMachine.FAIL.Specification;
public record struct Scope(List<Statement> Statements, Scope[] SharedScopes) : IEnumerable<Statement>
{
    public Scope(params Scope[] sharedScopes) : this([], sharedScopes) { }


    public readonly Statement? Search(Func<Statement, bool> predicate, bool singleLayer = false)
    {
        var entry = Statements.FirstOrDefault(predicate);
        if (entry is not null) return entry;

        if (singleLayer) return null;

        foreach (var sharedScope in SharedScopes)
        {
            var sharedEntry = sharedScope.Search(predicate);
            if (sharedEntry is not null) return sharedEntry;
        }

        return null;
    }

    public readonly void Add(Statement statement) => Statements.Add(statement);
    public readonly Scope Add(Scope scope)
    {
        Statements.AddRange(scope.Statements);
        return this;
    }

    public readonly IEnumerator<Statement> GetEnumerator() => Statements.GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override readonly string ToString() => string.Join("\n", Statements);
}
