using RandomAccessMachine.FAIL.Specification;

namespace RandomAccessMachine.FAIL.Debugging;
public record struct ErrorInfo(string Message, Token Token)
{
    public override readonly string ToString() => $"Error at line {Token.LineNumber}, column {Token.ColumnNumber}: {Message}";

    public static ErrorInfo WrongStatementStart(Token actual) => new(new($"Unexpected token type `{actual.Type}` at start of statement!"), actual);
    public static ErrorInfo WrongStatementEnd(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of statement, but found `{actual.Type}`!"), actual);

    public static ErrorInfo WrongBlockStart(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of block, but found `{actual.Type}`!"), actual);
    public static ErrorInfo WrongBlockEnd(TokenType expected, Token actual) => new(new($"Expected `{expected}` at end of block, but found `{actual.Type}`!"), actual);

    public static ErrorInfo WrongToken(TokenType expected, Token actual) => new(new($"Expected `{expected}`, but found `{actual.Type}`!"), actual);

    public static ErrorInfo DeclarationMissingIdentifier(Token actual) => new(new("Declaration is missing an identifier!"), actual);
    public static ErrorInfo DeclarationNeedingInitialization(Token actual) => new(new("Declaration needs initialization!"), actual);

    public static ErrorInfo AssignmentMissingOperator(Token actual) => new(new("Incomplete assignment! Missing assignment operator."), actual);

    public static ErrorInfo ArrayAccessorMissingClosingBrace(Token actual) => new(new("Array accessor is missing closing square brace!"), actual);
    public static ErrorInfo ClosingParenthesisMissing(Token actual) => new(new("Closing parenthesis missing!"), actual);

    public static ErrorInfo TypeNeededForInitialization(Token actual) => new(new("Type needed for initialization!"), actual);

    public static ErrorInfo InvalidExpression(Token actual) => new(new("Invalid expression!"), actual);

    public static ErrorInfo IfMissingOpeningParenthesis(Token actual) => new(new("If statement is missing opening parenthesis for its condition!"), actual);
    public static ErrorInfo WhileMissingOpeningParenthesis(Token actual) => new(new("While statement is missing opening parenthesis for its condition!"), actual);

    public static ErrorInfo ConditionMustReturnBoolean(Token actual) => new(new("Condition must return a boolean value!"), actual);

    public static ErrorInfo BreakMustBeInsideLoop(Token actual) => new(new("Break statement must be inside a loop!"), actual);
    public static ErrorInfo ContinueMustBeInsideLoop(Token actual) => new(new("Continue statement must be inside a loop!"), actual);

    public static ErrorInfo FunctionNeedingIdentifier(Token actual) => new(new("Function declaration is missing an identifier!"), actual);
    public static ErrorInfo FunctionWithReturnNeedingReturnType(Token actual) => new(new("Function declaration with return type needs a return type!"), actual);

    public static ErrorInfo ReturnMustBeInsideFunction(Token actual) => new(new("Return statement must be inside a function!"), actual);
}