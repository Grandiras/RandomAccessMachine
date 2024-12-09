using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RandomAccessMachine.App.Components;
using RandomAccessMachine.Backend.Interpreter;
using Windows.Storage;
using Windows.System;
using WinSharp.FlagInterfaces;

namespace RandomAccessMachine.App.Services;
public sealed class TabService(FileService FileService, Interpreter Interpreter, AutoSaveService AutoSaveService) : IService
{
    private readonly List<CodeEditorTab> Tabs = [];

    public CodeEditorTab? Current => TabView is not null && TabView.SelectedIndex >= 0 ? Tabs[TabView.SelectedIndex] : null;
    public bool HasCurrentChanged => Current?.HasChanged ?? false;

    public TabView? TabView { get; set; }

    public event EventHandler? CanSaveChanged;
    public event EventHandler? CanRunChanged;
    public event EventHandler<int>? FileNeedsSaving;

    public async Task Load()
    {
        if (TabView is not null)
        {
            TabView.TabCloseRequested += async (_, e) =>
            {
                var index = TabView.TabItems.IndexOf(e.Tab);
                var file = Tabs[index].File;

                var tab = Tabs[index];
                if (tab.HasChanged && tab.File is null)
                {
                    FileNeedsSaving?.Invoke(this, index);
                    return;
                }

                Tabs.RemoveAt(index);
                TabView.TabItems.RemoveAt(index);

                if (TabView.TabItems.Count > 0)
                {
                    if (index >= TabView.TabItems.Count) index = TabView.TabItems.Count - 1;
                    TabView.SelectedIndex = index;
                    Tabs[index].Focus();
                }

                if (file is not null) FileService.CloseFile(file);

                CanSaveChanged?.Invoke(this, EventArgs.Empty);
            };

            TabView.AddTabButtonCommand = new RelayCommand(async () => await AddNewTab());

            var accelerator = new KeyboardAccelerator { Key = VirtualKey.N, Modifiers = VirtualKeyModifiers.Control };
            accelerator.Invoked += (_, _) => _ = AddNewTab();
            TabView.KeyboardAccelerators.Add(accelerator);

            accelerator = new KeyboardAccelerator { Key = VirtualKey.F4, Modifiers = VirtualKeyModifiers.Control };
            accelerator.Invoked += (_, _) =>
            {
                if (TabView.SelectedIndex < 0) return;
                TabView.TabItems.RemoveAt(TabView.SelectedIndex);
            };
            TabView.KeyboardAccelerators.Add(accelerator);
        }

        if (FileService.OpenedFiles.Count is 0)
        {
            await AddNewTab();
            return;
        }

        foreach (var file in FileService.OpenedFiles) await AddTab(file.Key);
    }

    public async Task AddNewTab()
    {
        var tab = new CodeEditorTab(Interpreter, FileService, AutoSaveService);
        tab.CanSaveChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        tab.CanRunChanged += (_, _) => CanRunChanged?.Invoke(this, EventArgs.Empty);

        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = "New file", Content = tab.Content });

        if (TabView is not null) TabView.SelectedIndex = TabView.TabItems.Count - 1;

        await Task.CompletedTask;
    }
    public async Task AddTab(string path)
    {
        await FileService.OpenFileAsync(path);
        await AddTab(FileService.OpenedFiles.Last().Key);
    }
    public async Task AddTab(StorageFile file)
    {
        var tab = new CodeEditorTab(Interpreter, FileService, AutoSaveService, file);
        tab.CanSaveChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        tab.CanRunChanged += (_, _) => CanRunChanged?.Invoke(this, EventArgs.Empty);

        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = file.Name, Content = tab.Content });

        if (TabView is not null) TabView.SelectedIndex = TabView.TabItems.Count - 1;

        await Task.CompletedTask;
    }
    public async Task OpenTab()
    {
        if (!await FileService.OpenFileAsync()) return;

        var file = FileService.OpenedFiles.Last().Key;

        var tab = new CodeEditorTab(Interpreter, FileService, AutoSaveService, file);
        tab.CanSaveChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        tab.CanRunChanged += (_, _) => CanRunChanged?.Invoke(this, EventArgs.Empty);

        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = file.Name, Content = tab.Content });

        if (TabView is not null) TabView.SelectedIndex = TabView.TabItems.Count - 1;
    }

    public async Task SaveCurrentTab()
    {
        if (Current is null) return;

        if (Current.File is not null) _ = await FileService.SaveFileAsync(Current.File);
        else
        {
            var file = await FileService.SaveContent(Current.Text);
            Current.File = file;
        }

        Current.Save();
        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveTab(int index)
    {
        if (TabView is null) return;
        if (index < 0 || index >= TabView.TabItems.Count) return;

        var file = Tabs[index].File;

        Tabs.RemoveAt(index);
        TabView.TabItems.RemoveAt(index);

        if (TabView.TabItems.Count > 0)
        {
            if (index >= TabView.TabItems.Count) index = TabView.TabItems.Count - 1;
            TabView.SelectedIndex = index;
            Tabs[index].Focus();
        }

        if (file is not null) FileService.CloseFile(file);

        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    }
}