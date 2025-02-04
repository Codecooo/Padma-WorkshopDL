using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Padma.Models;
using Padma.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.Views;

public partial class HomeViews : UserControl
{
    public HomeViews()
    {
        InitializeComponent();
        AutoScrollLogs();
        _history = App.ServiceProvider.GetRequiredService<SaveHistory>();
        _homeViewModel = App.ServiceProvider.GetRequiredService<HomeViewModel>();
        _runner = new CmdRunner();
        _findThumbnailLoader = new ThumbnailLoader();
        _appIdFinder = new AppIdFinder();
        _runner.LogAsync += UILogAsync;
        _appIdFinder.LogAsync += UILogAsync;
        _findThumbnailLoader.LogAsync += UILogAsync;
        _history.LogAsync += UILogAsync;
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

    private async Task UILogAsync(string message)
    {
        // Use Dispatcher to update UI from background thread
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

    private string ExtractWorkshopId(string workshopUrl)
    {
        // Match a numeric ID at the end of the URL path
        var regex = new Regex(@"id=(\d+)");
        var match = regex.Match(workshopUrl);

        if (match.Success)
        {
            WorkshopId = match.Groups[1].Value; // Assign first
            return WorkshopId;
        }

        return null; // ID not found
    }

    private async Task AppIdFinder()
    {
        await _appIdFinder.AppFinder(WorkshopId);
        appId = _appIdFinder.AppId;
        _workshopTitle = _appIdFinder.ModTitle;
        _thumbnailUrl = _appIdFinder.ThumbnailUrl;
    }

    private async Task SaveHistory()
    {
        if (!string.IsNullOrEmpty(WorkshopUrl.Text) &&
            !string.IsNullOrEmpty(_workshopTitle))
            await _history.SaveHistoryAsync(
                _workshopTitle,
                WorkshopUrl.Text,
                "[DefaultDownloadPath]",
                _appIdFinder.FileSizeInfo); // Replace with actual path
    }

    private async void ExtractAppIdandWorkshopId(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(WorkshopUrl.Text)) return;

        // Extract Workshop ID
        WorkshopId = ExtractWorkshopId(WorkshopUrl.Text);

        if (string.IsNullOrEmpty(WorkshopId))
        {
            await UILogAsync("Invalid Workshop ID.");
            return;
        }

        await UILogAsync($"Extracted Workshop ID: {WorkshopId}");

        try
        {
            // Fetch App ID properly
            await AppIdFinder(); // This is now awaited correctly

            var bitmap = await _findThumbnailLoader.LoadThumbnail(_thumbnailUrl);
            // Update ViewModel properties on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                WorkshopTitle.Content = _workshopTitle;
                Thumbnail.Source = bitmap;
                ModId.Text = WorkshopId;
                Appid.Text = appId;
                FileSizeInfo.IsVisible = true;
                FileSizeInfo.Text = _appIdFinder.FileSizeInfo;
            });
        }
        catch (Exception ex)
        {
            await UILogAsync($"Error: {ex.Message}");
        }
    }

    private async void DownloadButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button downloadButton)
        {
            downloadButton.IsEnabled = false;
            try
            {
                _homeViewModel.AppId = _appIdFinder.AppId;
                _homeViewModel.WWorkshopId = WorkshopId;
                _homeViewModel.TotalSize = _appIdFinder.FileSizeBytes;
                if (_history.HistoryEnabled)
                    await SaveHistory();
                _history.DownloadStatusChange = "Downloading";
                await _runner.RunSteamCmd(WorkshopId, appId);
            }
            finally
            {
                _history.DownloadStatusChange = _runner.Success ? "Finished" : "Failed";
                downloadButton.IsEnabled = true;
                await UILogAsync("All processes finished.");
            }
        }
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        var toggleButton = sender as ToggleButton;
        if (toggleButton != null)
        {
            // Hide the ConsoleLogWindow when checked, show when unchecked
            ConsoleLogWindow.IsVisible = !(toggleButton.IsChecked ?? false);
            ChangeLabelsBasedOnCheck();
        }
    }
    
    private void CancelDownloadOn(object? sender, RoutedEventArgs e)
    {
        var downloadButton = this.FindControl<Button>("ConfirmButton");
        try
        {
            if (downloadButton is not null) 
                downloadButton.IsEnabled = false;
            _ = _runner.KillSteamCmd();
        }
        finally
        {
            if (downloadButton is not null)
            {
                downloadButton.Content = "Canceled";
                downloadButton.IsEnabled = true;
            }
        }
    }


    #region Private fields

    private readonly AppIdFinder _appIdFinder;
    private readonly CmdRunner _runner;
    private readonly ThumbnailLoader _findThumbnailLoader;
    private readonly SaveHistory _history;
    private readonly HomeViewModel _homeViewModel;
    private string WorkshopId;
    private string appId;
    private string _workshopTitle;
    private string _thumbnailUrl;

    #endregion

}