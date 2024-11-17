using RandomAccessMachine.Backend.Interpreter;

var code = @"
START:
    LOAD #1
    STORE 1
    JZERO START
    JNZERO ENDOFPROGRAM
ENDOFPROGRAM:
    END
";

var tokens = Tokenizer.Tokenize(code).AsT0;

foreach (var token in tokens)
{
    Console.WriteLine(token);
}

var scope = Parser.Parse(tokens);

foreach (var instruction in scope.AsT0.Instructions)
{
    Console.WriteLine(instruction);
}

foreach (var label in scope.AsT0.Labels)
{
    Console.WriteLine(label);
}

var validationResult = LabelResolver.Validate(scope.AsT0);
scope = validationResult.AsT0;

foreach (var label in validationResult.AsT0.Labels)
{
    Console.WriteLine(label);
}

var interpreter = new Interpreter();

foreach (var register in interpreter.Registers)
{
    Console.WriteLine(register);
}

var boundsCheckResult = BoundsChecker.CheckBounds(scope.AsT0, interpreter);

interpreter.Stopped += (sender, e) => Console.WriteLine("Stopped");

var cancellationToken = new CancellationTokenSource();

interpreter.LoadProgram(scope.AsT0);
_ = interpreter.Execute(cancellationToken.Token);

await Task.Delay(5000);
cancellationToken.Cancel();

foreach (var register in interpreter.Registers)
{
    Console.WriteLine(register);
}