using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using RandomAccessMachine.App.Helpers;
using RandomAccessMachine.App.Services;
using RandomAccessMachine.Backend.Interpreter;
using RandomAccessMachine.Backend.Specification;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.System;
using WinSharp.BindingExtensions;
using WinSharp.Helpers;
using WinSharp.Styles;

namespace RandomAccessMachine.App.Pages;
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    private readonly Interpreter Interpreter;
    private readonly PersistenceService PersistenceService;
    private readonly TabService TabService;

    private readonly Slider SpeedSlider;
    private readonly ToggleSwitch RealtimeToggle;
    private readonly ListView RegistersListView;
    private readonly TabView MainEditorTabView;

    private readonly ObservableCollection<Register> Registers;

    private readonly RelayCommand OpenCommand;
    private readonly RelayCommand SaveCommand;
    private readonly RelayCommand RunCommand;
    private readonly RelayCommand StopCommand;
    private readonly RelayCommand StepCommand;
    private readonly RelayCommand AddRegisterCommand;
    private readonly RelayCommand DeleteRegistersCommand;

    private CancellationTokenSource? RunnerCancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;


    public MainPage(Interpreter interpreter, PersistenceService persistenceService, TabService tabService)
    {
        Interpreter = interpreter;
        PersistenceService = persistenceService;
        TabService = tabService;

        Registers = [.. Interpreter.Registers];

        OpenCommand = new(async () =>
        {
            await TabService.OpenTab();

            SaveCommand!.NotifyCanExecuteChanged();
            RunCommand!.NotifyCanExecuteChanged();
            StepCommand!.NotifyCanExecuteChanged();
        });
        SaveCommand = new(async () => await TabService.SaveCurrentTab(), () => TabService.HasCurrentChanged);
        RunCommand = new(() =>
        {
            Interpreter.Speed = SpeedSlider!.Value;
            Interpreter.IsRealTime = RealtimeToggle!.IsOn;

            if (TabService.Current is null) return;

            Interpreter.LoadProgram(TabService.Current.CurrentScope, (uint)(Registers.Count - 1));

            Registers.Clear();
            foreach (var register in Interpreter.Registers) Registers.Add(register);

            RunnerCancellationTokenSource = new();
            _ = Interpreter.Execute(RunnerCancellationTokenSource.Token);
        }, () => !Interpreter.IsRunning && TabService.Current is not null && TabService.Current.CurrentScope != default && TabService.Current.CurrentScope.Instructions.Count is not 0 && TabService.Current.CanExecute);
        StopCommand = new(() => RunnerCancellationTokenSource?.Cancel(), () => Interpreter.IsRunning);
        StepCommand = new(() => { });
        AddRegisterCommand = new(() =>
        {
            var register = new Register($"R{Registers.Count}", 0);
            Registers.Add(register);
            Interpreter.Registers.Add(register);

            _ = TabService.Current?.EnqueueSyntaxCheck();
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
        Interpreter.Stepped += (_, pointer) =>
        {
            var lineNumber = Interpreter.Memory[(int)pointer].Token.LineNumber - 1;
            TabService.Current?.SelectOnly(lineNumber);
        };

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
                        KeyboardAccelerators = { new() { Key = VirtualKey.F5 } }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Run_Tooltip} (F5)")
                    .SetStyle(ButtonStyles.AccentButtonStyle),

                    // Stop
                    new Button
                    {
                        Content = Symbol.Stop.ToIcon(),
                        Height = 36,
                        Command = StopCommand,
                        KeyboardAccelerators = { new() { Key = VirtualKey.F5, Modifiers = VirtualKeyModifiers.Shift } }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Stop_Tooltip} (Shift + F5)"),

                    // Step
                    new Button
                    {
                        Content = Symbol.Next.ToIcon(),
                        Height = 36,
                        Command = StepCommand,
                        KeyboardAccelerators = { new() { Key = VirtualKey.F5, Modifiers = VirtualKeyModifiers.Control } }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_Step_Tooltip} (Ctrl + F5)"),

                    new AppBarSeparator(),

                    // Open file
                    new Button
                    {
                        Content = Symbol.OpenFile.ToIcon(),
                        Height = 36,
                        Command = OpenCommand,
                        KeyboardAccelerators = { new() { Key = VirtualKey.O, Modifiers = VirtualKeyModifiers.Control } }
                    }
                    .SetAttached(ToolTipService.ToolTipProperty, $"{App.Resources.MainPage_OpenFile_Tooltip} (Ctrl + O)"),

                    // Save file
                    new Button
                    {
                        Content = Symbol.Save.ToIcon(),
                        Height = 36,
                        Command = SaveCommand,
                        KeyboardAccelerators = { new() { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control } }
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

                    new TabView
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden,
                    }
                    .BindSelf(out MainEditorTabView)
                    .Dock(Dock.Bottom)
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
            await TabService.Load();

            SaveCommand.NotifyCanExecuteChanged();

            Registers[0].Value = 1;
        });

        RegistersListView.SelectionChanged += (_, _) =>
        {
            if (RegistersListView.SelectedItems.Contains(Registers[0])) _ = RegistersListView.SelectedItems.Remove(Registers[0]);

            DeleteRegistersCommand.NotifyCanExecuteChanged();
        };

        TabService.TabView = MainEditorTabView;

        TabService.CanSaveChanged += (_, _) => SaveCommand.NotifyCanExecuteChanged();
        TabService.CanRunChanged += (_, _) =>
        {
            RunCommand.NotifyCanExecuteChanged();
            StepCommand.NotifyCanExecuteChanged();
        };

        TabService.FileNeedsSaving += async (_, index) =>
        {
            var dialog = new ContentDialog
            {
                Title = App.Resources.MainPage_New_UnsavedChanges_Title,
                Content = App.Resources.MainPage_New_UnsavedChanges_Content,
                PrimaryButtonText = App.Resources.MainPage_New_UnsavedChanges_Save,
                SecondaryButtonText = App.Resources.MainPage_New_UnsavedChanges_DontSave,
                CloseButtonText = App.Resources.MainPage_New_UnsavedChanges_Cancel
            };
            var result = await dialog.ShowAsync(this);

            if (result is ContentDialogResult.None) return;

            if (result is ContentDialogResult.Primary) await TabService.SaveCurrentTab();

            TabService.RemoveTab(index);
        };
    }
}