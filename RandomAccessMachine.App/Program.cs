﻿using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using RandomAccessMachine.App.Components.Settings;
using RandomAccessMachine.App.Pages;
using RandomAccessMachine.Backend.Interpreter;
using System.Diagnostics;
using WinSharp;
using WinSharp.Pages;
using WinSharp.Pages.Components.Settings;
using WinSharp.Services;
using WinSharp.Windows;

namespace RandomAccessMachine.App;
public static class Program
{
    [STAThread]
    public static void Main(string[] args) => new AppBuilder()

        .AddNavigationWindow(window =>
        {
            window.Height = 1080;
            window.Width = 1920;

            _ = window
            .AddMenuPage<MainPage>(Resources.MainPage_Title, "\uE943", true)
            .AddFooterPage<SettingsPage>(Resources.SettingsPage_Title, new AnimatedSettingsVisualSource(), true);
        })
        .Configure<EventBinding>(events => events.ExceptionThrown += (sender, e) => Debugger.Break())
        .Configure<LocalizationService>(localization => localization.ResourceManager = Resources.ResourceManager)
        .Configure<TitleBar>(titleBar => titleBar.Title = Resources.Title)

        .Configure<SettingsPage>(settings => settings
            .AddComponent<ThemeSelector>()
            .AddComponent<StartupComponent>()
            .AddComponent<AutoSaveComponent>()
            .AddComponent<AboutSection>())
        .Configure<AboutSection>(section =>
        {
            section.AppName = Resources.Title;
            section.Publisher = "Grandiras";
            section.Version = "1.0.0.0";

            section.Links.Add(new("Repository", "https://github.com/Grandiras/RandomAccessMachine"));
        })

        .AddSingleton<Interpreter>()

        .AddTransient<AutoSaveComponent>()
        .AddTransient<StartupComponent>()

        .Build();
}