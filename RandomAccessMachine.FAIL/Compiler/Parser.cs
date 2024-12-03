using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.ElementTree;
using RandomAccessMachine.FAIL.Specification;
using RandomAccessMachine.FAIL.Specification.Operators;

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

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, new(type.Value.AsT2.GetTypeName())), expression.AsT0, true);
    }

    private static OneOf<Statement, ErrorInfo> ParseAssignment(Queue<Token> tokens)
    {
        var identifier = tokens.Dequeue();

        var token = tokens.Peek();

        if (token.Type is TokenType.SelfAssignment) return ParseSelfAssignment(identifier, tokens);
        if (token.Type is TokenType.IncrementalOperator) return ParseIncrementalAssignment(identifier, tokens);
        if (token.Type is TokenType.LeftSquareBrace) return ParseArrayModification(identifier, tokens);

        _ = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type {token.Type}! Requiring type {TokenType.Assignment}.", token);

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, new("var")), expression.AsT0); // TODO: validate type later
    }

    private static OneOf<Statement, ErrorInfo> ParseSelfAssignment(Token identifier, Queue<Token> tokens)
    {
        var selfAssignment = tokens.Dequeue();

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, new("var")), new(new BinaryOperation(selfAssignment.Value.AsT4.GetBinaryOperator(), new Expression(new Identifier(identifier.Value.AsT0, new("var"))), expression.AsT0)));
    }

    private static OneOf<Statement, ErrorInfo> ParseIncrementalAssignment(Token identifier, Queue<Token> tokens)
    {
        var incrementalOperator = tokens.Dequeue();
        return new Assignment(new Identifier(identifier.Value.AsT0, new("var")), new(new BinaryOperation(incrementalOperator.Value.AsT5.GetBinaryOperator(), new Expression(new Identifier(identifier.Value.AsT0, new("var"))), new Expression(new Number(1)))));
    }

    private static OneOf<Statement, ErrorInfo> ParseArrayModification(Token identifier, Queue<Token> tokens)
    {
        var leftSquareBrace = tokens.Dequeue();
        if (leftSquareBrace.Type is not TokenType.LeftSquareBrace) return new ErrorInfo($"Unexpected token type {leftSquareBrace.Type}! Requiring type {TokenType.LeftSquareBrace}.", leftSquareBrace);

        var index = ParseArithmetic(tokens, new None());
        if (index.IsT1) return index.AsT1;

        var rightSquareBrace = tokens.Dequeue();
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return new ErrorInfo($"Unexpected token type {rightSquareBrace.Type}! Requiring type {TokenType.RightSquareBrace}.", rightSquareBrace);

        var assignment = tokens.Dequeue();
        if (assignment.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type {assignment.Type}! Requiring type {TokenType.Assignment}.", assignment);

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new ArrayAccessor(new(identifier.Value.AsT0, new("var")), index.AsT0), expression.AsT0); // TODO: validate type later
    }

    private static OneOf<Statement, ErrorInfo> ParseTypeInitialization(Token identifier, Queue<Token> tokens)
    {
        _ = tokens.Dequeue();

        var type = tokens.Dequeue();
        if (type.Type is not TokenType.Type) return new ErrorInfo($"Unexpected token type {type.Type}! Requiring type {TokenType.Type}.", type);

        var token = tokens.Peek();

        if (token.Type is TokenType.LeftSquareBrace) return ParseArrayInitialization(type, identifier, tokens);

        return new ErrorInfo($"Unexpected token type {token.Type}! Requiring type {TokenType.LeftSquareBrace}.", token);
    }

    private static OneOf<Statement, ErrorInfo> ParseArrayInitialization(Token type, Token identifier, Queue<Token> tokens)
    {
        _ = tokens.Dequeue();

        var size = ParseArithmetic(tokens, Calculations.Term, new None()); // TODO: validate type later
        if (size.IsT1 || !size.AsT0.Value.IsT1) return size.AsT1;

        var rightSquareBrace = tokens.Dequeue();
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return new ErrorInfo($"Unexpected token type {rightSquareBrace.Type}! Requiring type {TokenType.RightSquareBrace}.", rightSquareBrace);

        return new Assignment(new Identifier(identifier.Value.AsT0, new("var")), new(new TypeInitialization(new(new ElementTree.Array(new("int"), size.AsT0.Value.AsT1.Value))))); // TODO: validate type later, special rules for arrays
    }

    private static OneOf<Expression, ErrorInfo> ParseArithmetic(Queue<Token> tokens, OneOf<Expression, None> heap) => ParseArithmetic(tokens, CalculationsExtensions.All, heap);
    private static OneOf<Expression, ErrorInfo> ParseArithmetic(Queue<Token> tokens, Calculations calculations, OneOf<Expression, None> heap)
    {
        if (calculations.HasFlag(Calculations.Term))
        {
            var result = ParseTerm(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.TryPickT0(out var expression, out var none) ? expression : none.AsT0;
        }

        if (calculations.HasFlag(Calculations.DotCalculations))
        {
            var result = ParseDotCalculation(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.TryPickT0(out var expression, out var none) ? expression : none.AsT0;
        }

        if (calculations.HasFlag(Calculations.StrokeCalculations))
        {
            var result = ParseStrokeCalculation(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.TryPickT0(out var expression, out var none) ? expression : none.AsT0;
        }

        if (calculations.HasFlag(Calculations.TestOperations))
        {
            var result = ParseTestOperation(tokens, heap);
            if (result.IsT2) return result.AsT2;
            heap = result.TryPickT0(out var expression, out var none) ? expression : none.AsT0;
        }

        if (heap.IsT1) return new ErrorInfo($"Empty heap for arithmetic calculation!", new());

        return heap.AsT0;
    }

    private static OneOf<Expression, None, ErrorInfo> ParseTerm(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.Identifier)
        {
            _ = tokens.Dequeue();

            if (tokens.Peek().Type is TokenType.LeftSquareBrace) return ParseArrayAccess(token, tokens, heap);

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

    private static OneOf<Expression, None, ErrorInfo> ParseArrayAccess(Token identifier, Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        _ = tokens.Dequeue();

        var index = ParseArithmetic(tokens, heap);
        if (index.IsT1) return index.AsT1;

        var rightSquareBrace = tokens.Dequeue();
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return new ErrorInfo($"Unexpected token type {rightSquareBrace.Type}! Requiring type {TokenType.RightSquareBrace}.", rightSquareBrace);

        return new Expression(new ArrayAccessor(new(identifier.Value.AsT0, new("var")), index.AsT0)); // TODO: validate type later
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
