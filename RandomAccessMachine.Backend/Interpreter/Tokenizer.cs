using OneOf;
using OneOf.Types;
using RandomAccessMachine.Backend.Debugging;
using RandomAccessMachine.Backend.Specification;
using System.Text;

namespace RandomAccessMachine.Backend.Interpreter;
public static class Tokenizer
{
    public static OneOf<Queue<Token>, ErrorInfo> Tokenize(string code)
    {
        var tokens = new Queue<Token>();

        code += Environment.NewLine;

        var state = TokenizerState.Start;
        var buffer = new StringBuilder();
        var lineNumber = 1u;
        var columnNumber = 0u;

        for (var i = 0; i < code.Length; i++)
        {
            var currentChar = code[i];
            columnNumber++;

            // Skip comments
            if (state is TokenizerState.Comment)
            {
                if (currentChar is '\n')
                {
                    state = TokenizerState.Start;
                    lineNumber++;
                    columnNumber = 0;
                }
                continue;
            }

            // Detect comments
            if (currentChar is '/' && i + 1 < code.Length && code[i + 1] is '/')
            {
                state = TokenizerState.Comment;
                continue;
            }

            // Skip whitespaces
            if (state is TokenizerState.Start && char.IsWhiteSpace(currentChar))
            {
                if (currentChar is '\n')
                {
                    lineNumber++;
                    columnNumber = 0;
                }
                continue;
            }

            // If the character is a letter, we either have a label or an opcode
            if (state is TokenizerState.Start && (char.IsLetter(currentChar) || currentChar is '_'))
            {
                _ = buffer.Append(currentChar);
                state = TokenizerState.Text;
                continue;
            }

            // We have a label declaration
            if (state is TokenizerState.Text && currentChar is ':')
            {
                tokens.Enqueue(new(buffer.ToString().ToUpper(), TokenType.Label, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length));
                _ = buffer.Clear();
                state = TokenizerState.Start;
                continue;
            }

            // We have an opcode or a label reference
            if (state is TokenizerState.Text && char.IsWhiteSpace(currentChar))
            {
                // If any of the OpCode enum values is parsed, it's an opcode, otherwise it's a label reference
                if (Enum.TryParse<OpCode>(buffer.ToString(), true, out _))
                    tokens.Enqueue(new(buffer.ToString().ToUpper(), TokenType.OpCode, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length));
                else
                    tokens.Enqueue(new(buffer.ToString().ToUpper(), TokenType.LabelReference, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length));

                _ = buffer.Clear();
                state = TokenizerState.Start;
                continue;
            }

            // Otherwise, if we are in a correct state, we can add the character to the buffer
            if (state is TokenizerState.Text && (char.IsLetterOrDigit(currentChar) || currentChar is '_'))
            {
                _ = buffer.Append(currentChar);
                continue;
            }

            // We have an address
            if (state is TokenizerState.Start && char.IsDigit(currentChar))
            {
                _ = buffer.Append(currentChar);
                state = TokenizerState.Address;
                continue;
            }

            // We have an immediate value
            if (state is TokenizerState.Start && currentChar is '#')
            {
                state = TokenizerState.Immediate;
                continue;
            }

            // We have an address pointer
            if (state is TokenizerState.Start && currentChar is '*')
            {
                state = TokenizerState.AddressPointer;
                continue;
            }

            // If we have a digit, we can add it to the buffer
            if (state is TokenizerState.Immediate or TokenizerState.Address or TokenizerState.AddressPointer && char.IsDigit(currentChar))
            {
                _ = buffer.Append(currentChar);
                continue;
            }

            // If we have a whitespace, we can add the token to the list
            if (state is TokenizerState.Address or TokenizerState.Immediate or TokenizerState.AddressPointer && char.IsWhiteSpace(currentChar))
            {
                if (buffer.Length is 0) return new ErrorInfo($"Address or immediate missing number!", new(new Error(), state is TokenizerState.Address ? TokenType.Address : TokenType.AddressPointer, lineNumber, columnNumber, 1));

                var type = state switch
                {
                    TokenizerState.Address => TokenType.Address,
                    TokenizerState.Immediate => TokenType.Immediate,
                    TokenizerState.AddressPointer => TokenType.AddressPointer,
                    _ => TokenType.Faulty
                };

                tokens.Enqueue(new(uint.Parse(buffer.ToString()), type, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length));

                _ = buffer.Clear();
                state = TokenizerState.Start;
                continue;
            }

            // This is an error now, we handled all allowed cases
            return new ErrorInfo($"Unexpected character '{currentChar}'!", new(new Error(), TokenType.Faulty, lineNumber, columnNumber, 1));
        }

        return state is TokenizerState.Start
            ? tokens
            : new ErrorInfo($"Unexpected state at file end!", new());
    }
}

internal enum TokenizerState
{
    Start,
    Comment,
    Text,
    Immediate,
    Address,
    AddressPointer
}
