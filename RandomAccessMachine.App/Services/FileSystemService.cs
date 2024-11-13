using System.Text.RegularExpressions;
using WinSharp.FlagInterfaces;

namespace RandomAccessMachine.App.Services;
public sealed class FileSystemService : IService
{
    public static string GetDirectoryPath()
#if !DEBUG
        => Directory.GetCurrentDirectory();
#else
        => Directory.GetCurrentDirectory().Split(@"\bin").First();
#endif

    public static IEnumerable<string> GetScriptFiles() => Directory.GetFiles(GetDirectoryPath(), "*.txt", SearchOption.AllDirectories);
    public static IEnumerable<string> GetSimplifiedScriptFiles() => GetScriptFiles().Select(f => Path.GetFileName(f));

    public static IEnumerable<string> GetScriptCommands(string fileName) => RegexHelpers.GetCommands().Split(File.ReadAllText(fileName)).Where(static c => !string.IsNullOrWhiteSpace(c));

    public static IEnumerable<string> GetCommands(string commandText) => RegexHelpers.GetCommands().Split(commandText).Where(static c => !string.IsNullOrWhiteSpace(c));
}

public partial class RegexHelpers
{
    [GeneratedRegex(@"(?<=[;])(?:\s*--.*\n)?\s*", RegexOptions.Multiline)]
    public static partial Regex GetCommands();
}
