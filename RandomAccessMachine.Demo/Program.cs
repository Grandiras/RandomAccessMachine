using RandomAccessMachine.Backend.Interpreter;

var code = @"
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
";

var tokens = Tokenizer.Tokenize(code).AsT0;

foreach (var token in tokens)
{
    Console.WriteLine(token);
}

var scope = Parser.Parse(tokens);

foreach (var instruction in scope.Instructions)
{
    Console.WriteLine(instruction);
}

foreach (var label in scope.Labels)
{
    Console.WriteLine(label);
}

var interpreter = new Interpreter(scope);

foreach (var register in interpreter.Registers)
{
    Console.WriteLine(register);
}

var cancellationToken = new CancellationTokenSource();
var task = Task.Factory.StartNew(() => interpreter.Execute(cancellationToken.Token));

task.Wait(TimeSpan.FromSeconds(1));
cancellationToken.Cancel();

foreach (var register in interpreter.Registers)
{
    Console.WriteLine(register);
}