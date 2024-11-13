using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using RandomAccessMachine.App.Components;
using RandomAccessMachine.App.Services;
using RandomAccessMachine.Backend.Interpreter;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using WinSharp.BindingExtensions;
using WinSharp.Helpers;
using WinSharp.Styles;
using WinUIEditor;
using WinUIEx;

namespace RandomAccessMachine.App.Pages;
public sealed class MainPage : Page
{
    private readonly CodeEditorControl Editor;
    private readonly Slider SpeedSlider;
    private readonly ToggleSwitch RealtimeToggle;
    private readonly WinSharp.Controls.StackPanel RegistersStackPanel;
    private readonly InfoBar ErrorInfo;

    private readonly RelayCommand OpenCommand;
    private readonly RelayCommand SaveCommand;
    private readonly RelayCommand RunCommand;
    private readonly RelayCommand StepCommand;

    private readonly ObservableCollection<RegisterComponent> Registers;

    private readonly bool IsRunning = false;

    private CancellationTokenSource SyntaxCheckCancellationTokenSource;
    private DateTime LastTimeChecked = DateTime.MinValue;


    public MainPage(WindowEx window, object? parameter = null)
    {
        var file = FileSystemService.GetScriptFiles().FirstOrDefault(f => f.EndsWith(parameter?.ToString() ?? "null"));

        OpenCommand = new(async () =>
        {
            var filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            filePicker.FileTypeFilter.Add(".txt");

            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(filePicker, hWnd);

            var openFile = await filePicker.PickSingleFileAsync();

            file = openFile!.Path;

            Editor?.Editor.SetText(file is not null ? FileSystemService.GetScriptCommands(file).Aggregate((x, y) => x + "\n\n" + y) : "");
            Editor?.Editor.SetSavePoint();

            SaveCommand!.NotifyCanExecuteChanged();
            RunCommand!.NotifyCanExecuteChanged();
            StepCommand!.NotifyCanExecuteChanged();
        });
        SaveCommand = new(async () =>
        {
            if (file is not null) File.WriteAllText(file, Editor?.Editor.GetText(Editor.Editor.TextLength) ?? FileSystemService.GetScriptCommands(file).Aggregate((x, y) => x + "\n\n" + y));
            else
            {
                var savePicker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss")
                };
                savePicker.FileTypeChoices.Add("TXT file", [".txt"]);

                var hWnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(savePicker, hWnd);

                var saveFile = await savePicker.PickSaveFileAsync();
                if (saveFile is not null)
                {
                    CachedFileManager.DeferUpdates(saveFile);

                    using var stream = await saveFile.OpenStreamForWriteAsync();
                    using var writer = new StreamWriter(stream);
                    writer.WriteLine(Editor?.Editor.GetText(Editor.Editor.TextLength));

                    file = saveFile!.Path;
                }
            }

            Editor?.Editor.SetSavePoint();
            SaveCommand!.NotifyCanExecuteChanged();
        }, () => Editor is not null && !string.IsNullOrWhiteSpace(Editor.Editor.GetText(Editor.Editor.TextLength)) && Editor.Editor.Modify);
        RunCommand = new(async () =>
        {

        }, () => Editor is not null && !string.IsNullOrWhiteSpace(Editor.Editor.GetText(Editor.Editor.TextLength)) && !IsRunning);
        StepCommand = new(async () =>
        {

        }, () => Editor is not null && !string.IsNullOrWhiteSpace(Editor.Editor.GetText(Editor.Editor.TextLength)) && !IsRunning);

        Registers = [];

