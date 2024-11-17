using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using RandomAccessMachine.App.Helpers;
using RandomAccessMachine.App.Services;
using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Storage;
using Windows.System;
using WinSharp.BindingExtensions;
using WinSharp.Helpers;
using WinSharp.Styles;
using WinUIEditor;

namespace RandomAccessMachine.App.Pages;
public sealed class MainPage : Page, INotifyPropertyChanged
{
    private readonly FileService FileService;
    private readonly Interpreter Interpreter;
    private readonly PersistenceService PersistenceService;

    private readonly CodeEditorControl Editor;
    private readonly Slider SpeedSlider;
    private readonly ToggleSwitch RealtimeToggle;
    private readonly ListView RegistersListView;
    private readonly InfoBar ErrorInfo;

    private readonly ObservableCollection<Register> Registers;

    private Scope CurrentScope = default;

    private readonly RelayCommand NewCommand;
    private readonly RelayCommand OpenCommand;
    private readonly RelayCommand SaveCommand;
    private readonly RelayCommand RunCommand;
    private readonly RelayCommand StopCommand;
    private readonly RelayCommand StepCommand;
    private readonly RelayCommand AddRegisterCommand;
    private readonly RelayCommand DeleteRegistersCommand;

    private CancellationTokenSource? SyntaxCheckCancellationTokenSource;
    private DateTime LastTimeChecked = DateTime.MinValue;

    private CancellationTokenSource? RunnerCancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;


