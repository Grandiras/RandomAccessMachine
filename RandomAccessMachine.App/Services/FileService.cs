using Windows.Storage;
using Windows.Storage.Pickers;
using WinSharp.FlagInterfaces;
using WinSharp.Services;

namespace RandomAccessMachine.App.Services;
public sealed class FileService(FilePickerService FilePickerService) : IService
{
    public StorageFile? OpenFile { get; private set; }


    public async Task CreateFileAsync()
    {
        OpenFile = null;
        await Task.CompletedTask;
    }
    public async Task OpenFileAsync() => OpenFile = await FilePickerService.OpenSingleFileAsync(PickerLocationId.DocumentsLibrary, ".txt");
    public async Task SaveFileAsync() => OpenFile = await FilePickerService.SaveFileAsync(DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss"), PickerLocationId.DocumentsLibrary, ".txt");
}
