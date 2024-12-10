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

    private static OneOf<Scope, ErrorInfo> ParseStatementList(Queue<Token> tokens, TokenType startOfBlockSign = TokenType.ErrorOrEmpty, TokenType endOfBlockSign = TokenType.ErrorOrEmpty, TokenType endOfStatementSign = TokenType.EndOfStatement, bool isLoop = false, bool isFunction = false, params Scope[] sharedScopes)
    {
        var scope = new Scope([], sharedScopes);

        if (startOfBlockSign is not TokenType.ErrorOrEmpty)
        {
            var startOfBlock = tokens.Dequeue();
            if (startOfBlock.Type != startOfBlockSign) return ErrorInfo.WrongBlockStart(startOfBlockSign, startOfBlock);
        }

        while (tokens.Count > 0 && (endOfBlockSign is TokenType.ErrorOrEmpty || tokens.Peek().Type != endOfBlockSign))
        {
            var statement = ParseStatement(tokens, scope, out var isBlock, isLoop, isFunction);
            if (statement.IsT1) return statement.AsT1;

            if (!isBlock && tokens.Count > 0 && tokens.Peek().Type != endOfBlockSign)
            {
                var endOfStatement = tokens.Dequeue();
                if (endOfStatement.Type != endOfStatementSign) return ErrorInfo.WrongStatementEnd(endOfStatementSign, endOfStatement);
            }


            scope.Add(statement.AsT0);
        }

        if (endOfBlockSign is not TokenType.ErrorOrEmpty)
        {
            var endOfBlock = tokens.Dequeue();
            if (endOfBlock.Type != endOfBlockSign) return ErrorInfo.WrongBlockEnd(endOfBlockSign, endOfBlock);
        }

        return scope;
    }

    private static OneOf<Statement, ErrorInfo> ParseStatement(Queue<Token> tokens, Scope scope, out bool isBlock, bool isLoop = false, bool isFunction = false)
    {
        var token = tokens.Peek();

        var internalIsBlock = false;

        var statement = token.Type switch
        {
            TokenType.Var or TokenType.Type => ParseDeclaration(tokens, isFunction),
            TokenType.Identifier => ParseAssignment(tokens, scope),
            TokenType.If => ParseIf(tokens, scope, out internalIsBlock, isLoop, isFunction),
            TokenType.While => ParseWhile(tokens, scope, out internalIsBlock, isFunction),
            TokenType.LeftCurlyBrace => ParseStatementList(tokens, TokenType.LeftCurlyBrace, TokenType.RightCurlyBrace, TokenType.EndOfStatement, isLoop, isFunction, scope).TryPickT0(out var bodyScope, out var error) ? new Body(bodyScope, token) : error,
            TokenType.Break => ParseBreak(tokens, isLoop),
            TokenType.Continue => ParseContinue(tokens, isLoop),
            TokenType.FunctionDeclaration => ParseFunction(tokens, scope, out internalIsBlock),
            TokenType.Return => ParseReturn(tokens, isFunction),
            TokenType.Number => ParseArithmetic(tokens, new None()).TryPickT0(out var value, out var error) ? value : error,
            _ => ErrorInfo.WrongStatementStart(token)
        };

        isBlock = internalIsBlock;

        return statement;
    }

    private static OneOf<Statement, ErrorInfo> ParseDeclaration(Queue<Token> tokens, bool isFunction)
    {
        var type = tokens.Dequeue();

        var identifier = tokens.Dequeue();
        if (identifier.Type is not TokenType.Identifier) return ErrorInfo.DeclarationMissingIdentifier(identifier);

        if (tokens.Peek().Type is not TokenType.Assignment && isFunction) return new ArgumentDefinition(new(identifier.Value.AsT0, identifier), new(type.Value.AsT2.GetTypeName(), type), identifier);

        var token = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return ErrorInfo.DeclarationNeedingInitialization(token);

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, identifier, new(type.Value.AsT2.GetTypeName(), type)), expression.AsT0, identifier, true);
    }

    private static OneOf<Statement, ErrorInfo> ParseAssignment(Queue<Token> tokens, Scope scope)
    {
        var identifier = tokens.Dequeue();
        var token = tokens.Peek();

        if (token.Type is not TokenType.LeftParenthesis && scope.Search(x => x is Assignment assignment && assignment.Identifier.Match(x => x.Name, x => x.Identifier.Name) == identifier.Value.AsT0) is null) return ErrorInfo.VariableNeedsDeclaration(identifier);

        if (token.Type is TokenType.SelfAssignment) return ParseSelfAssignment(identifier, tokens);
        if (token.Type is TokenType.IncrementalOperator) return ParseIncrementalAssignment(identifier, tokens);
        if (token.Type is TokenType.LeftSquareBrace) return ParseArrayModification(identifier, tokens);
        if (token.Type is TokenType.LeftParenthesis) return ParseFunctionCall(identifier, tokens).TryPickT0(out var value, out var error) ? value : error.AsT1;

        _ = tokens.Dequeue();
        if (token.Type is not TokenType.Assignment) return ErrorInfo.AssignmentMissingOperator(token);

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), expression.AsT0, identifier); // TODO: validate type later
    }

    private static OneOf<Statement, ErrorInfo> ParseSelfAssignment(Token identifier, Queue<Token> tokens)
    {
        var selfAssignment = tokens.Dequeue();

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), new(new BinaryOperation(selfAssignment.Value.AsT4.GetBinaryOperator(), new Expression(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), identifier), expression.AsT0, selfAssignment), identifier), identifier);
    }

    private static OneOf<Statement, ErrorInfo> ParseIncrementalAssignment(Token identifier, Queue<Token> tokens)
    {
        var incrementalOperator = tokens.Dequeue();
        return new Assignment(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), new(new BinaryOperation(incrementalOperator.Value.AsT5.GetBinaryOperator(), new Expression(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), identifier), new Expression(new Number(1, default), identifier), incrementalOperator), identifier), identifier);
    }

    private static OneOf<Statement, ErrorInfo> ParseArrayModification(Token identifier, Queue<Token> tokens)
    {
        _ = tokens.Dequeue();

        var index = ParseArithmetic(tokens, new None());
        if (index.IsT1) return index.AsT1;

        var rightSquareBrace = tokens.Dequeue();
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return ErrorInfo.ArrayAccessorMissingClosingBrace(rightSquareBrace);

        var assignment = tokens.Dequeue();
        if (assignment.Type is not TokenType.Assignment) return ErrorInfo.AssignmentMissingOperator(assignment);

        if (tokens.Peek().Type is TokenType.New) return ParseTypeInitialization(identifier, tokens);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Assignment(new ArrayAccessor(new(identifier.Value.AsT0, identifier, new("var", default)), index.AsT0, assignment), expression.AsT0, assignment); // TODO: validate type later
    }

    private static OneOf<Statement, ErrorInfo> ParseTypeInitialization(Token identifier, Queue<Token> tokens)
    {
        _ = tokens.Dequeue();

        var type = tokens.Dequeue();
        if (type.Type is not TokenType.Type) return ErrorInfo.TypeNeededForInitialization(type);

        var token = tokens.Peek();
        if (token.Type is TokenType.LeftSquareBrace) return ParseArrayInitialization(type, identifier, tokens);

        return ErrorInfo.WrongToken(TokenType.LeftSquareBrace, token);
    }

    private static OneOf<Statement, ErrorInfo> ParseArrayInitialization(Token type, Token identifier, Queue<Token> tokens)
    {
        _ = tokens.Dequeue();

        var size = ParseArithmetic(tokens, Calculations.Term, new None()); // TODO: validate type later
        if (size.IsT1 || !size.AsT0.Value.IsT1) return size.AsT1;

        var rightSquareBrace = tokens.Dequeue();
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return ErrorInfo.ArrayAccessorMissingClosingBrace(rightSquareBrace);

        return new Assignment(new Identifier(identifier.Value.AsT0, identifier, new("var", default)), new(new TypeInitialization(new(new ElementTree.Array(new("int", default), size.AsT0.Value.AsT1.Value, rightSquareBrace), type), type), identifier), identifier); // TODO: validate type later, special rules for arrays
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

        if (heap.IsT1) return ErrorInfo.InvalidExpression(tokens.Peek());

        return heap.AsT0;
    }

    private static OneOf<Expression, None, ErrorInfo> ParseTerm(Queue<Token> tokens, OneOf<Expression, None> heap)
    {
        var token = tokens.Peek();

        if (token.Type is TokenType.Identifier)
        {
            _ = tokens.Dequeue();

            if (tokens.Peek().Type is TokenType.LeftSquareBrace) return ParseArrayAccess(token, tokens, heap);
            if (tokens.Peek().Type is TokenType.LeftParenthesis) return ParseFunctionCall(token, tokens);

            return new Expression(new Identifier(token.Value.AsT0, token, new("var", default)), token); // TODO: validate type later
        }

        if (token.Type is TokenType.LeftParenthesis)
        {
            _ = tokens.Dequeue();

            var expression = ParseArithmetic(tokens, heap);
            if (expression.IsT1) return expression.AsT1;

            var rightParenthesis = tokens.Dequeue();
            if (rightParenthesis.Type is not TokenType.RightParenthesis) return ErrorInfo.ClosingParenthesisMissing(rightParenthesis);

            return expression.AsT0;
        }

        if (token.Type is TokenType.Number)
        {
            var number = tokens.Dequeue();

            return new Expression(new Number(token.Value.AsT1, number), number);
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

            if (heap.IsT1) return ErrorInfo.InvalidExpression(tokens.Peek());

            var term = ParseArithmetic(tokens, Calculations.DotCalculations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0, token), token));
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

            if (heap.IsT1) return ErrorInfo.InvalidExpression(tokens.Peek());

            var term = ParseArithmetic(tokens, Calculations.StrokeCalculations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0, token), token));
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

            if (heap.IsT1) return ErrorInfo.InvalidExpression(tokens.Peek());

            var term = ParseArithmetic(tokens, Calculations.TestOperations.SelfAndBelow(), new Expression(new BinaryOperation(token.Value.AsT3, heap.AsT0, right.AsT0, token), token));
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
        if (rightSquareBrace.Type is not TokenType.RightSquareBrace) return ErrorInfo.ArrayAccessorMissingClosingBrace(rightSquareBrace);

        return new Expression(new ArrayAccessor(new(identifier.Value.AsT0, identifier, new("var", default)), index.AsT0, rightSquareBrace), identifier); // TODO: validate type later
    }

    private static OneOf<Statement, ErrorInfo> ParseIf(Queue<Token> tokens, Scope scope, out bool isBlock, bool isLoop, bool isFunction)
    {
        var token = tokens.Dequeue();

        isBlock = true;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return ErrorInfo.IfMissingOpeningParenthesis(leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return ErrorInfo.ConditionMustReturnBoolean(condition.AsT0.Value.AsT2.Token);

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return ErrorInfo.ClosingParenthesisMissing(rightParenthesis);

        var body = ParseStatement(tokens, scope, out _, isLoop, isFunction);
        if (body.IsT1) return body.AsT1;

        var next = tokens.Peek();
        if (next.Type is not TokenType.Else) return new If(condition.AsT0, (Body)body.AsT0, token);

        _ = tokens.Dequeue();

        var elseBody = ParseStatement(tokens, scope, out _, isLoop, isFunction);
        if (elseBody.IsT1) return elseBody.AsT1;

        return new If(condition.AsT0, body.AsT0, token, elseBody.AsT0);
    }

    private static OneOf<Statement, ErrorInfo> ParseWhile(Queue<Token> tokens, Scope scope, out bool isBlock, bool isFunction)
    {
        var token = tokens.Dequeue();

        isBlock = true;

        var leftParenthesis = tokens.Dequeue();
        if (leftParenthesis.Type is not TokenType.LeftParenthesis) return ErrorInfo.WhileMissingOpeningParenthesis(leftParenthesis);

        var condition = ParseArithmetic(tokens, new None());
        if (condition.IsT1) return condition.AsT1;
        if (condition.AsT0.Value.IsT2 && !condition.AsT0.Value.AsT2.Operator.IsTestOperation()) return ErrorInfo.ConditionMustReturnBoolean(condition.AsT0.Value.AsT2.Token);

        var rightParenthesis = tokens.Dequeue();
        if (rightParenthesis.Type is not TokenType.RightParenthesis) return ErrorInfo.ClosingParenthesisMissing(rightParenthesis);

        var body = ParseStatement(tokens, scope, out _, true, isFunction);
        if (body.IsT1) return body.AsT1;

        return new While(condition.AsT0, body.AsT0, token);
    }

    private static OneOf<Statement, ErrorInfo> ParseBreak(Queue<Token> tokens, bool isLoop)
    {
        var token = tokens.Dequeue();

        if (!isLoop) return ErrorInfo.BreakMustBeInsideLoop(token);

        return new Break(token);
    }

    private static OneOf<Statement, ErrorInfo> ParseContinue(Queue<Token> tokens, bool isLoop)
    {
        var token = tokens.Dequeue();

        if (!isLoop) return ErrorInfo.ContinueMustBeInsideLoop(token);

        return new Continue(token);
    }

    private static OneOf<Statement, ErrorInfo> ParseFunction(Queue<Token> tokens, Scope scope, out bool isBlock)
    {
        _ = tokens.Dequeue();

        isBlock = true;

        var identifier = tokens.Dequeue();
        if (identifier.Type is not TokenType.Identifier) return ErrorInfo.FunctionNeedingIdentifier(identifier);

        var arguments = ParseStatementList(tokens, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.Comma, false, true, scope);
        if (arguments.IsT1) return arguments.AsT1; // TODO: check, if only argument definitions are present and their types are explicitly defined
        // TODO: check, if identifiers are unique (in their own scope and parent scopes)
        if (tokens.Peek().Type is TokenType.ReturnDeclaration)
        {
            _ = tokens.Dequeue();

            var returnType = tokens.Dequeue();
            if (returnType.Type is not TokenType.Type) return ErrorInfo.FunctionWithReturnNeedingReturnType(returnType);

            var returnBody = ParseStatement(tokens, scope, out _, false, true);
            if (returnBody.IsT1) return returnBody.AsT1;
            // TODO: Validate if there are all necessary return statements and if they are of the correct type
            return new FunctionDeclaration(new(identifier.Value.AsT0, identifier), arguments.AsT0, (Body)returnBody.AsT0, identifier, new(returnType.Value.AsT2.GetTypeName(), returnType));
        }

        var body = ParseStatement(tokens, scope, out _, false, true);
        if (body.IsT1) return body.AsT1;

        return new FunctionDeclaration(new(identifier.Value.AsT0, identifier), arguments.AsT0, (Body)body.AsT0, identifier);
    }

    private static OneOf<Statement, ErrorInfo> ParseReturn(Queue<Token> tokens, bool isFunction)
    {
        var token = tokens.Dequeue();
        if (!isFunction) return ErrorInfo.ReturnMustBeInsideFunction(token);

        var expression = ParseArithmetic(tokens, new None());
        if (expression.IsT1) return expression.AsT1;

        return new Return(expression.AsT0, token);
    }

    private static OneOf<Expression, None, ErrorInfo> ParseFunctionCall(Token identifier, Queue<Token> tokens)
    {
        var parameters = ParseStatementList(tokens, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.Comma, false, false); // TODO: validate parameters
        if (parameters.IsT1) return parameters.AsT1;

        return new Expression(new FunctionCall(new(identifier.Value.AsT0, identifier), parameters.AsT0, identifier), identifier);
    }
}