    public MainPage(FileService fileService, Interpreter interpreter, PersistenceService persistenceService, object? parameter = null)
    {
        FileService = fileService;
        Interpreter = interpreter;
        PersistenceService = persistenceService;

        Registers = [.. Interpreter.Registers];

        NewCommand = new(async () =>
        {
            if (Editor!.Editor.Modify)
            {
                var dialog = new ContentDialog
                {
                    Title = "Unsaved changes",
                    Content = "You have unsaved changes. Do you want to save them?",
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Don't save",
                    DefaultButton = ContentDialogButton.Primary
                };
                var result = await dialog.ShowAsync(this);
                if (result is ContentDialogResult.Primary)
                {
                    if (FileService.OpenFile is not null) await FileIO.WriteTextAsync(FileService.OpenFile, Editor!.Editor.GetText(Editor.Editor.TextLength) ?? "");
                    else
                    {
                        await FileService.SaveFileAsync();
                        if (FileService.OpenFile is null) return;

                        CachedFileManager.DeferUpdates(FileService.OpenFile);
                        using var stream = await FileService.OpenFile.OpenStreamForWriteAsync();
                        using var writer = new StreamWriter(stream);
                        writer.WriteLine(Editor?.Editor.GetText(Editor.Editor.TextLength));
                    }

                    Editor?.Editor.SetSavePoint();
                }
            };

            await FileService.CreateFileAsync();

            Editor?.Editor.SetText("");
            Editor?.Editor.SetSavePoint();
        });
        OpenCommand = new(async () =>
        {
            await FileService.OpenFileAsync();
            if (FileService.OpenFile is null) return;

            Editor?.Editor.SetText(await FileIO.ReadTextAsync(FileService.OpenFile));
            Editor?.Editor.SetSavePoint();

            SaveCommand!.NotifyCanExecuteChanged();
            RunCommand!.NotifyCanExecuteChanged();
            StepCommand!.NotifyCanExecuteChanged();
        });
        SaveCommand = new(async () =>
        {
            if (FileService.OpenFile is not null) await FileIO.WriteTextAsync(FileService.OpenFile, Editor!.Editor.GetText(Editor.Editor.TextLength) ?? "");
            else
            {
                await FileService.SaveFileAsync();
                if (FileService.OpenFile is null) return;

                CachedFileManager.DeferUpdates(FileService.OpenFile);
                using var stream = await FileService.OpenFile.OpenStreamForWriteAsync();
                using var writer = new StreamWriter(stream);
                writer.WriteLine(Editor?.Editor.GetText(Editor.Editor.TextLength));
            }

            Editor?.Editor.SetSavePoint();
            SaveCommand!.NotifyCanExecuteChanged();
        }, () => Editor is not null && Editor.Editor.Modify);
        RunCommand = new(() =>
        {
            Interpreter.Speed = SpeedSlider!.Value;
            Interpreter.IsRealTime = RealtimeToggle!.IsOn;

            Interpreter.LoadProgram(CurrentScope, (uint)(Registers.Count - 1));

            Registers.Clear();
            foreach (var register in Interpreter.Registers) Registers.Add(register);

            RunnerCancellationTokenSource = new();
            _ = Interpreter.Execute(RunnerCancellationTokenSource.Token);
        }, () => !Interpreter.IsRunning && CurrentScope != default && CurrentScope.Instructions.Count is not 0 && ErrorInfo!.Severity is not InfoBarSeverity.Error);
        StopCommand = new(() => RunnerCancellationTokenSource?.Cancel(), () => Interpreter.IsRunning);
        StepCommand = new(async () =>
        {

        });
        AddRegisterCommand = new(() =>
        {
            var register = new Register($"R{Registers.Count}", 0);
            Registers.Add(register);
            Interpreter.Registers.Add(register);
            PerformSyntaxCheck();
        });
        DeleteRegistersCommand = new(() =>
        {
            foreach (var register in RegistersListView!.SelectedItems)
            {
                _ = Registers.Remove((Register)register);
                _ = Interpreter.Registers.Remove((Register)register);
            }
        }, () => RegistersListView is not null && RegistersListView.SelectedItems.Count is not 0);

        Interpreter.Started += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            RunCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
        Interpreter.Stopped += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            RunCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
        Interpreter.Stepped += (_, pointer) => DispatcherQueue.TryEnqueue(() =>
        {
            Editor!.Editor.ClearSelections();

            var lineNumber = Interpreter.Memory[(int)pointer].Token.LineNumber - 1;
            var index = Editor.Editor.IndexPositionFromLine(lineNumber, LineCharacterIndexType.None);
            var length = Editor.Editor.LineLength(lineNumber);
            var offset = Editor.Editor.GetLine(lineNumber).TakeWhile(x => char.IsWhiteSpace(x)).Count();
            Editor.Editor.AddSelection(index + offset, index + length);
        });

        Content = new WinSharp.Controls.DockPanel
        {
            new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal,
                Margin = MarginStyles.PageContentMargin,
                Spacing = 4,
                Children =
                {
                    // Run
                    new Button
                    {
                        Content = Symbol.Play.ToIcon(),
                        Height = 36,
                        Command = RunCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.F5 }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Run (F5)")
                    .SetStyle(ButtonStyles.AccentButtonStyle),

                    // Stop
                    new Button
                    {
                        Content = Symbol.Stop.ToIcon(),
                        Height = 36,
                        Command = StopCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.F5, Modifiers = VirtualKeyModifiers.Shift }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Stop (Shift + F5)"),

                    // Step
                    new Button
                    {
                        Content = Symbol.Next.ToIcon(),
                        Height = 36,
                        Command = StepCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.F5, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Step (Ctrl + F5)"),

                    new AppBarSeparator().Dock(Dock.Right),

                    // New file
                    new Button
                    {
                        Content = Symbol.Add.ToIcon(),
                        Height = 36,
                        Command = NewCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.N, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "New file (Ctrl + N)"),

                    // Open file
                    new Button
                    {
                        Content = Symbol.OpenFile.ToIcon(),
                        Height = 36,
                        Command = OpenCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.O, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Open file (Ctrl + O)"),

                    // Save file
                    new Button
                    {
                        Content = Symbol.Save.ToIcon(),
                        Height = 36,
                        Command = SaveCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, "Save file (Ctrl + S)")
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
                        Child = new ScrollViewer
                        {
                            Content = new WinSharp.Controls.StackPanel
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
                                        }
                                        .Bind(Slider.ValueProperty, new Binding
                                        {
                                            Source = PersistenceService,
                                            Path = new PropertyPath("Speed"),
                                            Mode = BindingMode.TwoWay,
                                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                                        })
                                        .BindSelf(out SpeedSlider),

                                        new WinSharp.Controls.StackPanel
                                        {
                                            new TextBlock
                                            {
                                                Text = "Realtime"
                                            }.SetStyle(WinSharpStyles.SettingsCategoryTextBlock),
                                            new ToggleSwitch()
                                            .Bind(ToggleSwitch.IsOnProperty, new Binding
                                            {
                                                Source = PersistenceService,
                                                Path = new PropertyPath("IsRealTime"),
                                                Mode = BindingMode.TwoWay,
                                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                                            })
                                            .BindSelf(out RealtimeToggle)
                                        }
                                    }
                                },

                                new SettingsCard
                                {
                                    Header = "Registers",
                                    HeaderIcon = Symbol.List.ToIcon(),
                                    Description = "View registers in real time, add and remove them",
                                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                    Content = new WinSharp.Controls.StackPanel
                                    {
                                        new WinSharp.Controls.StackPanel
                                        {
                                            new Button
                                            {
                                                Content = Symbol.Add.ToIcon(),
                                                Height = 36,
                                                Command = AddRegisterCommand
                                            },

                                            new Button
                                            {
                                                Content = Symbol.Delete.ToIcon(),
                                                Height = 36,
                                                Command = DeleteRegistersCommand
                                            }
                                        }.SetProperties(x => x.Orientation = Orientation.Horizontal),

                                        new ListView
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                            SelectionMode = ListViewSelectionMode.Multiple,
                                            ItemTemplate = RegisterDataTemplate.Template
                                        }
                                        .BindSelf(out RegistersListView)
                                        .Bind(ItemsControl.ItemsSourceProperty, new Binding
                                        {
                                            Source = Registers,
                                            Mode = BindingMode.OneWay,
                                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                                        })
                                    }.SetProperties(x => x.HorizontalAlignment = HorizontalAlignment.Stretch)
                                }
                            }
                        }
                    }
                    .Dock(Dock.Right)
                    .SetStyle(WinSharpStyles.OverlayBorder),

                    new InfoBar
                    {
                        Content = "No issues found.",
                        Severity = InfoBarSeverity.Success,
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

        Loaded += (_, _) => _ = DispatcherQueue.TryEnqueue(async () =>
        {
            if (FileService.OpenFile is null)
            {
                Editor.Editor.SetText(PersistenceService.Code);
                Editor.Editor.SetSavePoint();
                return;
            }

            Editor.Editor.SetText(await FileIO.ReadTextAsync(FileService.OpenFile));
            Editor.Editor.SetSavePoint();

            Registers[0].Value = 1;
        });

        Editor.Editor.Modified += (_, _) =>
        {
            SaveCommand?.NotifyCanExecuteChanged();

            PersistenceService.Code = Editor.Editor.GetText(Editor.Editor.TextLength);

            _ = EnqueueSyntaxCheck();
        };

        RegistersListView.SelectionChanged += (_, _) =>
        {
            if (RegistersListView.SelectedItems.Contains(Registers[0])) _ = RegistersListView.SelectedItems.Remove(Registers[0]);

            DeleteRegistersCommand.NotifyCanExecuteChanged();
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
                    RunCommand?.NotifyCanExecuteChanged();
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

        var validationResult = LabelResolver.Validate(scope.AsT0);

        if (validationResult.IsT1)
        {
            ErrorInfo.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = validationResult.AsT1;
            return;
        }

        scope = validationResult.AsT0;

        var boundsCheckResult = BoundsChecker.CheckBounds(scope.AsT0, Interpreter);
        if (boundsCheckResult.IsT1)
        {
            ErrorInfo!.Severity = InfoBarSeverity.Error;
            ErrorInfo.Content = boundsCheckResult.AsT1;
            return;
        }

        CurrentScope = scope.AsT0;

        ErrorInfo.Content = "No issues found.";
        ErrorInfo.Severity = InfoBarSeverity.Success;
    }
}