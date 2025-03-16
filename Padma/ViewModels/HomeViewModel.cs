using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using Padma.Services;
using ReactiveUI;

namespace Padma.ViewModels;

public partial class HomeViewModel : ReactiveObject
{
    public HomeViewModel(
        SaveHistory history,
        AppIdFinder appIdFinder,
        CmdRunner runner,
        ThumbnailLoader thumbnailLoader,
        DownloadProgressTracker downloadTracker,
        FolderPicker folderPicker,
        StellarisAutoInstall stellarisAutoInstall,
        SupportedGames supportedGames,
        DownloadProcessor downloadProcessor)
    {
        _history = history;
        _folderPicker = folderPicker;
        _appIdFinder = appIdFinder;
        _runner = runner;
        _thumbnailLoader = thumbnailLoader;
        _downloadTracker = downloadTracker;
        _stellarisAutoInstall = stellarisAutoInstall;
        _supportedGames = supportedGames;
        _downloadProcessor = downloadProcessor;

        // Subscribe to progress updates in DownloadProgressTracker.
        _downloadTracker.ProgressUpdated += progress => DownloadProgress = progress;
        

        // Auto-clear download bar when download finish or failed.
        this.WhenAnyValue(x => x.DownloadStatusNow)
            .Subscribe(_ => AutoClearDownloadBar());

        // When the WorkshopUrl changes, extract the AppId and load thumbnail.
        this.WhenAnyValue(vm => vm.WorkshopUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .DistinctUntilChanged()
            .SelectMany(_ => Observable.FromAsync(() => ExtractSingleDownloadModInfo()))
            .Subscribe(
                _ =>
                {
                    // Intentionally empty
                },
                ex => LogAsync?.Invoke($"Error during extraction: {ex.Message}")
            );
        
        this.WhenAnyValue(vm => vm.MultipleWorkshopUrls)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .DistinctUntilChanged()
            .SelectMany(_ => Observable.FromAsync(() => ExtractMultipleModsInfo()))
            .Subscribe(
                _ =>
                {
                    // Intentionally empty
                },
                ex => LogAsync?.Invoke($"Error during extraction: {ex.Message}")
            );

        SetupEventHandlers();

        // Load a default thumbnail at startup.
        InitializeThumbnailsAsync();
    }

    public event Func<string, Task>? LogAsync;

    private async void InitializeThumbnailsAsync()
    {
        await LoadModsThumbnailAsync("https://i.imgur.com/mi85vxR.png");
    }

    /// <summary>
    ///     Setting up the event handlers for each classes/events and then reoport to the
    ///     UiLogsMessage method
    /// </summary>
    private void SetupEventHandlers()
    {
        _runner.LogAsync += UiLogsMessage;
        LogAsync += UiLogsMessage;
        _appIdFinder.LogAsync += UiLogsMessage;
        _thumbnailLoader.LogAsync += UiLogsMessage;
        _history.LogAsync += UiLogsMessage;
        _stellarisAutoInstall.LogAsync += UiLogsMessage;
        _downloadTracker.LogAsync += UiLogsMessage;
        _supportedGames.LogAsync += UiLogsMessage;
        _downloadProcessor.LogAsync += UiLogsMessage;
    }

    /// <summary>
    ///     Just for updating the UI for Console Log Window
    /// </summary>
    /// <param name="message"></param>
    private async Task UiLogsMessage(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Get the current log text from the property.
            var currentLog = LogsMessage;

            // If this is the first log appended, add a newline after the welcome message.
            if (currentLog == "Welcome to Padma version 1.1.1") currentLog += Environment.NewLine;

            // Append the new message with a newline.
            currentLog += message + Environment.NewLine;

            // Update the property to trigger UI notifications.
            LogsMessage = currentLog;
        });
    }

    /// <summary>
    ///     Extracts the app ID and loads the thumbnail.
    /// </summary>
    private async Task ExtractSingleDownloadModInfo()
    {
         IsEnabled = false;
         if (string.IsNullOrWhiteSpace(WorkshopUrl))
             return; 
         
         // Extract Workshop ID from the URL.
         var item = await _downloadProcessor.ExtractWorkshopInfo(WorkshopUrl);
         _downloadItem = item;
         
         // Update UI-bound properties.
         WorkshopId = _appIdFinder.WorkshopId;
         AppId = _appIdFinder.AppId;
         WorkshopTitle = _appIdFinder.ModTitle;
         var thumbnail = await _thumbnailLoader.LoadThumbnail(_appIdFinder.ThumbnailUrl);
         ModsThumbnail = thumbnail;
         
         FileSizeInfo = _appIdFinder.FileSizeInfo;
         IsVisible = true;
         IsEnabled = true;
    }

    private async Task LoadModsThumbnailAsync(string url)
    {
        var bitmap = await _thumbnailLoader.LoadThumbnail(url);
        ModsThumbnail = bitmap;
    }

    /// <summary>
    ///     Once the user click download this method will execute much of what this app mainly does
    ///     So it just call the CmdRunner method with the corresponding ModID or AppID
    /// </summary>
    [RelayCommand]
    private async Task DownloadButton_OnClickAsync()
    {
        if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_workshopId) || DownloadStarted)
            return;
        try
        {
            // Cancel any previous pending delay tasks.
            _cts.Cancel();
            IsEnabled = false;

            DownloadStatusNow = "Downloading";
            DownloadStarted = true;
            ButtonContent = "Cancel";

            await LogAsync("Starting download process...");

            // Run the download process.
            await _downloadProcessor.ProcessDownload(_downloadItem, StellarisAutoInstallEnabled, _history.HistoryEnabled);
        }
        finally
        {
            // Update status based on success.
            DownloadStatusNow = _runner.Success ? "Finished" : "Failed";
            IsEnabled = true;

            await LogAsync("All processes finished.");
            if (DownloadStatusNow is "Finished") ButtonContent = "Open";
        }
    }

    private async Task ExtractMultipleModsInfo()
    {
        if (string.IsNullOrWhiteSpace(MultipleWorkshopUrls)) return;
        
        var item = await _downloadProcessor.ExtractWorkshopInfo(MultipleWorkshopUrls);
        _downloadItem = item;

        IsVisible = true;
        MultipleFileSizeInfo = _downloadProcessor.GetTotalDownloadSize();
        
        var originalWorkshopTitle = _appIdFinder.ModTitle;

        // show the processed titles 
        ProcessedWorkshopTitles += originalWorkshopTitle + Environment.NewLine;

        MultipleWorkshopUrls = string.Empty;
    }

    [RelayCommand]
    public async Task MultiipleDownloadsAsync()
    {
        if (DownloadStarted)
        {
            await _downloadProcessor.CancelMultipleDownload();
            return;
        }
        
        if (string.IsNullOrEmpty(_downloadItem.AppId) || string.IsNullOrEmpty(_downloadItem.WorkshopId))
            return;
        WorkshopTitle = _appIdFinder.ModTitle;
        
        
        DownloadStatusNow = "Downloading";
        DownloadStarted = true;
        MultipleDownButtonContent = "Cancel";
        ButtonContent = "Cancel";

        await LogAsync("Starting download process...");
        
        await _downloadProcessor.ProcessQueuedDownloads(StellarisAutoInstallEnabled, _history.HistoryEnabled);
        
        // Update status based on success.
        DownloadStatusNow = _runner.Success ? "Finished" : "Failed";
        IsEnabled = true;

        await LogAsync("All processes finished.");
        if (DownloadStatusNow is "Finished") ButtonContent = "Open";
    }

    /// <summary>
    ///     Method to cancel the download process by killing steamcmd process
    ///     if the download is finished it will change to open the mod download folder
    /// </summary>
    [RelayCommand]
    private async Task CancelAndOpenAsync()
    {
        switch (DownloadStatusNow)
        {
            case "Finished":
            {
                await _folderPicker.OpenFolder(_downloadItem.DestinationFolder);
                break;
            }
            case "Failed":
            {
                ButtonContent = "Failed";
                break;
            }
            default:
            {
                CancelEnabled = false;
                _ = _runner.KillSteamCmd();
                CancelEnabled = true;
                ButtonContent = "Canceled";
                break;
            }
        }
    }

    [RelayCommand]
    public void HideLogsOnClick()
    {
        ConsoleLogsVisible = !ConsoleLogsVisible;
        HideLogsHoverMessage = ConsoleLogsVisible switch
        {
            true => "Hide Logs",
            false => "Show Logs"
        };

        HideLogsIcon = ConsoleLogsVisible switch
        {
            true => "/Assets/console-64.png",
            false => "/Assets/console-64-crossed.png"
        };
    }

    /// <summary>
    ///     Clears the download bar after a delay if download is finished or failed.
    ///     Uses a local cancellation token to manage delay tasks.
    /// </summary>
    public void AutoClearDownloadBar()
    {
        if (DownloadStatusNow is "Finished" or "Failed")
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            _downloadTracker.Reset();

            // Local variable for the delay task.
            var delayTask = Task.Delay(TimeSpan.FromMinutes(1.6), _cts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled) DownloadStarted = false;
                });
        }
    }

    #region Dependencies and Persistent State

    // Injected dependencies
    private readonly DownloadProgressTracker _downloadTracker;
    private readonly AppIdFinder _appIdFinder;
    private readonly CmdRunner _runner;
    private readonly StellarisAutoInstall _stellarisAutoInstall;
    private readonly SaveHistory _history;
    private readonly FolderPicker _folderPicker;
    private readonly ThumbnailLoader _thumbnailLoader;
    private readonly SupportedGames _supportedGames;
    private readonly DownloadProcessor _downloadProcessor;
    private DownloadItem _downloadItem;
    private DownloadResult _downloadResult;

    // Cancellation token shared for UI tasks.
    private CancellationTokenSource _cts = new();

    // Backing fields for properties.
    private string _workshopId;
    private string _logsMessage = "Welcome to Padma version 1.1";
    private string _appId;
    private string _buttonContent = "Cancel";
    private string _workshopTitle = "Created by Codecoo";
    private bool _isEnabled = true;
    private bool _cancelEnabled = true;
    private bool _downloadStarted;
    private Bitmap? _modsThumbnail;
    private string? _workshopUrl;
    private string _multipleWorkshopUrls;
    private string _fileSizeInfo;
    private string _multipleFileSizeInfo;
    private string _processedWorkshopTitles;
    private string _multipleDownButtonContent;
    private bool _isVisible;
    private string _hideLogsHoverMessage = "Hide Logs";
    private bool _consoleLogsVisible = true;
    private string _hideLogsIcon = "/Assets/console-64.png";
    private string _downloadStatus;
    private int _downloadProgress;
    private ObservableCollection<LiteDbHistory> _historyList = new();

    // Public state for UI binding.
    public string DownloadedPath;
    public bool StellarisAutoInstallEnabled = true;

    #endregion

    #region ReactiveUI Public Properties

    public int DownloadProgress
    {
        get => _downloadProgress;
        set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
    }

    public bool CancelEnabled
    {
        get => _cancelEnabled;
        set => this.RaiseAndSetIfChanged(ref _cancelEnabled, value);
    }

    public bool DownloadStarted
    {
        get => _downloadStarted;
        set => this.RaiseAndSetIfChanged(ref _downloadStarted, value);
    }

    public string LogsMessage
    {
        get => _logsMessage;
        set => this.RaiseAndSetIfChanged(ref _logsMessage, value);
    }

    public string MultipleWorkshopUrls
    {
        get => _multipleWorkshopUrls;
        set => this.RaiseAndSetIfChanged(ref _multipleWorkshopUrls, value);
    }

    public string MultipleFileSizeInfo
    {
        get => _multipleFileSizeInfo;
        set => this.RaiseAndSetIfChanged(ref _multipleFileSizeInfo, value);
    }

    public string MultipleDownButtonContent
    {
        get => _multipleDownButtonContent;
        set => this.RaiseAndSetIfChanged(ref _multipleDownButtonContent, value);
    }

    public string FileSizeInfo
    {
        get => _fileSizeInfo;
        set => this.RaiseAndSetIfChanged(ref _fileSizeInfo, value);
    }

    public string ProcessedWorkshopTitles
    {
        get => _processedWorkshopTitles;
        set => this.RaiseAndSetIfChanged(ref _processedWorkshopTitles, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public string HideLogsIcon
    {
        get => _hideLogsIcon;
        set => this.RaiseAndSetIfChanged(ref _hideLogsIcon, value);
    }

    public string HideLogsHoverMessage
    {
        get => _hideLogsHoverMessage;
        set => this.RaiseAndSetIfChanged(ref _hideLogsHoverMessage, value);
    }

    public bool ConsoleLogsVisible
    {
        get => _consoleLogsVisible;
        set => this.RaiseAndSetIfChanged(ref _consoleLogsVisible, value);
    }

    public string WorkshopUrl
    {
        get => _workshopUrl;
        set => this.RaiseAndSetIfChanged(ref _workshopUrl, value);
    }

    public string WorkshopId
    {
        get => _workshopId;
        set => this.RaiseAndSetIfChanged(ref _workshopId, value);
    }

    public string AppId
    {
        get => _appId;
        set => this.RaiseAndSetIfChanged(ref _appId, value);
    }

    public string WorkshopTitle
    {
        get => _workshopTitle;
        set => this.RaiseAndSetIfChanged(ref _workshopTitle, value);
    }

    public string ButtonContent
    {
        get => _buttonContent;
        set => this.RaiseAndSetIfChanged(ref _buttonContent, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public Bitmap? ModsThumbnail
    {
        get => _modsThumbnail;
        set => this.RaiseAndSetIfChanged(ref _modsThumbnail, value);
    }

    public string DownloadStatusNow
    {
        get => _downloadStatus;
        set => this.RaiseAndSetIfChanged(ref _downloadStatus, value);
    }

    public ObservableCollection<LiteDbHistory> HistoryList
    {
        get => _historyList;
        set => this.RaiseAndSetIfChanged(ref _historyList, value);
    }

    #endregion
}