using WinSharp.FlagInterfaces;
using WinSharp.Services;

namespace RandomAccessMachine.App.Services;
public sealed class StartupService(LocalSettingsService LocalSettingsService, FileService FileService) : IService, IMustInitialize
{
    private const string SETTINGS_KEY = "Startup";

    public StartupSettings? Settings { get; private set; }

    public event EventHandler? StartupCompleted;


    public async Task InitializeAsync()
    {
        Settings = LocalSettingsService.Read<StartupSettings>(SETTINGS_KEY).Match(x => x, _ => new(false, []));

        // TODO: Add proper migration and remove this
        if (Settings.FilePaths is null) Settings = Settings with { FilePaths = [] };

        if (Settings.ShouldOpenLastFile)
            foreach (var path in Settings.FilePaths)
            {
                if (!File.Exists(path)) continue;
                await FileService.OpenFileAsync(path);
            }

        FileService.FileOpened += async (sender, file) =>
        {
            if (!Settings.ShouldOpenLastFile) return;

            Settings.FilePaths.Add(file.Path);
            await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);
        };

        FileService.FileClosed += async (sender, file) =>
        {
            if (!Settings.ShouldOpenLastFile) return;

            _ = Settings.FilePaths.Remove(file.Path);
            await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);
        };

        StartupCompleted?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateStatus(bool shouldOpenLastFile)
    {
        Settings = Settings! with { ShouldOpenLastFile = shouldOpenLastFile };
        if (Settings.ShouldOpenLastFile)
            foreach (var path in FileService.OpenedFiles)
                Settings.FilePaths.Add(path.Key.Path);
        else Settings.FilePaths.Clear();

        await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);
    }
}

public record StartupSettings(bool ShouldOpenLastFile, List<string> FilePaths);