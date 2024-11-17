using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using RandomAccessMachine.App.Services;
using WinSharp.BindingExtensions;
using WinSharp.Helpers;
using WinSharp.Pages.Components.Settings;
using WinSharp.Styles;

namespace RandomAccessMachine.App.Components.Settings;
public sealed class AutoSaveComponent : SettingsComponent
{
    private readonly AutoSaveService AutoSaveService;

    private readonly ToggleSwitch AutoSaveSwitch;
    private readonly SettingsCard IntervalCard;
    private readonly NumberBox IntervalNumberBox;


    public AutoSaveComponent(AutoSaveService autoSaveService)
    {
        AutoSaveService = autoSaveService;

        Content = new WinSharp.Controls.StackPanel
        {
            new TextBlock
            {
                Text = "Auto Save"
            }.SetStyle(SettingsStyles.CategoryTextBlock),
            new SettingsExpander
            {
                Header = "Auto Save",
                HeaderIcon = Symbol.Save.ToIcon(),
                Description = "Automatically save your work",
                Margin = MarginStyles.XSmallTopMargin,
                Content = new ToggleSwitch
                {
                    IsOn = AutoSaveService.Settings.ShouldAutoSave
                }.BindSelf(out AutoSaveSwitch),
                Items =
                {
                    new SettingsCard
                    {
                        Header = "Interval",
                        HeaderIcon = Symbol.Clock.ToIcon(),
                        Description = "The interval in seconds between auto saves",
                        IsEnabled = AutoSaveService.Settings.ShouldAutoSave,
                        Content = new NumberBox
                        {
                            Value = AutoSaveService.Settings.Interval,
                            Minimum = 5,
                            Maximum = 300
                        }.BindSelf(out IntervalNumberBox)
                    }.BindSelf(out IntervalCard)
                }
            }
        };

        AutoSaveSwitch.Toggled += async (_, _) =>
        {
            await AutoSaveService.UpdateStatus(AutoSaveSwitch.IsOn);
            IntervalCard.IsEnabled = AutoSaveSwitch.IsOn;
        };

        IntervalNumberBox.ValueChanged += async (_, _) => await AutoSaveService.UpdateInterval((uint)IntervalNumberBox.Value);
    }
}
