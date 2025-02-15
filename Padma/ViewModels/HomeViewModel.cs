using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Padma.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Padma.Services;
using System.Reactive.Linq;

namespace Padma.ViewModels
{
    public partial class HomeViewModel : ReactiveObject
    {
        #region Private fields

        private readonly DownloadProgressTracker _downloadTracker;
        private readonly AppIdFinder _appIdFinder;
        private readonly CmdRunner _runner;
        private readonly StellarisAutoInstall _stellarisAutoInstall;
        private readonly SaveHistory _history;
        private readonly FolderPicker _folderPicker;
        private readonly ThumbnailLoader _thumbnailLoader;
        private CancellationTokenSource _cts = new();
        private string _workshopId;
        private string _appId;
        public string DownloadedPath;
        private string _buttonContent = "Cancel";
        private string _workshopTitle = "Created by Codecoo";
        private bool _isEnabled = true;
        private bool _cancelEnabled = true;
        private bool _downloadStarted;
        private Bitmap _modsThumbnail;
        private string? _workshopUrl;
        private string _fileSizeInfo;
        private bool _isVisible;
        private string _downloadStatus;
        private int _downloadProgress;
        private ObservableCollection<LiteDbHistory> _historyList = new();

        #endregion

        public event Func<string, Task>? LogAsync;

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

            // Subscribe to progress updates
            _downloadTracker.ProgressUpdated += progress => DownloadProgress = progress;

            this.WhenAnyValue(x => x.DownloadStatusNow)
                .Subscribe(_ => AutoClearDownloadBar());

            this.WhenAnyValue(vm => vm.WorkshopUrl)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                // Only react when the value actually changes
                .DistinctUntilChanged()
                .SelectMany(url => Observable.FromAsync(() => ExtractAppIdAndThumbnail()))
                .Subscribe(
                    _ => { /* Optionally handle success */ },
                    ex => LogAsync?.Invoke($"Error during extraction: {ex.Message}")
                );
            
            InitializeThumbnails();
        }

        private async void InitializeThumbnails()
        {
            await LoadModsThumbnail("https://i.imgur.com/mJMNIz5.png");
        }
        
        private async Task AppIdFinder() 
        {
            await _appIdFinder.ExtractWorkshopId(WorkshopUrl);
            if (string.IsNullOrEmpty(_appIdFinder.WorkshopId)) return; 
            await _appIdFinder.AppFinder();
            WorkshopId = _appIdFinder.WorkshopId;
            AppId = _appIdFinder.AppId;
            WorkshopTitle = _appIdFinder.ModTitle;
        }

        private async Task LoadModsThumbnail(string url)
        {
            await _thumbnailLoader.LoadThumbnail(url);
            ModsThumbnail = _thumbnailLoader.Thumbnail;
        }
        
        private async Task SaveHistory()
        {
            if (!string.IsNullOrEmpty(_workshopUrl) &&
                !string.IsNullOrEmpty(_workshopTitle))
                await _history.SaveHistoryAsync(
                    WorkshopTitle,
                    _workshopUrl,
                    DownloadedPath,
                    _appIdFinder.FileSizeInfo,
                    _appIdFinder.FileSizeBytes); 
        }

        private async Task ExtractAppIdAndThumbnail()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkshopUrl)) return;
                // Fetch App ID properly
                await AppIdFinder(); // This is now awaited correctly
                var bitmap = await _thumbnailLoader.LoadThumbnail(_appIdFinder.ThumbnailUrl);
                ModsThumbnail = bitmap;
                FileSizeInfo = _appIdFinder.FileSizeInfo;
                IsVisible = true;
            }
            catch (Exception ex)
            {
                await LogAsync($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DownloadButton_OnClick()
        {
            try
            {
                _cts.Cancel();
                IsEnabled = false;
                DownloadedPath = Path.Combine(_folderPicker.FolderPathView, AppId, WorkshopId);
                _downloadTracker.DownloadFolder = _folderPicker.SelectedPath;
                _appIdFinder.SetValuesOfProgressTracker();
                DownloadStatusNow = "Downloading"; 
                DownloadStarted = true;
                ButtonContent = "Cancel";
                await _runner.RunSteamCmd(_workshopId, _appId);
            }
            finally
            {
                DownloadStatusNow = _runner.Success ? "Finished" : "Failed";
                if (_history.HistoryEnabled && _runner.Success)
                    await SaveHistory();
                if (AppId is "281990")
                {
                    await LogAsync($"Workshop item {WorkshopId} is a Stellaris mod");
                    await _stellarisAutoInstall.RunStellarisAutoInstallMods(DownloadedPath, WorkshopTitle);
                } 
                await LogAsync("All processes finished.");
                IsEnabled = true;
                ButtonContent = "Open";
            }
        }
        
        [RelayCommand]
        private async Task CancelAndOpen()
        {
            if (DownloadStatusNow is "Finished")
            {
                await _folderPicker.OpenFolder(DownloadedPath);                
            }
            else
            {
                try
                {
                    CancelEnabled = false;
                    _ = _runner.KillSteamCmd();
                }
                finally
                {
                    ButtonContent = "Canceled";
                }
            }
        }
        
        #region Public Properties

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

        // Update the download status in the view model.
        public void AutoClearDownloadBar()
        {
            if (DownloadStatusNow is "Finished" or "Failed")
            {
                _cts.Dispose();
                _cts = new CancellationTokenSource();
                _downloadTracker.Reset();
                Task.Delay(TimeSpan.FromMinutes(1.6), _cts.Token)
                    .ContinueWith(x =>
                    {
                        if (!x.IsCanceled)
                        {
                            DownloadStarted = false;
                        } 
                    });
            }
        }
    }
}
