using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Compiler;
public static class Parser
{
    public static OneOf<Scope, ErrorInfo> Parse(Queue<Token> tokens) => ParseStatementList(tokens);

    private static OneOf<Scope, ErrorInfo> ParseStatementList(Queue<Token> tokens, TokenType startOfBlockSign = TokenType.ErrorOrEmpty, TokenType endOfBlockSign = TokenType.ErrorOrEmpty, bool isLoop = false, params Scope[] sharedScopes)
    {
        var scope = new Scope([], sharedScopes);

        if (startOfBlockSign is not TokenType.ErrorOrEmpty)
        {
            var startOfBlock = tokens.Dequeue();
            if (startOfBlock.Type != startOfBlockSign) return new ErrorInfo($"Unexpected token type {startOfBlock.Type}! Requiring type {startOfBlockSign}.", startOfBlock);
        }

        while (tokens.Count > 0 && (endOfBlockSign is TokenType.ErrorOrEmpty || tokens.Peek().Type != endOfBlockSign))
        {
            var statement = ParseStatement(tokens, scope, isLoop);
            if (statement.IsT1) return statement.AsT1;
            scope.Add(statement.AsT0);
        }

        if (endOfBlockSign is not TokenType.ErrorOrEmpty)
        {
            var endOfBlock = tokens.Dequeue();
            if (endOfBlock.Type != endOfBlockSign) return new ErrorInfo($"Unexpected token type {endOfBlock.Type}! Requiring type {endOfBlockSign}.", endOfBlock);
        }

        return scope;
    }

    private static OneOf<Statement, ErrorInfo> ParseStatement(Queue<Token> tokens, Scope scope, bool isLoop = false)
    {
        var token = tokens.Peek();

        var isBlock = false;

        var statement = token.Type switch
        {
            TokenType.Var or TokenType.Type => ParseDeclaration(tokens),
            TokenType.Identifier => ParseAssignment(tokens),
            TokenType.If => ParseIf(tokens, scope, out isBlock, isLoop),
            TokenType.While => ParseWhile(tokens, scope, out isBlock),
            TokenType.LeftCurlyBrace => ParseStatementList(tokens, TokenType.LeftCurlyBrace, TokenType.RightCurlyBrace, isLoop, scope).TryPickT0(out var bodyScope, out var error) ? new Body(bodyScope) : error,
            TokenType.Break => ParseBreak(tokens, isLoop),
            TokenType.Continue => ParseContinue(tokens, isLoop),
            _ => new ErrorInfo($"Unexpected token type {token.Type} at start of statement!", token)
        };

        if (statement.IsT1) return statement.AsT1;

        if (isBlock || token.Type is TokenType.LeftCurlyBrace) return statement.AsT0;

        var endOfLine = tokens.Dequeue();
        if (endOfLine.Type is not TokenType.EndOfStatement) return new ErrorInfo($"Unexpected token type {endOfLine.Type}! Requiring type {TokenType.EndOfStatement}.", endOfLine);

        return statement.AsT0;
    }

    private static OneOf<Statement, ErrorInfo> ParseDeclaration(Queue<Token> tokens)
    {
        var type = tokens.Dequeue();

        var identifier = tokens.Dequeue();
        if (identifier.Type is not TokenType.Identifier) return new ErrorInfo($"Unexpected token type {identifier.Type}! Requiring type {TokenType.Identifier}.", identifier);

        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type {token.Type}! Requiring type {TokenType.Assignment}.", token);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new(identifier.Value.AsT0, new(type.Value.AsT2.GetTypeName())), expression.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseAssignment(Queue<Token> tokens)
    {
        var identifier = tokens.Dequeue();

        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type {token.Type}! Requiring type {TokenType.Assignment}.", token);

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
            if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type {rightParenthesis.Type}! Requiring type {TokenType.RightParenthesis}.", rightParenthesis);

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

            if (heap.IsT1) return new ErrorInfo($"Empty heap!", new());

            var term = ParseArithmetic(tokens, Calculations.TestOperations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0)));
            if (term.IsT1) return term.AsT1;

            return term.AsT0;
        }
        return heap.TryPickT0(out var some, out var none) ? some : none;
    }

    private static OneOf<Statement, ErrorInfo> ParseIf(Queue<Token> tokens, Scope scope, out bool isBlock, bool isLoop = false)
    {
        _ = tokens.Dequeue();

        isBlock = true;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return new ErrorInfo($"Unexpected token type {leftParenthesis.Type}! Requiring type {TokenType.LeftParenthesis}.", leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return new ErrorInfo($"Unexpected token type {condition.AsT0.Value.AsT2.Operator}! Must return a boolean.", new()); // TODO: improve error message (token)

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type {rightParenthesis.Type}! Requiring type {TokenType.RightParenthesis}.", rightParenthesis);

        var body = ParseStatement(tokens, scope, isLoop);
        if (body.IsT1) return body.AsT1;

        var next = tokens.Peek();
        if (next.Type is not TokenType.Else) return new If(condition.AsT0, (Body)body.AsT0);

        _ = tokens.Dequeue();

        var elseBody = ParseStatement(tokens, scope, isLoop);
        if (elseBody.IsT1) return elseBody.AsT1;

        return new If(condition.AsT0, body.AsT0, elseBody.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseWhile(Queue<Token> tokens, Scope scope, out bool isBlock)
    {
        _ = tokens.Dequeue();

        isBlock = true;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return new ErrorInfo($"Unexpected token type {leftParenthesis.Type}! Requiring type {TokenType.LeftParenthesis}.", leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return new ErrorInfo($"Unexpected token type {condition.AsT0.Value.AsT2.Operator}! Must return a boolean.", new()); // TODO: improve error message (token)

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return new ErrorInfo($"Unexpected token type {rightParenthesis.Type}! Requiring type {TokenType.RightParenthesis}.", rightParenthesis);

        var body = ParseStatement(tokens, scope, true);
        if (body.IsT1) return body.AsT1;

        return new While(condition.AsT0, body.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseBreak(Queue<Token> tokens, bool isLoop)
    {
        _ = tokens.Dequeue();

        if (!isLoop) return new ErrorInfo($"Break must be inside a loop!", new()); // TODO: improve error message (token)

        return new Break();
    }

    private static OneOf<Statement, ErrorInfo> ParseContinue(Queue<Token> tokens, bool isLoop)
    {
        _ = tokens.Dequeue();
        if (!isLoop) return new ErrorInfo($"Continue must be inside a loop!", new()); // TODO: improve error message (token)
        return new Continue();
    }
}
