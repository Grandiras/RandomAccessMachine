﻿using OneOf;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Compiler;
public static class IdentifierResolver
{
    public static OneOf<Scope, ErrorInfo> ResolveFunctions(Scope scope)
    {
        var functions = scope.OfType<FunctionDeclaration>();
        var functionCalls = scope.OfType<FunctionCall>()
            .Concat(scope.OfType<Expression>().Where(x => x.Value.IsT5).Select(x => x.Value.AsT5))
            .Concat(scope.OfType<Assignment>().Where(x => x.Expression.Value.IsT5).Select(x => x.Expression.Value.AsT5));

        foreach (var functionCall in functionCalls)
        {
            var function = functions.FirstOrDefault(x => x.Identifier.Name == functionCall.Identifier.Name);
            if (function is null) return ErrorInfo.FunctionNotFound(functionCall.Token);

            if (functionCall.Arguments.Count != function.Arguments.Count) return ErrorInfo.FunctionArgumentMismatch(functionCall.Token);

            functionCall.Function = function;
        }

        return scope;
    }
}
