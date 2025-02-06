using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Padma.Models;
using System.Collections.ObjectModel;
using System.Threading;

namespace Padma.ViewModels
{
    public class HomeViewModel : ReactiveObject
    {
        private readonly SaveHistory _history;
        private readonly CancellationTokenSource _cts = new();
        private string _downloadStatus;
        private int _downloadProgress;
        private ObservableCollection<LiteDbHistory> _historyList = new();
        
        public HomeViewModel(SaveHistory history)
        {
            _history = history;
            _history.HistoryChangedSignal
                .Subscribe(_ => LoadRecentHistory());

            // Subscribe to changes on DownloadStatusChange from the model.
            _history.WhenAnyValue(h => h.DownloadStatusChange)
                .Subscribe(_ => AutoClearDownloadBar());
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
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
