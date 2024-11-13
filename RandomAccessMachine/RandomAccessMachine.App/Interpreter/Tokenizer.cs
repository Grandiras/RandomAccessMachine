using RandomAccessMachine.App.Specification;
using System.Text;

namespace RandomAccessMachine.App.Interpreter;
internal class Tokenizer
{
    public List<Instruction> Tokenize(string code)
    {
        var tokens = new List<Instruction>();

        // go through each character in the code, if it's a letter, it's a label or an opcode, if it doesn't start with one, there is an error
        var state = TokenizerState.Start;
        var buffer = new StringBuilder();
        for (var i = 0; i < code.Length; i++)
        {
            var currentChar = code[i];
            var lineNumber = code[..i].Count(c => c == '\n') + 1;
            var columnNumber = i - code[..i].LastIndexOf('\n');

            // Skip comments
            if (currentChar is '/' && code[i + 1] is '/')
            {
                i = code.IndexOf('\n', i);
                continue;
            }

            // Skip whitespaces
            if (state is TokenizerState.Start && char.IsWhiteSpace(currentChar)) continue;

            // If the character is a letter, we either have a label or an opcode
            if (state is TokenizerState.Start && char.IsLetter(currentChar))
            {
                _ = buffer.Append(currentChar);
                state = TokenizerState.Text;
                continue;
            }

            // We have a label declaration
            if (state is TokenizerState.Text && currentChar is ':')
            {

            }

            // We have an opcode or a label reference
            if (state is TokenizerState.Text && char.IsWhiteSpace(currentChar))
            {

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

            if (state is TokenizerState.Immediate or TokenizerState.AddressPointer && char.IsDigit(currentChar))
            {
                _ = buffer.Append(currentChar);
                continue;
            }

            if (state is TokenizerState.Immediate or TokenizerState.AddressPointer && char.IsWhiteSpace(currentChar))
            {

            }
        }

        return tokens;
    }
}

internal enum TokenizerState
{
    Start,
    Text,
    Immediate,
    Address,
    AddressPointer
}
