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
    public event EventHandler? FileNeedsSaving;

    public async Task Load()
    {
        if (TabView is not null)
        {
            TabView.TabCloseRequested += (_, e) => RemoveTab(Tabs[TabView.TabItems.IndexOf(e.Tab)]);
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
        var tab = new CodeEditorTab(Interpreter, FileService, AutoSaveService, this);
        tab.CanSaveChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        tab.CanRunChanged += (_, _) => CanRunChanged?.Invoke(this, EventArgs.Empty);

        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = "New file", IconSource = new FontIconSource { Glyph = "\uE943" }, Content = tab.Content });

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
        var tab = CreateNewTab(file);
        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = file.Name, IconSource = new FontIconSource { Glyph = "\uE943" }, Content = tab.Content });

        if (TabView is not null) TabView.SelectedIndex = TabView.TabItems.Count - 1;

        await Task.CompletedTask;
    }
    public async Task OpenTab()
    {
        if (!await FileService.OpenFileAsync()) return;

        var file = FileService.OpenedFiles.Last().Key;
        var tab = CreateNewTab(file);
        Tabs.Add(tab);
        TabView?.TabItems.Add(new TabViewItem { Header = file.Name, Content = tab.Content });

        if (TabView is not null) TabView.SelectedIndex = TabView.TabItems.Count - 1;
    }

    public async Task SaveCurrentTab() { if (Current is not null) await SaveTab(Current); }
    public async Task SaveTab(CodeEditorTab tab)
    {
        if (tab.File is not null) _ = await FileService.SaveFileAsync(tab.File);
        else
        {
            var file = await FileService.SaveContent(tab.Text);

            if (file is null) return;
            tab.File = file;
        }

        tab.Save();
    }

    public void RemoveCurrentTab(bool force = false) { if (Current is not null) RemoveTab(Current, force); }
    public void RemoveTab(CodeEditorTab tab, bool force = false)
    {
        if (TabView is null) return;

        if (tab.File is null && tab.HasChanged && !force)
        {
            FileNeedsSaving?.Invoke(this, EventArgs.Empty);
            return;
        }

        var index = Tabs.IndexOf(tab);
        _ = Tabs.Remove(tab);
        TabView.TabItems.RemoveAt(index);

        if (TabView.TabItems.Count > 0)
        {
            if (index >= TabView.TabItems.Count) index = TabView.TabItems.Count - 1;
            TabView.SelectedIndex = index;
            Tabs[index].Focus();
        }

        if (tab.File is not null) FileService.CloseFile(tab.File);

        tab.Dispose();

        CanSaveChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAndRemoveCurrentTab(bool force = false) { if (Current is not null) await SaveAndRemoveTab(Current, force); }
    public async Task SaveAndRemoveTab(CodeEditorTab tab, bool force = false)
    {
        await SaveTab(tab);
        RemoveTab(tab, force);
    }

    public void MarkTabAsUnsaved(CodeEditorTab tab)
    {
        if (TabView is null) return;

        ((TabViewItem)TabView.TabItems[Tabs.IndexOf(tab)]!).Header = tab.File is not null ? $"{tab.File.Name} ●" : "New File ●";
    }
    public void MarkTabAsSaved(CodeEditorTab tab)
    {
        if (TabView is null) return;
        ((TabViewItem)TabView.TabItems[Tabs.IndexOf(tab)]!).Header = tab.File is not null ? tab.File.Name : "New File";
    }

    private CodeEditorTab CreateNewTab(StorageFile? file = null)
    {
        var tab = new CodeEditorTab(Interpreter, FileService, AutoSaveService, this, file);
        tab.CanSaveChanged += (_, _) => CanSaveChanged?.Invoke(this, EventArgs.Empty);
        tab.CanRunChanged += (_, _) => CanRunChanged?.Invoke(this, EventArgs.Empty);
        return tab;
    }
}