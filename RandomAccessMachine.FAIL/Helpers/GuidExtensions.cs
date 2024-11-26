namespace RandomAccessMachine.FAIL.Helpers;
public static class GuidExtensions
{
    public static string ToLabelString(this Guid guid) => $"L_{guid:N}";
}
