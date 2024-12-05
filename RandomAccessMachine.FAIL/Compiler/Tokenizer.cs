using OneOf;
using OneOf.Types;
using RandomAccessMachine.FAIL.Debugging;
using RandomAccessMachine.FAIL.Specification;
using RandomAccessMachine.FAIL.Specification.Operators;
using System.Text;

namespace RandomAccessMachine.FAIL.Compiler;
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

            // If the character is a letter, we have a text token
            if (state is TokenizerState.Start && (char.IsLetter(currentChar) || currentChar is '_'))
            {
                _ = buffer.Append(currentChar);
                state = TokenizerState.Text;
                continue;
            }

            // If the character is a number, we have a number token
            if (state is TokenizerState.Start && char.IsDigit(currentChar))
            {
                _ = buffer.Append(currentChar);
                state = TokenizerState.Number;
                continue;
            }

            // We have a text token
            if (state is TokenizerState.Text && (char.IsLetterOrDigit(currentChar) || currentChar is '_'))
            {
                _ = buffer.Append(currentChar);
                continue;
            }

            // Terminate the text token
            if (state is TokenizerState.Text)
            {
                // If the buffer is a keyword, we have a keyword token
                if (buffer.ToString().IsSpelledCorrectly())
                {
                    var keyword = Enum.Parse<Keyword>(buffer.ToString(), true);
                    tokens.Enqueue(new(keyword, keyword.GetTokenType(), lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length, buffer.ToString()));
                }
                else tokens.Enqueue(new(buffer.ToString(), TokenType.Identifier, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length, buffer.ToString()));

                _ = buffer.Clear();
                state = TokenizerState.Start;
                i--;
                columnNumber--;
                continue;
            }

            // We have a number token
            if (state is TokenizerState.Number && char.IsDigit(currentChar))
            {
                _ = buffer.Append(currentChar);
                continue;
            }

            // Terminate the number token
            if (state is TokenizerState.Number)
            {
                tokens.Enqueue(new(uint.Parse(buffer.ToString()), TokenType.Number, lineNumber, (uint)(columnNumber - buffer.Length), (uint)buffer.Length, buffer.ToString()));
                _ = buffer.Clear();
                state = TokenizerState.Start;
                i--;
                columnNumber--;
                continue;
            }

            // Function return declaration token
            if (state is TokenizerState.Start && code[i..(i + 2)] is "->")
            {
                tokens.Enqueue(new("->", TokenType.ReturnDeclaration, lineNumber, columnNumber, 2, "->"));
                i++;
                columnNumber++;
                continue;
            }

            // Self-assignment operator tokens
            if (state is TokenizerState.Start && code[i..(i + 2)].ToString().GetSelfAssignmentOperator() is not null and SelfAssignmentOperator selfAssignmentOperator)
            {
                tokens.Enqueue(new(selfAssignmentOperator, TokenType.SelfAssignment, lineNumber, columnNumber, 2, code[i..(i + 2)].ToString()));
                i++;
                columnNumber++;
                continue;
            }

            // Incremental operator tokens
            if (state is TokenizerState.Start && code[i..(i + 2)].ToString().GetIncrementalOperator() is not null and IncrementalOperator incrementalOperator)
            {
                tokens.Enqueue(new(incrementalOperator, TokenType.IncrementalOperator, lineNumber, columnNumber, 2, code[i..(i + 2)].ToString()));
                i++;
                columnNumber++;
                continue;
            }

            // Two-character operator tokens
            if (state is TokenizerState.Start && code[i..(i + 2)].ToString().GetBinaryOperator() is not null and BinaryOperator @operator)
            {
                tokens.Enqueue(new(@operator, TokenType.BinaryOperator, lineNumber, columnNumber, 2, code[i..(i + 2)].ToString()));
                i++;
                columnNumber++;
                continue;
            }

            // Operator tokens
            if (state is TokenizerState.Start && currentChar.ToString().GetBinaryOperator() is not null and BinaryOperator binaryOperator)
            {
                tokens.Enqueue(new(binaryOperator, TokenType.BinaryOperator, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // Comparison tokens
            if (state is TokenizerState.Start && currentChar is '=' && i + 1 < code.Length && code[i + 1] is '=')
            {
                tokens.Enqueue(new(BinaryOperator.Equal, TokenType.BinaryOperator, lineNumber, columnNumber, 2, "=="));
                i++;
                columnNumber++;
                continue;
            }
            if (state is TokenizerState.Start && currentChar is '>')
            {
                tokens.Enqueue(new(BinaryOperator.GreaterThan, TokenType.BinaryOperator, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // Assignment token
            if (state is TokenizerState.Start && currentChar is '=')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.Assignment, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // Parenthesis tokens
            if (state is TokenizerState.Start && currentChar is '(')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.LeftParenthesis, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }
            if (state is TokenizerState.Start && currentChar is ')')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.RightParenthesis, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }
            if (state is TokenizerState.Start && currentChar is '{')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.LeftCurlyBrace, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }
            if (state is TokenizerState.Start && currentChar is '}')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.RightCurlyBrace, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }
            if (state is TokenizerState.Start && currentChar is '[')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.LeftSquareBrace, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }
            if (state is TokenizerState.Start && currentChar is ']')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.RightSquareBrace, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // End of line token
            if (state is TokenizerState.Start && currentChar is ';')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.EndOfStatement, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // Comma token
            if (state is TokenizerState.Start && currentChar is ',')
            {
                tokens.Enqueue(new(currentChar.ToString(), TokenType.Comma, lineNumber, columnNumber, 1, currentChar.ToString()));
                continue;
            }

            // Error token
            return new ErrorInfo($"Unexpected character '{currentChar}' at line {lineNumber}, column {columnNumber}!", new(new Error(), TokenType.ErrorOrEmpty, lineNumber, columnNumber, 1, currentChar.ToString()));
        }

        return state is TokenizerState.Start
            ? tokens
            : new ErrorInfo("Unexpected state at file end!", new());
    }
}

public enum TokenizerState
{
    Start,
    Text,
    Number,
    Comment,
}
