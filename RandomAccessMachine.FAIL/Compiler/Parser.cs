using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Parser
{
    public static OneOf<Scope, ErrorInfo> Parse(Queue<Token> tokens)
    {
        var scope = new Scope([]);

        if (tokens.Count is 0) return scope;

        while (tokens.Count > 0)
        {
            var statement = ParseStatement(tokens);
            if (statement.IsT1) return statement.AsT1;
            scope.Statements.Add(statement.AsT0);
        }

        return scope;
    }

    private static OneOf<Statement, ErrorInfo> ParseStatement(Queue<Token> tokens)
    {
        var token = tokens.Dequeue();

        var statement = token.Type switch
        {
            TokenType.Var or TokenType.Type => ParseDeclaration(tokens, token),
            TokenType.Identifier => ParseAssignment(tokens, token),
            _ => new ErrorInfo($"Unexpected token type: {token.Type}", token)
        };

        if (statement.IsT1) return statement.AsT1;

        var endOfLine = tokens.Dequeue();
        if (endOfLine.Type is not TokenType.EndOfLine) return new ErrorInfo($"Unexpected token type: {endOfLine.Type}", endOfLine);

        return statement.AsT0;
    }

    private static OneOf<Statement, ErrorInfo> ParseDeclaration(Queue<Token> tokens, Token type)
    {
        var identifier = tokens.Dequeue();
        if (identifier.Type is not TokenType.Identifier) return new ErrorInfo($"Unexpected token type: {identifier.Type}", identifier);

        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new(identifier.Value.AsT0, new(type.Value.AsT2.GetTypeName())), expression.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseAssignment(Queue<Token> tokens, Token identifier)
    {
        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new(identifier.Value.AsT0, new("var")), expression.AsT0); // TODO: validate type later
    }

    private static OneOf<Expression, ErrorInfo> ParseArithmetic(Queue<Token> tokens, OneOf<Expression, None> heap) => ParseArithmetic(tokens, CalculationsExtensions.All, heap);
    private static OneOf<Expression, ErrorInfo> ParseArithmetic(Queue<Token> tokens, Calculations calculations, OneOf<Expression, None> heap)
    {
        if (calculations.HasFlag(Calculations.Term))
        {
            var result = ParseTerm(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.AsT0;
        }

        if (calculations.HasFlag(Calculations.DotCalculations))
        {
            var result = ParseDotCalculation(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.AsT0;
        }

        if (calculations.HasFlag(Calculations.StrokeCalculations))
        {
            var result = ParseStrokeCalculation(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.AsT0;
        }

        return heap.AsT0;
    }

    private static OneOf<Expression, None, ErrorInfo> ParseTerm(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.Identifier)
        {
            _ = tokens.Dequeue();

            return new Expression(new Identifier(token.Value.AsT0, new("var"))); // TODO: validate type later
        }

        if (token.Type is TokenType.LeftParenthesis)
        {
            _ = tokens.Dequeue();

            var expression = ParseArithmetic(tokens, heap);
            if (expression.IsT1) return expression.AsT1;

            var rightParenthesis = tokens.Dequeue();
            if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type: {rightParenthesis.Type}", rightParenthesis);

            return expression.AsT0;
        }

        if (token.Type is TokenType.Number)
        {
            _ = tokens.Dequeue();

            return new Expression(new Number(token.Value.AsT1));
        }

        return heap.TryPickT0(out var some, out var none) ? some : none;
    }

    private static OneOf<Expression, None, ErrorInfo> ParseDotCalculation(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.BinaryOperator && token.Value.AsT3 is BinaryOperator.Multiply or BinaryOperator.Divide)
        {
            _ = tokens.Dequeue();

            var right = ParseArithmetic(tokens, Calculations.DotCalculations.Above(), new None());
            if (right.IsT1) return right.AsT1;

            if (heap.IsT1) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

            var term = ParseArithmetic(tokens, Calculations.DotCalculations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0)));
            if (term.IsT1) return term.AsT1;

            return term.AsT0;
        }

        return heap.TryPickT0(out var some, out var none) ? some : none;
    }

    private static OneOf<Expression, None, ErrorInfo> ParseStrokeCalculation(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.BinaryOperator && token.Value.AsT3 is BinaryOperator.Add or BinaryOperator.Subtract)
        {
            _ = tokens.Dequeue();

            var right = ParseArithmetic(tokens, Calculations.StrokeCalculations.Above(), new None());
            if (right.IsT1) return right.AsT1;

            if (heap.IsT1) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

            var term = ParseArithmetic(tokens, Calculations.StrokeCalculations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0)));
            if (term.IsT1) return term.AsT1;

            return term.AsT0;
        }

        return heap.TryPickT0(out var some, out var none) ? some : none; ;
    }
}
