using RandomAccessMachine.FAIL.Compiler;

var code = @"
var x = 5;
// Do a factorial
var y = 1;
while (x > 1) {
    y = y * x;
    x = x - 1;
}
";

Console.WriteLine(code.Trim());

Console.WriteLine();

var tokens = Tokenizer.Tokenize(code);

if (tokens.IsT1)
{
    Console.WriteLine(tokens.AsT1);
    return;
}

foreach (var token in tokens.AsT0)
{
    Console.WriteLine(token);
}

Console.WriteLine();

var scope = Parser.Parse(tokens.AsT0);

if (scope.IsT1)
{
    Console.WriteLine(scope.AsT1);
    return;
}

foreach (var statement in scope.AsT0.Statements)
{
    Console.WriteLine(statement);
}

Console.WriteLine();

var output = Emitter.Emit(scope.AsT0);

Console.WriteLine(output);

// Hook this up to the interpreter

var ramTokens = RandomAccessMachine.Backend.Interpreter.Tokenizer.Tokenize(output);

if (ramTokens.IsT1)
{
    Console.WriteLine(ramTokens.AsT1);
    return;
}

foreach (var token in ramTokens.AsT0)
{
    Console.WriteLine(token);
}

Console.WriteLine();

var ramScope = RandomAccessMachine.Backend.Interpreter.Parser.Parse(ramTokens.AsT0);

if (ramScope.IsT1)
{
    Console.WriteLine(ramScope.AsT1);
    return;
}

foreach (var instruction in ramScope.AsT0.Instructions)
{
    Console.WriteLine(instruction);
}

Console.WriteLine();

foreach (var label in ramScope.AsT0.Labels)
{
    Console.WriteLine(label);
}

Console.WriteLine();

var validationResult = RandomAccessMachine.Backend.Interpreter.LabelResolver.Validate(ramScope.AsT0);

if (validationResult.IsT1)
{
    Console.WriteLine(validationResult.AsT1);
    return;
}

foreach (var label in validationResult.AsT0.Labels)
{
    Console.WriteLine(label);
}

Console.WriteLine();

var interpreter = new RandomAccessMachine.Backend.Interpreter.Interpreter
{
    IsRealTime = true
};

foreach (var register in interpreter.Registers)
{
    Console.WriteLine(register);
}

Console.WriteLine();

var boundsCheckResult = RandomAccessMachine.Backend.Interpreter.BoundsChecker.CheckBounds(ramScope.AsT0, interpreter);

if (boundsCheckResult.IsT1)
{
    Console.WriteLine(boundsCheckResult.AsT1);
    return;
}

interpreter.Stopped += (sender, e) =>
{
    Console.WriteLine("Stopped");

    Console.WriteLine();

    foreach (var register in interpreter.Registers)
    {
        Console.WriteLine(register);
    }

    Environment.Exit(0);
};

var cancellationToken = new CancellationTokenSource();

interpreter.LoadProgram(ramScope.AsT0);
_ = interpreter.Execute(cancellationToken.Token);

await Task.Delay(5000);
cancellationToken.Cancel();