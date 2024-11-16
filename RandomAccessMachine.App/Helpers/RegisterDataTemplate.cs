using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using RandomAccessMachine.Backend.Specification;
using WinSharp.Styles;

namespace RandomAccessMachine.App.Helpers;
public static class RegisterDataTemplate
{
    public static DataTemplate<Register> Template = (DataTemplate)XamlReader.LoadWithInitialTemplateValidation("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" x:Key="RegistersDataTemplate">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}" Width="48" Margin="0 0 8 0" />
                <AppBarSeparator />
                <TextBlock Text="{Binding Value}" Margin="8 0 0 0" />
            </StackPanel>
        </DataTemplate>
        """);
}
