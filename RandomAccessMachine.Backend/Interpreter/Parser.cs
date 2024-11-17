using OneOf;
using RandomAccessMachine.Backend.Debugging;
using RandomAccessMachine.Backend.Specification;

namespace RandomAccessMachine.Backend.Interpreter;
public static class Parser
{
    public static OneOf<Scope, ErrorInfo> Parse(Queue<Token> tokens)
    {
        var scope = new Scope([], []);

        while (tokens.Count > 0)
        {
            var token = tokens.Dequeue();

            if (token.Type is TokenType.Label)
            {
                var label = new Label(token.Value.AsT0, (uint)scope.Instructions.Count, token);
                scope.Labels.Add(label);
            }
            else
            {
                var instruction = ParseInstruction(token, tokens, scope);
                if (instruction.IsT1) return instruction.AsT1;

                scope.Instructions.Add(instruction.AsT0);
            }
        }

        return scope;
    }

    private static OneOf<Instruction, ErrorInfo> ParseInstruction(Token token, Queue<Token> tokens, Scope scope)
    {
        if (token.Type is not TokenType.OpCode) return new ErrorInfo($"Unexpected token type: {token.Type}", token);

        var opCode = Enum.Parse<OpCode>(token.Value.AsT0);

        // If the OpCode is a END, there should be no argument
        if (opCode is OpCode.END) return new Instruction(opCode, null, token);

        var argumentToken = tokens.Dequeue();
        var argument = ParseArgument(argumentToken, tokens, scope);

        if (argument.IsT1) return argument.AsT1;

        // Check if the given argument is valid for the OpCode
        if (opCode is OpCode.GOTO or OpCode.JZERO or OpCode.JNZERO)
        {
            // Only labels are allowed for GOTO, JZERO and JNZERO
            if (!argument.AsT0.Value.IsT3)
                return new ErrorInfo($"Invalid argument type for OpCode {opCode}: {argument.AsT0.Value}", argumentToken);
        }
        else if (opCode is OpCode.STORE)
        {
            // Only addresses and address pointers are allowed for STORE
            if (!argument.AsT0.Value.IsT1 && !argument.AsT0.Value.IsT2)
                return new ErrorInfo($"Invalid argument type for OpCode {opCode}: {argument.AsT0.Value}", argumentToken);
        }
        else
        {
            // Only immediate values, addresses and address pointers are allowed for LOAD, ADD, SUB, MUL, and DIV
            if (argument.AsT0.Value.IsT3)
                return new ErrorInfo($"Invalid argument type for OpCode {opCode}: {argument.AsT0.Value}", argumentToken);
        }


        return new Instruction(opCode, argument.AsT0, token);
    }

    private static OneOf<Argument, ErrorInfo> ParseArgument(Token token, Queue<Token> tokens, Scope scope) => token.Type switch
    {
        TokenType.Immediate => new Argument(new Immediate(token.Value.AsT1), token),
        TokenType.Address => new Argument(new Address(token.Value.AsT1), token),
        TokenType.AddressPointer => new Argument(new AddressPointer(token.Value.AsT1), token),
        TokenType.LabelReference => new Argument(new LabelReference(token.Value.AsT0, null!), token),
        _ => new ErrorInfo($"Unexpected token type: {token.Type}", token)
    };
}
