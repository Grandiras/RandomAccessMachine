using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using RandomAccessMachine.App;
using RandomAccessMachine.App.Pages;
using System.Diagnostics;
using WinSharp;
using WinSharp.Pages;
using WinSharp.Pages.Components.Settings;
using WinSharp.Services;
using WinSharp.Windows;

new AppBuilder()

.AddNavigationWindow(window => _ = window
    .AddMenuPage<MainPage>(Resources.MainPage_Title, Symbol.Home, true)
    .AddFooterPage<SettingsPage>(Resources.SettingsPage_Title, new AnimatedSettingsVisualSource(), true))
.Configure<EventBinding>(events => events.ExceptionThrown += (sender, e) => Debugger.Break())
.Configure<LocalizationService>(localization => localization.ResourceManager = Resources.ResourceManager)
.Configure<TitleBar>(titleBar => titleBar.Title = Resources.Title)

.Configure<SettingsPage>(settings => settings
    .AddComponent<ThemeSelector>()
    .AddComponent<AboutSection>())
.Configure<AboutSection>(section =>
{
    section.AppName = Resources.Title;
    section.Publisher = "Grandiras";
    section.Version = "1.0.0.0";

    section.Links.Add(new("Repository", "https://github.com/Grandiras/RandomAccessMachine"));
})

.Build();