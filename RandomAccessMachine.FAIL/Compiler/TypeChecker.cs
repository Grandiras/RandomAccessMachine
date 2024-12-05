using OneOf;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Compiler;
public static class TypeChecker
{
    public static OneOf<Scope, ErrorInfo> CheckTypes(Scope scope)
    {
        foreach (var declaration in scope.OfType<Assignment>())
        {
            if (declaration.Identifier.IsT1) continue;

            declaration.Identifier.AsT0.Type = declaration.Expression.Value.Match(x => x.Type, x => ElementType.Int, x => x.GetResultType(), x => x.Identifier.Type, x => x.Type, x => x.Function!.ReturnType);
        }

        foreach (var returnStatement in scope.OfType<FunctionDeclaration>().SelectMany(x => x.Body.Scope.Statements.OfType<Return>()))
        {
            if (!returnStatement.Expression.Value.IsT0) continue;

            returnStatement.Expression.Value.AsT0.Type = returnStatement.Expression.Value.Match(x => x.Type, x => ElementType.Int, x => x.GetResultType(), x => x.Identifier.Type, x => x.Type, x => x.Function!.ReturnType);
        }

         return scope;
    }
}
