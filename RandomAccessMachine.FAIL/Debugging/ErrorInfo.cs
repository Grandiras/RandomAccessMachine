using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Debugging;
public record struct ErrorInfo(string Message, ErrorType Type, ErrorCode ErrorCode, Token Token)
{
    public override readonly string ToString() => $"{Type.GetErrorTypeName()} (E{(int)ErrorCode}): {Message}\n  at {Token.LineNumber}:{Token.ColumnNumber} ({Token.Length})";

    public static ErrorInfo UnexpectedCharacter(char character, uint lineNumber, uint columnNumber) => new(new($"Unexpected character `{character}`!"), ErrorType.Syntax, ErrorCode.UnexpectedCharacter, new("", TokenType.ErrorOrEmpty, lineNumber, columnNumber, 1, character.ToString()));
    public static ErrorInfo UnexpectedEndOfCode(uint lineNumber, uint columnNumber) => new(new("Unexpected end of code!"), ErrorType.Syntax, ErrorCode.UnexpectedEndOfCode, new("", TokenType.ErrorOrEmpty, lineNumber, columnNumber, 0, ""));

    public static ErrorInfo WrongStatementStart(Token actual) => new(new($"Unexpected token type `{actual.Type}` at start of statement!"), ErrorType.Syntax, ErrorCode.WrongStatementStart, actual);
    public static ErrorInfo WrongStatementEnd(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of statement, but found `{actual.Type}`!"), ErrorType.Syntax, ErrorCode.WrongStatementEnd, actual);

    public static ErrorInfo WrongBlockStart(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of block, but found `{actual.Type}`!"), ErrorType.Syntax, ErrorCode.WrongBlockStart, actual);
    public static ErrorInfo WrongBlockEnd(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of block, but found `{actual.Type}`!"), ErrorType.Syntax, ErrorCode.WrongBlockEnd, actual);

    public static ErrorInfo WrongToken(TokenType expected, Token actual) => new(new($"Expected `{expected}`, but found `{actual.Type}`!"), ErrorType.Syntax, ErrorCode.WrongToken, actual);

    public static ErrorInfo DeclarationMissingIdentifier(Token actual) => new(new("Declaration is missing an identifier!"), ErrorType.Semantic, ErrorCode.DeclarationMissingIdentifier, actual);
    public static ErrorInfo DeclarationNeedingInitialization(Token actual) => new(new("Declaration needs initialization!"), ErrorType.Semantic, ErrorCode.DeclarationNeedingInitialization, actual);

    public static ErrorInfo AssignmentMissingOperator(Token actual) => new(new("Incomplete assignment! Missing assignment operator."), ErrorType.Syntax, ErrorCode.AssignmentMissingOperator, actual);

    public static ErrorInfo ArrayAccessorMissingClosingBrace(Token actual) => new(new("Array accessor is missing closing square brace!"), ErrorType.Syntax, ErrorCode.ArrayAccessorMissingClosingBrace, actual);
    public static ErrorInfo ClosingParenthesisMissing(Token actual) => new(new("Closing parenthesis missing!"), ErrorType.Syntax, ErrorCode.ClosingParenthesisMissing, actual);

    public static ErrorInfo TypeNeededForInitialization(Token actual) => new(new("Type needed for initialization!"), ErrorType.Semantic, ErrorCode.TypeNeededForInitialization, actual);

    public static ErrorInfo InvalidExpression(Token actual) => new(new("Invalid expression!"), ErrorType.Syntax, ErrorCode.InvalidExpression, actual);

    public static ErrorInfo IfMissingOpeningParenthesis(Token actual) => new(new("If statement is missing opening parenthesis for its condition!"), ErrorType.Syntax, ErrorCode.IfMissingOpeningParenthesis, actual);
    public static ErrorInfo WhileMissingOpeningParenthesis(Token actual) => new(new("While statement is missing opening parenthesis for its condition!"), ErrorType.Syntax, ErrorCode.WhileMissingOpeningParenthesis, actual);

    public static ErrorInfo ConditionMustReturnBoolean(Token actual) => new(new("Condition must return a boolean value!"), ErrorType.Type, ErrorCode.ConditionMustReturnBoolean, actual);

    public static ErrorInfo BreakMustBeInsideLoop(Token actual) => new(new("Break statement must be inside a loop!"), ErrorType.Syntax, ErrorCode.BreakMustBeInsideLoop, actual);
    public static ErrorInfo ContinueMustBeInsideLoop(Token actual) => new(new("Continue statement must be inside a loop!"), ErrorType.Syntax, ErrorCode.ContinueMustBeInsideLoop, actual);

    public static ErrorInfo FunctionNeedingIdentifier(Token actual) => new(new("Function declaration is missing an identifier!"), ErrorType.Semantic, ErrorCode.FunctionNeedingIdentifier, actual);
    public static ErrorInfo FunctionWithReturnNeedingReturnType(Token actual) => new(new("Function declaration with return type needs a return type!"), ErrorType.Semantic, ErrorCode.FunctionWithReturnNeedingReturnType, actual);

    public static ErrorInfo ReturnMustBeInsideFunction(Token actual) => new(new("Return statement must be inside a function!"), ErrorType.Syntax, ErrorCode.ReturnMustBeInsideFunction, actual);

    public static ErrorInfo FunctionNotFound(Token actual) => new(new($"Function `{actual.Value.AsT0}` not found!"), ErrorType.Semantic, ErrorCode.FunctionNotFound, actual);
    public static ErrorInfo FunctionArgumentMismatch(Token actual) => new(new($"Function `{actual.Value.AsT0}` has mismatched arguments!"), ErrorType.Semantic, ErrorCode.FunctionArgumentMismatch, actual);
}

public enum ErrorType
{
    Syntax,
    Semantic,
    Type,
}

public static class ErrorTypeExtensions
{
    public static string GetErrorTypeName(this ErrorType type) => type switch
    {
        ErrorType.Syntax => "Syntax Error",
        ErrorType.Semantic => "Semantic Error",
        ErrorType.Type => "Type Error",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}

public enum ErrorCode
{
    UnexpectedCharacter = 1001,
    UnexpectedEndOfCode,
    WrongStatementStart,
    WrongStatementEnd,
    WrongBlockStart,
    WrongBlockEnd,
    WrongToken,
    DeclarationMissingIdentifier,
    DeclarationNeedingInitialization,
    AssignmentMissingOperator,
    ArrayAccessorMissingClosingBrace,
    ClosingParenthesisMissing,
    TypeNeededForInitialization,
    InvalidExpression,
    IfMissingOpeningParenthesis,
    WhileMissingOpeningParenthesis,
    ConditionMustReturnBoolean,
    BreakMustBeInsideLoop,
    ContinueMustBeInsideLoop,
    FunctionNeedingIdentifier,
    FunctionWithReturnNeedingReturnType,
    ReturnMustBeInsideFunction,
    FunctionNotFound,
    FunctionArgumentMismatch,
}