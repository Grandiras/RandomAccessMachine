using OneOf;
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

        var expression = ParseExpression(tokens);
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new(identifier.Value.AsT0, new(type.Value.AsT2.GetTypeName())), expression.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseAssignment(Queue<Token> tokens, Token identifier)
    {
        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

        var expression = ParseExpression(tokens);
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new(identifier.Value.AsT0, new("var")), expression.AsT0); // TODO: validate type later
    }

    private static OneOf<Expression, ErrorInfo> ParseExpression(Queue<Token> tokens)
    {
        var token = tokens.Dequeue();

        if (token.Type is TokenType.LeftParenthesis)
        {
            var expression = ParseExpression(tokens);
            if (expression.IsT1) return expression.AsT1;

            var rightParenthesis = tokens.Dequeue();
            return rightParenthesis.Type is not TokenType.RightParenthesis
                ? new ErrorInfo($"Unexpected token type: {rightParenthesis.Type}", rightParenthesis)
                : expression;
        }

        if (token.Type is TokenType.Number)
        {
            if (tokens.Peek().Type is TokenType.BinaryOperator)
            {
                var binaryOperator = tokens.Dequeue();
                var rightExpression = ParseExpression(tokens);
                if (rightExpression.IsT1) return rightExpression.AsT1;

                return new Expression(new BinaryOperation(binaryOperator.Value.AsT3, new(new Number(token.Value.AsT1)), rightExpression.AsT0));
            }

            return new Expression(new Number(token.Value.AsT1));
        }

        if (token.Type is TokenType.Identifier)
        {
            if (tokens.Peek().Type is TokenType.BinaryOperator)
            {
                var binaryOperator = tokens.Dequeue();
                var rightExpression = ParseExpression(tokens);
                if (rightExpression.IsT1) return rightExpression.AsT1;

                return new Expression(new BinaryOperation(binaryOperator.Value.AsT3, new(new Identifier(token.Value.AsT0, new("var"))), rightExpression.AsT0)); // TODO: validate type later
            }
            return new Expression(new Identifier(token.Value.AsT0, new("var"))); // TODO: validate type later
        }

        return new ErrorInfo($"Unexpected token type: {token.Type}", token);
    }
}