        Content = new WinSharp.Controls.DockPanel
        {
            new DockPanel
            {
                Margin = MarginStyles.PageContentMargin,
                LastChildFill = false,
                Children =
                {
                    new Button
                    {
                        Content = Symbol.Save.ToIcon(),
                        Height = 36,
                        Margin = MarginStyles.XXSmallLeftMargin,
                        Command = SaveCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Save file")
                    .Dock(Dock.Right)
                    .SetStyle(ButtonStyles.AccentButtonStyle),

                    new Button
                    {
                        Content = Symbol.OpenFile.ToIcon(),
                        Height = 36,
                        Margin = MarginStyles.XXSmallLeftMargin,
                        Command = OpenCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.O, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Open file")
                    .Dock(Dock.Right),

                    new AppBarSeparator().Dock(Dock.Right),

                    new Button
                    {
                        Content = Symbol.Next.ToIcon(),
                        Height = 36,
                        Margin = MarginStyles.XXSmallRightMargin,
                        Command = StepCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.F5, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Run")
                    .Dock(Dock.Right),

                    new Button
                    {
                        Content = Symbol.Play.ToIcon(),
                        Height = 36,
                        Margin = MarginStyles.XXSmallRightMargin with { Left = MarginStyles.XXSmallLeftMargin.Left },
                        Command = RunCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.F5 }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Step")
                    .Dock(Dock.Right)
                    .SetStyle(ButtonStyles.AccentButtonStyle)
                }
            }.Dock(Dock.Top),
            new DockPanel
            {
                Margin = MarginStyles.PageContentMargin with { Left = 40, Right = 40},
                Children =
                {
                    new Border
                    {
                        Width = 350,
                        Padding = MarginStyles.XSmallLeftTopRightBottomMargin,
                        Child = new WinSharp.Controls.StackPanel
                        {
                            new SettingsCard
                            {
                                Header = "Speed (Hz)",
                                HeaderIcon = new FontIcon { Glyph = "\uEC4A" },
                                Description = "Control how fast to simulate",
                                Content = new WinSharp.Controls.StackPanel
                                {
                                    new Slider
                                    {
                                        SnapsTo = SliderSnapsTo.StepValues,
                                        StepFrequency = 0.1,
                                        Minimum = 0.1,
                                        Maximum = 10,
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                    }.BindSelf(out SpeedSlider),

                                    new WinSharp.Controls.StackPanel
                                    {
                                        new TextBlock
                                        {
                                            Text = "Realtime"
                                        }.SetStyle(WinSharpStyles.SettingsCategoryTextBlock),
                                        new ToggleSwitch().BindSelf(out RealtimeToggle)
                                    }
                                }
                            },

                            new SettingsCard
                            {
                                Header = "Registers",
                                HeaderIcon = Symbol.List.ToIcon(),
                                Description = "View registers in real time, add and remove them",
                                Content = new WinSharp.Controls.StackPanel
                                {
                                    new WinSharp.Controls.StackPanel().BindSelf(out RegistersStackPanel),

                                    new Button
                                    {
                                        Content = Symbol.Add.ToIcon()
                                    }
                                }
                            }
                        }
                    }
                    .Dock(Dock.Right)
                    .SetStyle(WinSharpStyles.OverlayBorder),

                    new InfoBar
                    {
                        Severity = InfoBarSeverity.Error,
                        IsOpen = true,
                        IsClosable = false,
                        Margin = MarginStyles.XSmallLeftTopRightBottomMargin,
                    }
                    .BindSelf(out ErrorInfo)
                    .Dock(Dock.Bottom),

                    new CodeEditorControl
                    {
                        Margin = MarginStyles.SmallRightMargin
                    }.BindSelf(out Editor)
                }
            }
        }.SetProperties(x =>
        {
            x.Margin = new(0, -40, 0, 0);
            x.HorizontalAlignment = HorizontalAlignment.Stretch;
            x.VerticalAlignment = VerticalAlignment.Stretch;
            x.ChildrenTransitions =
            [
                new EntranceThemeTransition { IsStaggeringEnabled = false, FromVerticalOffset = 50 },
                new RepositionThemeTransition { IsStaggeringEnabled = true }
            ];
        });

        if (file is not null)
        {
            Editor.Editor.SetText(file is not null ? FileSystemService.GetScriptCommands(file).Aggregate((x, y) => x + "\n\n" + y) : "");
            Editor.Editor.SetSavePoint();
        }

        Editor.Editor.Modified += (_, _) =>
        {
            SaveCommand.NotifyCanExecuteChanged();
            RunCommand.NotifyCanExecuteChanged();
            StepCommand.NotifyCanExecuteChanged();

            _ = EnqueueSyntaxCheck();
        };
    }

    private async Task EnqueueSyntaxCheck()
    {
        SyntaxCheckCancellationTokenSource?.Cancel();
        SyntaxCheckCancellationTokenSource = new CancellationTokenSource();

        var token = SyntaxCheckCancellationTokenSource.Token;
        var delay = TimeSpan.FromSeconds(1);
        var timeSinceLastCheck = DateTime.Now - LastTimeChecked;

        if (timeSinceLastCheck >= delay)
        {
            PerformSyntaxCheck();
            LastTimeChecked = DateTime.Now;
        }
        else
        {
            try
            {
                var timeToWait = delay - timeSinceLastCheck;
                await Task.Delay(timeToWait, token);

                if (!token.IsCancellationRequested)
                {
                    PerformSyntaxCheck();
                    LastTimeChecked = DateTime.Now;
                }
            }
            catch (TaskCanceledException) { } // Syntax check was canceled due to new modifications
        }
    }
    private void PerformSyntaxCheck()
    {
        var tokens = Tokenizer.Tokenize(Editor.Editor.GetText(Editor.Editor.TextLength));

        if (tokens.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = tokens.AsT1;
            return;
        }

        var scope = Parser.Parse(tokens.AsT0);

        if (scope.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = scope.AsT1;
            return;
        }

        ErrorInfo.Content = "No issues found.";
        ErrorInfo.Severity = InfoBarSeverity.Success;
    }
}