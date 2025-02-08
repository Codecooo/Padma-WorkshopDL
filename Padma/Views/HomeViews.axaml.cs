using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Padma.Models;
using Padma.Services;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;

namespace Padma.Views;

public partial class HomeViews : UserControl
{
    public HomeViews()
    {
        InitializeComponent();
        AutoScrollLogs();
        _history = App.ServiceProvider.GetRequiredService<SaveHistory>();
        _homeViewModel = App.ServiceProvider.GetRequiredService<HomeViewModel>();
        _runner = App.ServiceProvider.GetRequiredService<CmdRunner>();
        _appIdFinder = App.ServiceProvider.GetRequiredService<AppIdFinder>();
        _findThumbnailLoader = App.ServiceProvider.GetRequiredService<ThumbnailLoader>();
        _runner.LogAsync += UiLogAsync;
        _homeViewModel.LogAsync += UiLogAsync;
        _appIdFinder.LogAsync += UiLogAsync;
        _findThumbnailLoader.LogAsync += UiLogAsync;
        _history.LogAsync += UiLogAsync;
    }

    private void HideConsole_Hovered(object? sender, PointerEventArgs e)
        => HideConsoleHover.IsVisible = true;
    
    private void HideConsole_NotHovered(object? sender, PointerEventArgs e)
       => HideConsoleHover.IsVisible = false;
    
    private void ChangeLabelsBasedOnCheck()
    {
        if (HideOrShowConsole.IsChecked ?? true)
            HideConsoleHover.Text = "Show Logs";
        else
            HideConsoleHover.Text = "Hide Logs";
    }

    private async Task UiLogAsync(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { LogOutput.Text += message + Environment.NewLine; });
    }

    private void AutoScrollLogs()
    {
        var logoutput = this.FindControl<TextBox>("LogOutput");
        logoutput.GetObservable<string>(TextBlock.TextProperty);
        logoutput.GetObservable<string>(TextBox.TextProperty)
            .Subscribe(_ =>
            {
                // Set the caret index to the end of the text
                logoutput.CaretIndex = logoutput.Text.Length;
            });
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            // Hide the ConsoleLogWindow when checked, show when unchecked
            ConsoleLogWindow.IsVisible = !(toggleButton.IsChecked ?? false);
            ChangeLabelsBasedOnCheck();
        }
    }
    


    #region Private fields

    private readonly AppIdFinder _appIdFinder;
    private readonly CmdRunner _runner;
    private readonly HomeViewModel _homeViewModel;
    private readonly ThumbnailLoader _findThumbnailLoader;
    private readonly SaveHistory _history;

    #endregion

}