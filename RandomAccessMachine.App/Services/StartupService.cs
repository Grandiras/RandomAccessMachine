using Windows.Storage;
using WinSharp.FlagInterfaces;
using WinSharp.Services;

namespace RandomAccessMachine.App.Services;
public sealed class StartupService(LocalSettingsService LocalSettingsService, FileService FileService, PersistenceService PersistenceService) : IService, IMustInitialize
{
    private const string SETTINGS_KEY = "Startup";

    public StartupSettings? Settings { get; private set; }


    public async Task InitializeAsync()
    {
        Settings = LocalSettingsService.Read<StartupSettings>(SETTINGS_KEY).Match(x => x, _ => new(false, ""));
        if (Settings.ShouldOpenLastFile && !string.IsNullOrWhiteSpace(Settings.LastFilePath))
        {
            await FileService.OpenFileAsync(Settings.LastFilePath);
            PersistenceService.Code = await FileIO.ReadTextAsync(FileService.OpenFile);
        }

        FileService.FileOpened += async (sender, _) =>
        {
            Settings = Settings! with { LastFilePath = FileService.OpenFile!.Path };
            await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);
        };
    }

    public async Task UpdateStatus(bool shouldOpenLastFile)
    {
        Settings = Settings! with { ShouldOpenLastFile = shouldOpenLastFile };
        if (Settings.ShouldOpenLastFile && FileService.OpenFile is not null) Settings = Settings! with { LastFilePath = FileService.OpenFile.Path };

        await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);
    }
}

public record StartupSettings(bool ShouldOpenLastFile, string LastFilePath);