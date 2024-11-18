using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using RandomAccessMachine.App.Helpers;
using RandomAccessMachine.App.Services;
using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
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

    private readonly RelayCommand CopyCommand;
    private readonly RelayCommand PasteCommand;
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


    public MainPage(FileService fileService, Interpreter interpreter, PersistenceService persistenceService, AutoSaveService autoSaveService, object? parameter = null)
    {
        FileService = fileService;
        Interpreter = interpreter;
        PersistenceService = persistenceService;

        Registers = [.. Interpreter.Registers];

        CopyCommand = new(async () =>
        {
            // get the text out of all selected ranges combined and copy it to the clipboard
            var text = Editor!.Editor.GetSelText();
            // make a content dialog to show the copied text
            var dialog = new ContentDialog
            {
                Title = "Copy",
                Content = text,
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "Close",
                IsSecondaryButtonEnabled = false
            };
            _ = DispatcherQueue.TryEnqueue(async () => _ = await dialog.ShowAsync(this));
            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }, () => Editor is not null && Editor.Editor.Selections is not 0 && !Editor.Editor.SelectionEmpty);
        PasteCommand = new(async () => _ = DispatcherQueue.TryEnqueue(async () =>
        {
            var content = Clipboard.GetContent();
            var text = await content.GetTextAsync();
            Editor!.Editor.ReplaceSel(text);
        }), () => Editor is not null && Clipboard.GetContent().Contains(StandardDataFormats.Text));
        NewCommand = new(async () =>
        {
            if (Editor!.Editor.Modify)
            {
                var dialog = new ContentDialog
                {
                    Title = App.Resources.MainPage_New_UnsavedChanges_Title,
                    Content = App.Resources.MainPage_New_UnsavedChanges_Content,
                    PrimaryButtonText = App.Resources.MainPage_New_UnsavedChanges_Save,
                    SecondaryButtonText = App.Resources.MainPage_New_UnsavedChanges_DontSave,
                    CloseButtonText = App.Resources.MainPage_New_UnsavedChanges_Cancel,
                    DefaultButton = ContentDialogButton.Primary
                };
                var result = await dialog.ShowAsync(this);
                if (result is not ContentDialogResult.Primary or ContentDialogResult.Secondary) return;
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
                    // Copy
                    new Button
                    {
                        Content = Symbol.Copy.ToIcon(),
                        Height = 36,
                        Command = CopyCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.C, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Copy_Tooltip} (Ctrl + C)"),

                    // Paste
                    new Button
                    {
                        Content = Symbol.Paste.ToIcon(),
                        Height = 36,
                        Command = PasteCommand,
                        KeyboardAccelerators =
                        {
                            new() { Key = VirtualKey.V, Modifiers = VirtualKeyModifiers.Control }
                        }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Paste_Tooltip} (Ctrl + V)"),

                    new AppBarSeparator(),

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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Run_Tooltip} (F5)")
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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Stop_Tooltip} (Shift + F5)"),

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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Step_Tooltip} (Ctrl + F5)"),

                    new AppBarSeparator(),

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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_NewFile_Tooltip} (Ctrl + N)"),

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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_OpenFile_Tooltip} (Ctrl + O)"),

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
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_SaveFile_Tooltip} (Ctrl + S)")
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
                                    Header = App.Resources.MainPage_Speed_Title,
                                    HeaderIcon = new FontIcon { Glyph = "\uEC4A" },
                                    Description = App.Resources.MainPage_Speed_Description,
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
                                        .BindTwoWay(RangeBase.ValueProperty, PersistenceService, nameof(PersistenceService.Speed))
                                        .BindSelf(out SpeedSlider),

                                        new WinSharp.Controls.StackPanel
                                        {
                                            new TextBlock
                                            {
                                                Text = App.Resources.MainPage_Speed_Realtime
                                            }.SetStyle(WinSharpStyles.SettingsCategoryTextBlock),
                                            new ToggleSwitch()
                                            .BindTwoWay(ToggleSwitch.IsOnProperty, PersistenceService, nameof(PersistenceService.IsRealTime))
                                            .BindSelf(out RealtimeToggle)
                                        }
                                    }
                                },

                                new SettingsCard
                                {
                                    Header = App.Resources.MainPage_Registers_Title,
                                    HeaderIcon = Symbol.List.ToIcon(),
                                    Description = App.Resources.MainPage_Registers_Description,
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
                                        .BindOneWay(ItemsControl.ItemsSourceProperty, Registers)
                                    }.SetProperties(x => x.HorizontalAlignment = HorizontalAlignment.Stretch)
                                }
                            }
                        }
                    }
                    .Dock(Dock.Right)
                    .SetStyle(WinSharpStyles.OverlayBorder),

                    new InfoBar
                    {
                        Content = App.Resources.MainPage_Issues_None,
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

            SaveCommand.NotifyCanExecuteChanged();

            Registers[0].Value = 1;
        });

        Editor.Editor.Modified += (_, _) =>
        {
            SaveCommand.NotifyCanExecuteChanged();

            PersistenceService.Code = Editor.Editor.GetText(Editor.Editor.TextLength);

            _ = EnqueueSyntaxCheck();
        };

        Editor.Editor.UpdateUI += (_, _) =>
        {
            CopyCommand.NotifyCanExecuteChanged();
            PasteCommand.NotifyCanExecuteChanged();
        };

        RegistersListView.SelectionChanged += (_, _) =>
        {
            if (RegistersListView.SelectedItems.Contains(Registers[0])) _ = RegistersListView.SelectedItems.Remove(Registers[0]);

            DeleteRegistersCommand.NotifyCanExecuteChanged();
        };

        autoSaveService.AutoSaved += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            Editor.Editor.SetSavePoint();
            SaveCommand.NotifyCanExecuteChanged();
        });
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

        ErrorInfo.Content = App.Resources.MainPage_Issues_None;
        ErrorInfo.Severity = InfoBarSeverity.Success;

        RunCommand?.NotifyCanExecuteChanged();
    }
}