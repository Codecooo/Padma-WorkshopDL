using System.Linq;
using ReactiveUI;
using Padma.Models;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Padma.ViewModels;

public class HomeViewModel : ReactiveObject
{
    private readonly SaveHistory _history;
    private string _downloadStatus;
    public long TotalSize;
    private int _downloadProgress;
    private Timer _timer;
    public string AppId;
    public string WWorkshopId;
    private FileSystemWatcher? _watcher;
    private long _currentSize = 0;
    private ObservableCollection<LiteDbHistory> _historyList = new();

    public HomeViewModel(SaveHistory history)
    {
        _history = history;
        StartTrackingDownload(AppId, WWorkshopId);
        _history.HistoryChangedSignal
            .Subscribe (_  => LoadRecentHistory());
        this.WhenAnyValue(h => h._history.DownloadStatusChange)
            .Subscribe(_ => AutoClearDownloadBar());
        // _timer = new Timer(state =>
        // {
        //     TrackDownloadProgress(AppId, WWorkshopId);
        // }, null, 0, 1000);
    }
    
    public void StartTrackingDownload(string appId, string workshopId)
    {
        string path = $"/home/lagita/.local/share/Steam/steamapps/workshop/downloads/{appId}/{workshopId}";

        if (!Directory.Exists(path)) return;

        // Initialize current size at the start
        _currentSize = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);

        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
        };

        _watcher.Changed += UpdateDownloadProgress;
        _watcher.Created += UpdateDownloadProgress;
    }

    private void UpdateDownloadProgress(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Update only the changed file instead of rescanning all
            long newSize = new FileInfo(e.FullPath).Length;
            _currentSize += newSize;  

            int downloadPercentage = (int)(Math.Round(_currentSize / (float)TotalSize) * 100);
            DownloadProgress = downloadPercentage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tracking download: {ex.Message}");
        }
    }

    public int DownloadProgress
    {
        get => _downloadProgress;
        set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
    }

    // public void TrackDownloadProgress(string appId, string workshopId)
    // {
    //     string path = $"/home/lagita/.local/share/Steam/steamapps/workshop/downloads/{appId}/{workshopId}";
    //     if (Directory.Exists(path))
    //     {
    //         long currentSize = 0; 
    //         foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
    //         {
    //             currentSize += new FileInfo(file).Length;
    //         }  
    //         Console.WriteLine($"Current size: {currentSize}");
    //         int downloadPercentage = (int)(Math.Round(currentSize / (float) TotalSize) * 100);
    //         DownloadProgress = downloadPercentage;
    //     }
    // }
    
    public string DownloadStatusNow
    {
        get => _downloadStatus;
        set => this.RaiseAndSetIfChanged(ref _downloadStatus, value);
    }
    
    private void LoadRecentHistory()
    {
        var recenthistory = _history.GetRecentHistoryList().ToList();
        HistoryList = new ObservableCollection<LiteDbHistory>(recenthistory);
    }
    
    public ObservableCollection<LiteDbHistory> HistoryList
    {
        get => _historyList;
        set => this.RaiseAndSetIfChanged(ref _historyList, value);
    }

    public void AutoClearDownloadBar()
    {
        DownloadStatusNow = _history.DownloadStatusChange;
        if (_history.DownloadStatusChange == "Finished")
            Task.Delay(TimeSpan.FromMinutes(1.6)).ContinueWith(_ => HistoryList.Clear());
    }
}