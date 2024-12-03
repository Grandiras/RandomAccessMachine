using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Tests.Backend;
[TestClass]
public class BoundsCheckerTests
{
    [TestMethod]
    public void CheckBounds_AllRegistersWithinBounds_ReturnsScope()
    {
        var interpreter = new Interpreter();
        var scope = new Scope(
            [
                new Instruction(OpCode.LOAD, new Argument(new Address(1u), new Token(1u, TokenType.Address, 0, 0, 1)), new Token("LOAD", TokenType.OpCode, 0, 0, 0))
            ], []);

        var result = BoundsChecker.CheckBounds(scope, interpreter);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(scope, result.AsT0);
    }

    [TestMethod]
    public void CheckBounds_RegisterOutOfBounds_ReturnsErrorInfo()
    {
        var interpreter = new Interpreter();
        var scope = new Scope(
            [
                new Instruction(OpCode.LOAD, new Argument(new Address(7u), new Token(7u, TokenType.Address, 0, 0, 1)), new Token("LOAD", TokenType.OpCode, 0, 0, 0))
            ], []);

        var result = BoundsChecker.CheckBounds(scope, interpreter);
        Assert.IsTrue(result.IsT1);
    }

    [TestMethod]
    public void CheckBounds_NoInstructions_ReturnsScope()
    {
        var interpreter = new Interpreter();
        var scope = new Scope([], []);

        var result = BoundsChecker.CheckBounds(scope, interpreter);
        Assert.IsTrue(result.IsT0);
        Assert.AreEqual(scope, result.AsT0);
    }
}
