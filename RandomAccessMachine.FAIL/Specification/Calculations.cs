﻿namespace RandomAccessMachine.FAIL.Specification;
[Flags]
internal enum Calculations : int
{
    Term = 1,
    DotCalculations = 2,
    StrokeCalculations = 4,
}

internal static class CalculationsExtensions
{
    private static readonly int[] Values = Enum.GetValues<Calculations>().Cast<int>().ToArray();
    internal static Calculations All => (Calculations)Values.Sum();

    internal static Calculations Above(this Calculations calculations) => (Calculations)((int)calculations - 1);
    internal static Calculations SelfAndAbove(this Calculations calculations) => (Calculations)(((int)calculations * 2) - 1);
    internal static Calculations Below(this Calculations calculations) => (Calculations)((int)All - (((int)calculations * 2) - 1));
    internal static Calculations SelfAndBelow(this Calculations calculations) => (Calculations)((int)All - ((int)calculations - 1));

    public static TokenType GetOperationTokenType(this Calculations calculation) => calculation switch
    {
        Calculations.DotCalculations => TokenType.BinaryOperator,
        Calculations.StrokeCalculations => TokenType.BinaryOperator,
        _ => throw new NotSupportedException()
    };
}
