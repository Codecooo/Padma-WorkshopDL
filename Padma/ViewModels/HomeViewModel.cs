using System;
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
        StellarisAutoInstall stellarisAutoInstall)
    {
        _history = history;
        _folderPicker = folderPicker;
        _appIdFinder = appIdFinder;
        _runner = runner;
        _thumbnailLoader = thumbnailLoader;
        _downloadTracker = downloadTracker;
        _stellarisAutoInstall = stellarisAutoInstall;

        // Subscribe to progress updates.
        _downloadTracker.ProgressUpdated += progress => DownloadProgress = progress;

        // Auto-clear download bar when status changes.
        this.WhenAnyValue(x => x.DownloadStatusNow)
            .Subscribe(_ => AutoClearDownloadBar());

        // When the WorkshopUrl changes, extract the AppId and load thumbnail.
        this.WhenAnyValue(vm => vm.WorkshopUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .DistinctUntilChanged()
            .SelectMany(_ => Observable.FromAsync(() => ExtractAppIdAndThumbnailAsync()))
            .Subscribe(
                _ =>
                {
                    // Intentionally empty
                },
                ex => LogAsync?.Invoke($"Error during extraction: {ex.Message}")
            );

        // _downloadTracker.ProgressUpdated += progress =>
        // {
        //     DownloadProgress = progress;
        //
        //     // Update the status message depending on progress
        //     if (DownloadStarted && DownloadStatusNow == "Downloading")
        //     {
        //         if (progress == 100)
        //         {
        //             // Don't update here - the CommandButton_OnClickAsync handler will update status
        //         }
        //         else if (progress > 0 && progress < 100)
        //         {
        //             // UI will show progress bar movement, no need to change status text
        //         }
        //
        //
        //
        //         }
        //     };
        
        SetupEventHandlers();

        // Load a default thumbnail at startup.
        InitializeThumbnailsAsync();
    }

    public event Func<string, Task>? LogAsync;

    private async void InitializeThumbnailsAsync()
    {
        await LoadModsThumbnailAsync("https://i.imgur.com/mi85vxR.png");
    }

    private void SetupEventHandlers()
    {
        _runner.LogAsync += UiLogsMessage;
        LogAsync += UiLogsMessage;
        _appIdFinder.LogAsync += UiLogsMessage;
        _thumbnailLoader.LogAsync += UiLogsMessage;
        _history.LogAsync += UiLogsMessage;
        _stellarisAutoInstall.LogAsync += UiLogsMessage;
    }
    
    private async Task UiLogsMessage(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Get the current log text from the property.
            var currentLog = LogsMessage;
         
            // If this is the first log appended, add a newline after the welcome message.
            if (currentLog == "Welcome to Padma version 1.0")
            {
                currentLog += Environment.NewLine;
            }
         
            // Append the new message with a newline.
            currentLog += message + Environment.NewLine;
         
            // Update the property to trigger UI notifications.
            LogsMessage = currentLog;
        });
    }
    
    /// <summary>
    ///     Extracts the app ID and loads the thumbnail.
    /// </summary>
    private async Task ExtractAppIdAndThumbnailAsync()
    {
        try
        {
            IsEnabled = false;
            if (string.IsNullOrWhiteSpace(WorkshopUrl))
                return;

            // Extract Workshop ID from the URL.
            await _appIdFinder.ExtractWorkshopId(WorkshopUrl);
            if (string.IsNullOrEmpty(_appIdFinder.WorkshopId))
                return;

            // Get App details.
            await _appIdFinder.AppFinder();
            // Update UI-bound properties.
            WorkshopId = _appIdFinder.WorkshopId;
            AppId = _appIdFinder.AppId;
            WorkshopTitle = _appIdFinder.ModTitle;

            var thumbnail = await _thumbnailLoader.LoadThumbnail(_appIdFinder.ThumbnailUrl);
            ModsThumbnail = thumbnail;
            FileSizeInfo = _appIdFinder.FileSizeInfo;
            IsVisible = true;
        }
        catch (Exception ex)
        {
            if (LogAsync != null)
                await LogAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async Task LoadModsThumbnailAsync(string url)
    {
        var bitmap = await _thumbnailLoader.LoadThumbnail(url);
        ModsThumbnail = bitmap;
    }

    private async Task SaveHistoryAsync()
    {
        if (!string.IsNullOrEmpty(_workshopUrl) && !string.IsNullOrEmpty(_workshopTitle))
        {
            await _history.SaveHistoryAsync(
                WorkshopTitle,
                _workshopUrl,
                DownloadedPath,
                _appIdFinder.FileSizeInfo,
                _appIdFinder.FileSizeBytes);
        }
    }

    [RelayCommand]
    private async Task DownloadButton_OnClickAsync()
    {
        try
        {
            // Cancel any previous pending delay tasks.
            _cts.Cancel();
            IsEnabled = false;
            DownloadedPath = Path.Combine(_folderPicker.FolderPathView, AppId, WorkshopId);
            _downloadTracker.DownloadFolder = _folderPicker.SelectedPath;
            _appIdFinder.SetValuesOfProgressTracker();

            DownloadStatusNow = "Downloading";
            DownloadStarted = true;
            ButtonContent = "Cancel";

            // Add log message to indicate start of process
            await LogAsync?.Invoke("Starting download process...");

            // Run the download process.
            await _runner.RunSteamCmd(_workshopId, _appId);
        }
        finally
        {
            // Update status based on success.
            DownloadStatusNow = _runner.Success ? "Finished" : "Failed";
            IsEnabled = true;

            // If it is a Stellaris mod and auto-install is enabled, trigger auto-install.
            if (AppId is "281990" && DownloadStatusNow is "Finished" && StellarisAutoInstallEnabled)
            {
                await LogAsync?.Invoke($"Workshop item {WorkshopId} is a Stellaris mod");
                await _stellarisAutoInstall.RunStellarisAutoInstallMods(DownloadedPath, WorkshopTitle);
                DownloadedPath = $"\"{_stellarisAutoInstall.StellarisDocPath}\"";
            }

            if (_history.HistoryEnabled && _runner.Success)
            {
                await SaveHistoryAsync();
            }

            await LogAsync?.Invoke("All processes finished.");
            if (DownloadStatusNow is "Finished")
            {
                ButtonContent = "Open";
            }
        }
    }

    [RelayCommand]
    private async Task CancelAndOpenAsync()
    {
        switch (DownloadStatusNow)
        {
            case "Finished":
            {
                await _folderPicker.OpenFolder(DownloadedPath);
                break;
            }
            case "Failed":
            {
                ButtonContent = "Failed";
                break;
            }
            default:
            {
                try
                {
                    CancelEnabled = false;
                    _ = _runner.KillSteamCmd();
                }
                finally
                {
                    CancelEnabled = true;
                    ButtonContent = "Canceled";
                }

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

    // Cancellation token shared for UI tasks.
    private CancellationTokenSource _cts = new();

    // Backing fields for properties.
    private string _workshopId;
    private string _logsMessage = "Welcome to Padma version 1.0";
    private string _appId;
    private string _buttonContent = "Cancel";
    private string _workshopTitle = "Created by Codecoo";
    private bool _isEnabled = true;
    private bool _cancelEnabled = true;
    private bool _downloadStarted;
    private Bitmap _modsThumbnail;
    private string? _workshopUrl;
    private string _fileSizeInfo;
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

    public string FileSizeInfo
    {
        get => _fileSizeInfo;
        set => this.RaiseAndSetIfChanged(ref _fileSizeInfo, value);
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

    public Bitmap ModsThumbnail
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