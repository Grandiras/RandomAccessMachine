using OneOf;
using RandomAccessMachine.FAIL.Specification;
using RandomAccessMachine.FAIL.Specification.Operators;

namespace RandomAccessMachine.FAIL.ElementTree;
public record Statement(Token Token);

public record Assignment(OneOf<Identifier, ArrayAccessor> Identifier, Expression Expression, Token Token, bool IsInitial = false) : Statement(Token)
{
    public override string ToString() => $"{Identifier.Match(x => x.ToString(), x => x.ToString())} = {Expression}";
}

public record Identifier(string Name, Token Token) : Statement(Token)
{
    public ElementType? Type { get; set; }

    public static Identifier Temporary { get; } = new("temp", default); // TODO: improve this
    public static Identifier Empty { get; } = new("", default); // TODO: improve this


    public Identifier(string name, Token token, ElementType type) : this(name, token) => Type = type;


    public override string ToString() => Type is null ? Name : $"{Type} {Name}";
}

public record ElementType(OneOf<Array, string> Name, Token Token) : Statement(Token)
{
    public static ElementType Int { get; } = new("int", default);
    public static ElementType Bool { get; } = new("bool", default);


    public override string ToString() => Name.Match(x => x.ToString(), x => x);
}

public record Array(ElementType Type, uint Size, Token Token) : Statement(Token)
{
    public override string ToString() => $"{Type}[]";
}

public record Number(uint Value, Token Token) : Statement(Token)
{
    public override string ToString() => Value.ToString();
}

public record TypeInitialization(ElementType Type, Token Token) : Statement(Token)
{
    public override string ToString() => $"{Type}";
}

public record ArrayAccessor(Identifier Identifier, Expression Index, Token Token) : Statement(Token)
{
    public override string ToString() => $"{Identifier}[{Index}]";
}

public record BinaryOperation(BinaryOperator Operator, Expression Left, Expression Right, Token Token) : Statement(Token)
{
    public override string ToString() => $"{Left} {Operator.GetBinaryOperatorString()} {Right}";

    public ElementType GetResultType()
    {
        if (Left is null) return ElementType.Int;
        if (Right is null) return ElementType.Int;

        return ElementType.Int; // TODO: improve type resolving
    }
}

public record Expression(OneOf<Identifier, Number, BinaryOperation, ArrayAccessor, TypeInitialization, FunctionCall> Value, Token Token) : Statement(Token)
{
    public override string ToString() => Value.Match(
        identifier => identifier.ToString(),
        number => number.ToString(),
        binaryOperation => binaryOperation.ToString(),
        arrayAccessor => arrayAccessor.ToString(),
        typeInitialization => typeInitialization.ToString(),
        functionCall => functionCall.ToString()
    );
}

public record If(Expression Condition, Statement Body, Token Token, Statement? ElseBody = null) : Statement(Token)
{
    public override string ToString() => $"if ({Condition}) {Body}{(ElseBody is not null ? $" else {ElseBody}" : "")}";
}

public record While(Expression Condition, Statement Body, Token Token) : Statement(Token)
{
    public override string ToString() => $"while ({Condition}) {Body}";
}

public record Body(Scope Scope, Token Token) : Statement(Token)
{
    public override string ToString() => $"{{ {Scope} }}";
}

public record Break(Token Token) : Statement(Token)
{
    public override string ToString() => "break";
}

public record Continue(Token Token) : Statement(Token)
{
    public override string ToString() => "continue";
}

public record FunctionDeclaration(Identifier Identifier, Scope Arguments, Body Body, Token Token, ElementType? ReturnType = null) : Statement(Token)
{
    public override string ToString() => $"{Identifier}({string.Join(", ", Arguments)}) -> {(ReturnType is not null ? ReturnType.ToString() : "()")} {Body}";
}

public record ArgumentDefinition(Identifier Identifier, ElementType Type, Token Token) : Statement(Token)
{
    public override string ToString() => $"{Type} {Identifier}";
}

public record Return(Expression Expression, Token Token) : Statement(Token)
{
    public override string ToString() => $"return {Expression}";
}

public record FunctionCall(Identifier Identifier, Scope Arguments, Token Token) : Statement(Token)
{
    public FunctionDeclaration? Function { get; set; }


    public override string ToString() => $"{Identifier}({string.Join(", ", Arguments)})";
}