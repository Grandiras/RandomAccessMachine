using Windows.Storage;
using WinSharp.FlagInterfaces;
using WinSharp.Services;

namespace RandomAccessMachine.App.Services;
public sealed class AutoSaveService(LocalSettingsService LocalSettingsService, FileService FileService, PersistenceService PersistenceService) : IService, IMustInitialize
{
    private const string SETTINGS_KEY = "AutoSave";
    private PeriodicTimer? Timer;
    private Task? Task;

    private CancellationTokenSource CancellationTokenSource = new();

    public AutoSaveSettings? Settings { get; private set; }

    public event EventHandler? AutoSaved;


    public async Task InitializeAsync()
    {
        Settings = LocalSettingsService.Read<AutoSaveSettings>(SETTINGS_KEY).Match(x => x, _ => new(false, 30));

        Timer = new PeriodicTimer(TimeSpan.FromSeconds(Settings.Interval));
        Task = Task.Run(Update(), CancellationTokenSource.Token);

        await Task.CompletedTask;
    }


    private Func<Task?> Update() => async () =>
    {
        while (Settings!.ShouldAutoSave && await Timer!.WaitForNextTickAsync(CancellationTokenSource.Token))
        {
            if (FileService.OpenFile is null) continue;

            CachedFileManager.DeferUpdates(FileService.OpenFile);
            using var stream = await FileService.OpenFile.OpenStreamForWriteAsync();
            using var writer = new StreamWriter(stream);
            writer.WriteLine(PersistenceService.Code);

            AutoSaved?.Invoke(this, EventArgs.Empty);
        }
    };

    public async Task UpdateStatus(bool shouldAutoSave)
    {
        Settings = Settings! with { ShouldAutoSave = shouldAutoSave };
        await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);

        if (!Settings.ShouldAutoSave) CancellationTokenSource.Cancel();
        else
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            CancellationTokenSource = new();

            Task = Task.Run(Update(), CancellationTokenSource.Token);
        }
    }
    public async Task UpdateInterval(uint interval)
    {
        Settings = Settings! with { Interval = interval };
        await LocalSettingsService.SaveAsync(SETTINGS_KEY, Settings);

        Timer!.Period = TimeSpan.FromSeconds(Settings.Interval);
    }
}

public record AutoSaveSettings(bool ShouldAutoSave, uint Interval);
