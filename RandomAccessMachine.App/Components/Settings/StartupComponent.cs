using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using RandomAccessMachine.App.Services;
using WinSharp.BindingExtensions;
using WinSharp.Helpers;
using WinSharp.Pages.Components.Settings;
using WinSharp.Styles;

namespace RandomAccessMachine.App.Components.Settings;
public sealed class StartupComponent : SettingsComponent
{
    private readonly StartupService StartupService;

    private readonly ToggleSwitch ShouldOpenLastFileToggle;


    public StartupComponent(StartupService startupService)
    {
        StartupService = startupService;

        Content = new WinSharp.Controls.StackPanel
        {
            new TextBlock
            {
                Text = Resources.Settings_Startup_SectionHeader
            }.SetStyle(SettingsStyles.CategoryTextBlock),
            new SettingsCard
            {
                Header = Resources.Settings_Startup_OpenLastFileHeader,
                HeaderIcon = Symbol.Play.ToIcon(),
                Description = Resources.Settings_Startup_OpenLastFileDescription,
                Margin = MarginStyles.XSmallTopMargin,
                Content = new ToggleSwitch
                {
                    IsOn = StartupService.Settings!.ShouldOpenLastFile
                }
                .BindSelf(out ShouldOpenLastFileToggle)
            }
        };

        ShouldOpenLastFileToggle.Toggled += async (_, _) => await StartupService.UpdateStatus(ShouldOpenLastFileToggle.IsOn);
    }
}
