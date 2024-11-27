using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Parser
{
    public static OneOf<Scope, ErrorInfo> Parse(Queue<Token> tokens, TokenType endOfBlockSign = TokenType.Error, bool isLoop = false)
    {
        var scope = new Scope([]);

        if (tokens.Count is 0) return scope;

        while (tokens.Count > 0 && (endOfBlockSign is TokenType.Error || tokens.Peek().Type != endOfBlockSign))
        {
            var statement = ParseStatement(tokens, isLoop);
            if (statement.IsT1) return statement.AsT1;
            scope.Statements.Add(statement.AsT0);
        }

        if (endOfBlockSign is not TokenType.Error)
        {
            var endOfBlock = tokens.Dequeue();
            if (endOfBlock.Type != endOfBlockSign) return new ErrorInfo($"Unexpected token type {endOfBlock.Type}! Requiring type {endOfBlockSign}.", endOfBlock);
        }

        return scope;
    }

    private static OneOf<Statement, ErrorInfo> ParseStatement(Queue<Token> tokens, bool isLoop = false, bool needsEndOfLine = true)
    {
        var token = tokens.Dequeue();

        var isBlock = token.Type is TokenType.LeftCurlyBrace;

        var statement = token.Type switch
        {
            TokenType.Var or TokenType.Type => ParseDeclaration(tokens, token),
            TokenType.Identifier => ParseAssignment(tokens, token),
            TokenType.If => ParseIf(tokens, token, out isBlock, isLoop),
            TokenType.While => ParseWhile(tokens, out isBlock),
            TokenType.LeftCurlyBrace => Parse(tokens, TokenType.RightCurlyBrace, isLoop).TryPickT0(out var scope, out var error) ? new Body(scope) : error,
            TokenType.Break => isLoop ? new Break() : new ErrorInfo($"Break must be inside a loop!", token),
            TokenType.Continue => isLoop ? new Continue() : new ErrorInfo($"Continue must be inside a loop!", token),
            _ => new ErrorInfo($"Unexpected token type {token.Type} at start of statement!", token)
        };

        if (statement.IsT1) return statement.AsT1;

        if (isBlock || !needsEndOfLine) return statement.AsT0;

        var endOfLine = tokens.Dequeue();
        if (endOfLine.Type is not TokenType.EndOfLine) return new ErrorInfo($"Unexpected token type {endOfLine.Type}! Requiring type {TokenType.EndOfLine}.", endOfLine);

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

        if (calculations.HasFlag(Calculations.TestOperations))
        {
            var result = ParseTestOperation(tokens, heap);
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

        if (token.Type is TokenType.BinaryOperator && token.Value.AsT3.IsDotCalculation())
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

        if (token.Type is TokenType.BinaryOperator && token.Value.AsT3.IsStrokeCalculation())
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

    private static OneOf<Expression, None, ErrorInfo> ParseTestOperation(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.BinaryOperator && token.Value.AsT3.IsTestOperation())
        {
            _ = tokens.Dequeue();

            var right = ParseArithmetic(tokens, Calculations.TestOperations.Above(), new None());
            if (right.IsT1) return right.AsT1;

            if (heap.IsT1) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

            var term = ParseArithmetic(tokens, Calculations.TestOperations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0)));
            if (term.IsT1) return term.AsT1;

            return term.AsT0;
        }
        return heap.TryPickT0(out var some, out var none) ? some : none;
    }

    private static OneOf<Statement, ErrorInfo> ParseIf(Queue<Token> tokens, Token token, out bool isBlock, bool isLoop = false)
    {
        isBlock = false;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return new ErrorInfo($"Unexpected token type: {leftParenthesis.Type}", leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return new ErrorInfo($"Unexpected token type: {condition.AsT0.Value.AsT2.Operator}", new()); // TODO: improve error message (token)

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type: {rightParenthesis.Type}", rightParenthesis);
        var leftCurlyBrace = tokens.Peek();

        OneOf<Scope, ErrorInfo>? body;
        if (leftCurlyBrace.Type is not TokenType.LeftCurlyBrace)
        {
            var statement = ParseStatement(tokens, isLoop, false);
            if (statement.IsT1) return statement.AsT1;

            body = new Scope([statement.AsT0]);
        }
        else
        {
            _ = tokens.Dequeue();
            body = Parse(tokens, TokenType.RightCurlyBrace, isLoop);

            isBlock = true;
        }

        if (body!.Value.IsT1) return body!.Value.AsT1;

        var next = tokens.Peek();
        if (next.Type is not TokenType.Else) return new If(condition.AsT0, new Body(body!.Value.AsT0));

        _ = tokens.Dequeue();

        OneOf<Scope, ErrorInfo>? elseBody = null;

        var leftCurlyBraceElse = tokens.Peek();
        if (leftCurlyBraceElse.Type is not TokenType.LeftCurlyBrace)
        {
            var statement = ParseStatement(tokens, isLoop, false);
            if (statement.IsT1) return statement.AsT1;

            elseBody = new Scope([statement.AsT0]);

            isBlock = false;
        }
        else
        {
            _ = tokens.Dequeue();
            body = Parse(tokens, TokenType.RightCurlyBrace, isLoop);

            isBlock = true;
        }

        if (elseBody!.Value.IsT1) return elseBody!.Value.AsT1;

        return new If(condition.AsT0, new Body(body!.Value.AsT0), new Body(elseBody!.Value.AsT0));
    }

    private static OneOf<Statement, ErrorInfo> ParseWhile(Queue<Token> tokens, out bool isBlock)
    {
        isBlock = false;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return new ErrorInfo($"Unexpected token type: {leftParenthesis.Type}", leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return new ErrorInfo($"Unexpected token type: {condition.AsT0.Value.AsT2.Operator}", new()); // TODO: improve error message (token)

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type: {rightParenthesis.Type}", rightParenthesis);
        var leftCurlyBrace = tokens.Peek();

        OneOf<Scope, ErrorInfo>? body;
        if (leftCurlyBrace.Type is not TokenType.LeftCurlyBrace)
        {
            var statement = ParseStatement(tokens, true, false);
            if (statement.IsT1) return statement.AsT1;

            body = new Scope([statement.AsT0]);
        }
        else
        {
            _ = tokens.Dequeue();
            body = Parse(tokens, TokenType.RightCurlyBrace, true);

            isBlock = true;
        }

        if (body!.Value.IsT1) return body!.Value.AsT1;

        return new While(condition.AsT0, new Body(body!.Value.AsT0));
    }
}
