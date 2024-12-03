using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Tests.Backend;

[TestClass]
public class TokenizerTests
{
    [TestMethod]
    public void Tokenize_ValidInput_ReturnsTokens()
    {
        var input = @"
        START:
            LOAD #0
            STORE 1
            STORE *1
            GOTO START
            END
        ";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT0);

        var tokens = result.AsT0;
        Assert.IsNotNull(tokens);
        Assert.AreEqual(10, tokens.Count);

        var tokensArray = tokens.ToArray();

        Assert.AreEqual("START", tokensArray[0].Value);
        Assert.AreEqual(TokenType.Label, tokensArray[0].Type);

        Assert.AreEqual("LOAD", tokensArray[1].Value);
        Assert.AreEqual(TokenType.OpCode, tokensArray[1].Type);

        Assert.AreEqual(0u, tokensArray[2].Value.AsT1);
        Assert.AreEqual(TokenType.Immediate, tokensArray[2].Type);

        Assert.AreEqual("STORE", tokensArray[3].Value);
        Assert.AreEqual(TokenType.OpCode, tokensArray[3].Type);

        Assert.AreEqual(1u, tokensArray[4].Value.AsT1);
        Assert.AreEqual(TokenType.Address, tokensArray[4].Type);

        Assert.AreEqual("STORE", tokensArray[5].Value);
        Assert.AreEqual(TokenType.OpCode, tokensArray[5].Type);

        Assert.AreEqual(1u, tokensArray[6].Value.AsT1);
        Assert.AreEqual(TokenType.AddressPointer, tokensArray[6].Type);

        Assert.AreEqual("GOTO", tokensArray[7].Value);
        Assert.AreEqual(TokenType.OpCode, tokensArray[7].Type);

        Assert.AreEqual("START", tokensArray[8].Value);
        Assert.AreEqual(TokenType.LabelReference, tokensArray[8].Type);

        Assert.AreEqual("END", tokensArray[9].Value);
        Assert.AreEqual(TokenType.OpCode, tokensArray[9].Type);
    }

    [TestMethod]
    public void Tokenize_InvalidInput_ReturnsError()
    {
        ReadOnlySpan<string> inputs = ["123INVALID_INSTRUCTION", "&1", "START;", ","];

        foreach (var input in inputs)
        {
            var result = Tokenizer.Tokenize(input);
            Assert.IsTrue(result.IsT1);
        }
    }

    [TestMethod]
    public void Tokenize_EmptyInput_ReturnsEmptyQueue()
    {
        var input = "";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT0);

        var tokens = result.AsT0;
        Assert.IsNotNull(tokens);
        Assert.AreEqual(0, tokens.Count);
    }

    [TestMethod]
    public void Tokenize_OnlyComments_ReturnsEmptyQueue()
    {
        var input = "// This is a comment\n// Another comment";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT0);

        var tokens = result.AsT0;
        Assert.IsNotNull(tokens);
        Assert.AreEqual(0, tokens.Count);
    }

    [TestMethod]
    public void Tokenize_OnlyWhitespaces_ReturnsEmptyQueue()
    {
        var input = "   \n\t  ";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT0);

        var tokens = result.AsT0;
        Assert.IsNotNull(tokens);
        Assert.AreEqual(0, tokens.Count);
    }

    [TestMethod]
    public void Tokenize_InvalidCharacters_ReturnsError()
    {
        var input = "LOAD @";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT1);
    }

    [TestMethod]
    public void Tokenize_MixedValidAndInvalidInput_ReturnsError()
    {
        var input = "LOAD 0\nINVALID@";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT1);
    }

    [TestMethod]
    public void Tokenize_ImmediateValueWithoutNumber_ReturnsError()
    {
        var input = "LOAD #";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT1);
    }

    [TestMethod]
    public void Tokenize_AddressPointerWithoutNumber_ReturnsError()
    {
        var input = "LOAD *";

        var result = Tokenizer.Tokenize(input);
        Assert.IsTrue(result.IsT1);
    }
}
