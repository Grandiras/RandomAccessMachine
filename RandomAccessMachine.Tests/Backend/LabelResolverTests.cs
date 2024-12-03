using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Tests.Backend;
[TestClass]
public class LabelResolverTests
{
    private static Scope CreateScope(List<Instruction> instructions, List<Label> labels) => new(instructions, labels);

    private static Instruction CreateInstruction(OpCode opCode, Argument? argument, Token token) => new(opCode, argument, token);

    private static Label CreateLabel(string name, uint address, Token token) => new(name, address, token);

    private static Token CreateToken(string value, TokenType type, uint lineNumber, uint columnNumber, uint length) => new(value, type, lineNumber, columnNumber, length);

    [TestMethod]
    public void Validate_ValidLabels_ReturnsScope()
    {
        var token = CreateToken("label1", TokenType.LabelReference, 1, 1, 6);
        var label = CreateLabel("label1", 0, token);
        var argument = new Argument(new LabelReference("label1", null!), token);
        var instruction = CreateInstruction(OpCode.GOTO, argument, token);
        var scope = CreateScope([instruction], [label]);

        var result = LabelResolver.Validate(scope);

        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(scope, result.AsT0);
    }

    [TestMethod]
    public void Validate_InvalidLabels_ReturnsErrorInfo()
    {
        var token = CreateToken("label1", TokenType.LabelReference, 1, 1, 6);
        var argument = new Argument(new LabelReference("label1", null!), token);
        var instruction = CreateInstruction(OpCode.GOTO, argument, token);
        var scope = CreateScope([instruction], []);

        var result = LabelResolver.Validate(scope);

        Assert.IsTrue(result.IsT1);
    }

    [TestMethod]
    public void Validate_NullArguments_ReturnsScope()
    {
        var token = CreateToken("value", TokenType.LabelReference, 1, 1, 5);
        var instruction = CreateInstruction(OpCode.GOTO, null, token);
        var scope = CreateScope([instruction], []);

        var result = LabelResolver.Validate(scope);

        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(scope, result.AsT0);
    }
}
