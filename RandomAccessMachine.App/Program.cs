// Instructions
// ADD, SUB, MUL, DIV, LOAD, STORE, GOTO, JZERO, JNZERO, END
// #i = immediate value
// i = address
// *i = value at address i
// Registers
// AKK, R1, R2, R3, R4, R5, R6, R7, R8, R9, R10
// Memory
// Labels allowed

using RandomAccessMachine.App.Interpreter;

var code = @"""
START:
    LOAD #1
    STORE 1
    LOAD #2
    STORE 2
    LOAD 1
    ADD 2
    STORE 2
    GOTO START
    END
""";

var tokenizer = new Tokenizer();
var _ = tokenizer.Tokenize(code);