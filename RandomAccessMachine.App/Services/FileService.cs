using Windows.Storage;
using Windows.Storage.Pickers;
using WinSharp.FlagInterfaces;
using WinSharp.Services;

namespace RandomAccessMachine.App.Services;
public sealed class FileService(FilePickerService FilePickerService) : IService
{
    public Dictionary<StorageFile, string> OpenedFiles { get; } = [];

    public event EventHandler<StorageFile>? FileOpened;
    public event EventHandler<StorageFile>? FileSaved;
    public event EventHandler<StorageFile>? FileClosed;


    public async Task OpenFileAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);

        OpenedFiles.Add(file, await FileIO.ReadTextAsync(file));
        FileOpened?.Invoke(this, file);
    }
    public async Task<bool> OpenFileAsync()
    {
        var file = await FilePickerService.OpenSingleFileAsync(PickerLocationId.DocumentsLibrary, ".txt");
        if (file is null) return false;

        OpenedFiles.Add(file, await FileIO.ReadTextAsync(file));
        FileOpened?.Invoke(this, file);
        return true;
    }

    public async Task<bool> SaveFileAsync()
    {
        var file = await FilePickerService.SaveFileAsync(DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss"), PickerLocationId.DocumentsLibrary, ".txt");
        if (file is null) return false;

        OpenedFiles.Add(file, await FileIO.ReadTextAsync(file));
        FileSaved?.Invoke(this, file);
        return true;
    }
    public async Task<bool> SaveFileAsync(StorageFile file)
    {
        if (!OpenedFiles.TryGetValue(file, out var value)) return await SaveFileAsync();

        await FileIO.WriteTextAsync(file, value);
        FileSaved?.Invoke(this, file);
        return true;
    }
    public async Task<bool> SaveFileWithContentAsync(StorageFile file, string content)
    {
        if (!OpenedFiles.ContainsKey(file)) return await SaveFileAsync();

        OpenedFiles[file] = content;

        await FileIO.WriteTextAsync(file, content);
        FileSaved?.Invoke(this, file);
        return true;
    }
    public async Task<StorageFile?> SaveContent(string content)
    {
        var file = await FilePickerService.SaveFileAsync(DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss"), PickerLocationId.DocumentsLibrary, ".txt");
        if (file is null) return null;

        OpenedFiles.Add(file, content);

        await FileIO.WriteTextAsync(file, content);
        FileSaved?.Invoke(this, file);
        return file;
    }

    public void CloseFile(StorageFile file)
    {
        _ = OpenedFiles.Remove(file);
        FileClosed?.Invoke(this, file);
    }
}
