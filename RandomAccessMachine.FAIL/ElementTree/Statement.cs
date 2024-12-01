using OneOf;
using RandomAccessMachine.FAIL.Specification;
using RandomAccessMachine.FAIL.Specification.Operators;

namespace RandomAccessMachine.FAIL.ElementTree;
public record Statement();

public record Assignment(Identifier Identifier, Expression Expression) : Statement
{
    public override string ToString() => $"{Identifier} = {Expression}";
}

public record Identifier(string Name, ElementType? Type = null) : Statement
{
    public static Identifier Temporary { get; } = new("t", new("var")); // TODO: improve this
    public static Identifier Empty { get; } = new(""); // TODO: improve this

    public override string ToString() => Type is null ? Name : $"{Type} {Name}";
}

public record ElementType(OneOf<Array, string> Name) : Statement
{
    public override string ToString() => Name.Match(x => x.ToString(), x => x);
}

public record Array(ElementType Type, uint Size) : Statement
{
    public override string ToString() => $"{Type}[]";
}

public record Number(uint Value) : Statement
{
    public override string ToString() => Value.ToString();
}

public record TypeInitialization(ElementType Type) : Statement
{
    public override string ToString() => $"{Type}";
}

public record ArrayAccessor(Identifier Identifier, Expression Index) : Statement
{
    public override string ToString() => $"{Identifier}[{Index}]";
}

public record BinaryOperation(BinaryOperator Operator, Expression Left, Expression Right) : Statement
{
    public override string ToString() => $"{Left} {Operator.GetBinaryOperatorString()} {Right}";
}

public record Expression(OneOf<Identifier, Number, BinaryOperation, ArrayAccessor, TypeInitialization> Value) : Statement
{
    public override string ToString() => Value.Match(
        identifier => identifier.ToString(),
        number => number.ToString(),
        binaryOperation => binaryOperation.ToString(),
        arrayAccessor => arrayAccessor.ToString(),
        typeInitialization => typeInitialization.ToString()
    );
}

public record If(Expression Condition, Statement Body, Statement? ElseBody = null) : Statement
{
    public override string ToString() => $"if ({Condition}) {Body}{(ElseBody is not null ? $" else {ElseBody}" : "")}";
}

public record While(Expression Condition, Statement Body) : Statement
{
    public override string ToString() => $"while ({Condition}) {Body}";
}

public record Body(Scope Scope) : Statement
{
    public override string ToString() => $"{{ {Scope} }}";
}

public record Break() : Statement
{
    public override string ToString() => "break";
}

public record Continue() : Statement
{
    public override string ToString() => "continue";
}