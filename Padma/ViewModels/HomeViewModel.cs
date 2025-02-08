using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Padma.Models;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Padma.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.ViewModels
{
    public partial class HomeViewModel : ReactiveObject
    {
        #region Private fields
        
        private readonly AppIdFinder _appIdFinder;
        private readonly CmdRunner _runner;
        private readonly SaveHistory _history;
        private readonly ThumbnailLoader _thumbnailLoader;
        private readonly CancellationTokenSource _cts = new();
        private string _workshopId;
        private string _appId;
        private string _buttonContent = "Cancel";
        private bool _isEnabled = true;
        private Bitmap _modsThumbnail;
        private string? _workshopUrl;
        private string _workshopTitle;
        private string _thumbnailUrl;
        private string _downloadStatus;
        private int _downloadProgress;
        private ObservableCollection<LiteDbHistory> _historyList = new();
        
        #endregion
        
        public event Func<string, Task>? LogAsync;
        public HomeViewModel()
        {
            _history = App.ServiceProvider.GetRequiredService<SaveHistory>();
            _appIdFinder = App.ServiceProvider.GetRequiredService<AppIdFinder>();
            _runner = App.ServiceProvider.GetRequiredService<CmdRunner>();
            _thumbnailLoader = App.ServiceProvider.GetRequiredService<ThumbnailLoader>();
            _history.HistoryChangedSignal
                .Subscribe(_ => LoadRecentHistory());

            // Subscribe to changes on DownloadStatusChange from the model.
            _history.WhenAnyValue(h => h.DownloadStatusChange)
                .Subscribe(_ => AutoClearDownloadBar());

            this.WhenAnyValue(vm => vm._workshopUrl)
                .Subscribe(async void (_) => await ExtractAppIdAndThumbnail());
        } 
        
        private async Task AppIdFinder() 
        {
            await _appIdFinder.ExtractWorkshopId(_workshopUrl);
            await _appIdFinder.AppFinder();
            _appId = _appIdFinder.AppId;
            _workshopTitle = _appIdFinder.ModTitle;
            _thumbnailUrl = _appIdFinder.ThumbnailUrl;
        }

        private async Task SaveHistory()
        {
            if (!string.IsNullOrEmpty(_workshopUrl) &&
                !string.IsNullOrEmpty(_workshopTitle))
                await _history.SaveHistoryAsync(
                    _workshopTitle,
                    _workshopUrl,
                    _runner.DownloadPath,
                    _appIdFinder.FileSizeInfo); // Replace with actual path
        }

        [RelayCommand]
        private async Task ExtractAppIdAndThumbnail()
        {
            try
            {
                if (string.IsNullOrEmpty(_workshopUrl)) return;
                // Fetch App ID properly
                await AppIdFinder(); // This is now awaited correctly
                AppId = _appIdFinder.AppId;
                WorkshopId = _appIdFinder.WorkshopId;
                var bitmap = await _thumbnailLoader.LoadThumbnail(_thumbnailUrl);
                ModsThumbnail = bitmap;
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
                IsEnabled = false;
                if (_history.HistoryEnabled)
                    await SaveHistory();
                _appIdFinder.SetValuesOfProgressTracker();
                _history.DownloadStatusChange = "Downloading";
                _runner.DownloadPath = "/home/lagita/Downloads";
                await _runner.RunSteamCmd(_workshopId, _appId);
            }
            finally
            {
                _history.DownloadStatusChange = _runner.Success ? "Finished" : "Failed";
                await LogAsync("All processes finished.");
                IsEnabled = true;
            }
        }
        
        [RelayCommand]
        private void CancelDownloadOn()
        {
            try
            {
                IsEnabled = false;
                _ = _runner.KillSteamCmd();
            }
            finally
            {
                ButtonContent = "Canceled";
                IsEnabled = true;
            }
        }
        
        #region Public Properties

        public int DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
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
        
        #endregion

        public ObservableCollection<LiteDbHistory> HistoryList
        {
            get => _historyList;
            set => this.RaiseAndSetIfChanged(ref _historyList, value);
        }
        
        // Reload recent history from the model.
        private void LoadRecentHistory()
        {
            var recenthistory = _history.GetRecentHistoryList().ToList();
            HistoryList = new ObservableCollection<LiteDbHistory>(recenthistory);
            _cts.Cancel();
        }

        // Update the download status in the view model.
        public void AutoClearDownloadBar()
        {
            DownloadStatusNow = _history.DownloadStatusChange;
            if (DownloadStatusNow is "Finished" or "Failed")
            {
                Task.Delay(TimeSpan.FromMinutes(1.6), _cts.Token)
                    .ContinueWith(_ => HistoryList.Clear());
            }
        }
    }
}
