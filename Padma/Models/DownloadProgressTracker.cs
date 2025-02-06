using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Padma.ViewModels;

namespace Padma.Models;

public class DownloadProgressTracker : ReactiveObject
{
    private readonly HomeViewModel _homeViewModel;
    public long TotalSize;
    private FileSystemWatcher? _folderWatcher;
    private FileSystemWatcher? _downloadWatcher;
    private long _currentSize;
    // For debouncing progress updates.
    private Timer? _progressDebounceTimer;
    // We'll store the download folder path once the folder is created.
    private string _downloadFolderPath = string.Empty;
    
    public DownloadProgressTracker(HomeViewModel homeViewModel)
    {
        _homeViewModel = homeViewModel;
        // When both AppId and WorkshopId are provided, start tracking.
        this.WhenAnyValue(x => x.AppId, x => x.WorkshopId,
                (appId, workshopId) => !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(workshopId))
            .Where(valid => valid)
            .Take(1)
            .Subscribe(_ => StartTrackingDownload(AppId, WorkshopId));

        this.WhenAnyValue(x => x._homeViewModel.DownloadStatusNow)
            .Subscribe(_ => StopTracking());
    }

    private string _appId;
    public string AppId
    {
        get => _appId;
        set => this.RaiseAndSetIfChanged(ref _appId, value);
    }

    private string _workshopId;
    public string WorkshopId
    {
        get => _workshopId;
        set => this.RaiseAndSetIfChanged(ref _workshopId, value);
    }

     // This method watches the parent directory for the download folder.
        public void StartTrackingDownload(string appId, string workshopId)
        {
            // Parent folder path where downloads are stored.
            string basePath = "/home/lagita/.local/share/Steam/steamapps/workshop/downloads";

            if (!Directory.Exists(basePath))
            {
                Console.WriteLine("Base folder does not exist. Waiting for it to be created.");
                return;
            }

            _folderWatcher = new FileSystemWatcher(basePath)
            {
                Filter = "*",
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = true
            };

            _folderWatcher.Created += (s, e) =>
            {
                var dirInfo = new DirectoryInfo(e.FullPath);
                // Check if the created folder's name is the workshopId and its parent's name is appId.
                if (dirInfo.Name.Equals(workshopId, StringComparison.OrdinalIgnoreCase) &&
                    dirInfo.Parent?.Name.Equals(appId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _downloadFolderPath = e.FullPath;
                    _folderWatcher.EnableRaisingEvents = false; // Stop watching parent folder.
                    AttachDownloadWatcher(_downloadFolderPath);
                }
            };
            _folderWatcher.EnableRaisingEvents = true;
        }

        // Attach a watcher to the newly created download folder.
        private void AttachDownloadWatcher(string folderPath)
        {
            // Initialize current size.
            _currentSize = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                            .Sum(file => new FileInfo(file).Length);

            _downloadWatcher = new FileSystemWatcher(folderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
            };

            // Instead of immediately recalculating on every event,
            // we reset a debounce timer.
            _downloadWatcher.Changed += OnDownloadFolderChanged;
            _downloadWatcher.Created += OnDownloadFolderChanged;
            _downloadWatcher.Deleted += OnDownloadFolderChanged;
            _downloadWatcher.Renamed += OnDownloadFolderChanged;

            _downloadWatcher.EnableRaisingEvents = true;

            // Create debounce timer, set to trigger after 500ms of inactivity.
            _progressDebounceTimer = new Timer(_ => RecalculateProgress(), null, Timeout.Infinite, Timeout.Infinite);
        }

        // When a file event occurs, restart the debounce timer.
        private void OnDownloadFolderChanged(object sender, FileSystemEventArgs e)
        {
            // Restart debounce timer for 500ms delay.
            _progressDebounceTimer?.Change(160, Timeout.Infinite);
        }

        // Recalculate the progress once there have been no file events for 500ms.
        private void RecalculateProgress()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_downloadFolderPath) && Directory.Exists(_downloadFolderPath))
                {
                    long newSize = Directory.GetFiles(_downloadFolderPath, "*", SearchOption.AllDirectories)
                                        .Sum(file => new FileInfo(file).Length);
                    _currentSize = newSize;
                    int downloadPercentage = (int)(Math.Round((double)_currentSize / TotalSize, 2) * 100);
                    _homeViewModel.DownloadProgress = downloadPercentage;
                    StopTracking();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recalculating download progress: {ex.Message}");
            }
        }
        
        private void StopTracking()
        {
            if (_homeViewModel.DownloadStatusNow is "Downloading") return;
            _downloadWatcher?.Dispose();
            _folderWatcher?.Dispose();
            _progressDebounceTimer?.Dispose();
            _homeViewModel.DownloadProgress = 0;
            _currentSize = 0;
        }

}