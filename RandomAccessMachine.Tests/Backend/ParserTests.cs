using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Tests.Backend;
[TestClass]
public class ParserTests
{
    private static Queue<Token> CreateTokenQueue(params Token[] tokens) => new(tokens);

    [TestMethod]
    public void Parse_EmptyTokenQueue_ReturnsEmptyScope()
    {
        var tokens = CreateTokenQueue();
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(0, result.AsT0.Instructions.Count);
        Assert.AreEqual(0, result.AsT0.Labels.Count);
    }

    [TestMethod]
    public void Parse_SingleLabel_ReturnsScopeWithLabel()
    {
        var tokens = CreateTokenQueue(new Token("label1", TokenType.Label, 0, 0, 0));
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(1, result.AsT0.Labels.Count);
        Assert.AreEqual("label1", result.AsT0.Labels[0].Name);
    }

    [TestMethod]
    public void Parse_SingleInstruction_ReturnsScopeWithInstruction()
    {
        var tokens = CreateTokenQueue(new Token("LOAD", TokenType.OpCode, 0, 0, 0), new Token(1u, TokenType.Immediate, 0, 0, 0));
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(1, result.AsT0.Instructions.Count);
        Assert.AreEqual(OpCode.LOAD, result.AsT0.Instructions[0].OpCode);
    }

    [TestMethod]
    public void Parse_InstructionWithInvalidArgument_ReturnsError()
    {
        var tokens = CreateTokenQueue(new Token("LOAD", TokenType.OpCode, 0, 0, 0), new Token("label1", TokenType.LabelReference, 0, 0, 0));
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT1);
        Assert.IsTrue(result.AsT1.Message.Contains("Invalid argument type"));
    }

    [TestMethod]
    public void Parse_InstructionWithValidArgument_ReturnsScopeWithInstruction()
    {
        var tokens = CreateTokenQueue(new Token("STORE", TokenType.OpCode, 0, 0, 0), new Token(1u, TokenType.Address, 0, 0, 0));
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(1, result.AsT0.Instructions.Count);
        Assert.AreEqual(OpCode.STORE, result.AsT0.Instructions[0].OpCode);
    }

    [TestMethod]
    public void Parse_MultipleInstructionsAndLabels_ReturnsScopeWithAllElements()
    {
        var tokens = CreateTokenQueue(
            new Token("label1", TokenType.Label, 0, 0, 0),
            new Token("LOAD", TokenType.OpCode, 0, 0, 0), new Token(1u, TokenType.Immediate, 0, 0, 0),
            new Token("label2", TokenType.Label, 0, 0, 0),
            new Token("STORE", TokenType.OpCode, 0, 0, 0), new Token(2u, TokenType.Address, 0, 0, 0)
        );
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(2, result.AsT0.Labels.Count);
        Assert.AreEqual(2, result.AsT0.Instructions.Count);
    }

    [TestMethod]
    public void Parse_InvalidTokenType_ReturnsError()
    {
        var tokens = CreateTokenQueue(new Token("INVALID", TokenType.AddressPointer, 0, 0, 0));
        var result = Parser.Parse(tokens);
        Assert.IsTrue(result.IsT1);
        Assert.IsTrue(result.AsT1.Message.Contains("Unexpected token type"));
    }
}
