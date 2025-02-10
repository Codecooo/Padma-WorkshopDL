using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;

namespace Padma.Services;

public class DownloadProgressTracker : ReactiveObject
{
    public event Action<int>? ProgressUpdated;
    public long TotalSize;
    public FileSystemWatcher? FolderWatcher;
    public FileSystemWatcher? DownloadWatcher;
    public long CurrentSize;
    // For debouncing progress updates.
    public Timer? ProgressDebounceTimer;
    // We'll store the download folder path once the folder is created.
    public string DownloadFolder;
    
    public DownloadProgressTracker()
    {
        // When both AppId and WorkshopId are provided, start tracking.
        this.WhenAnyValue(x => x.AppId, x => x.WorkshopId,
                (appId, workshopId) => !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(workshopId))
            .Where(valid => valid)
            .Take(1)
            .Subscribe(_ => StartTrackingDownload(AppId, WorkshopId));
    }

    private string _appId = string.Empty;
    public string AppId
    {
        get => _appId;
        set => this.RaiseAndSetIfChanged(ref _appId, value);
    }

    private string _workshopId = string.Empty;
    public string WorkshopId
    {
        get => _workshopId;
        set => this.RaiseAndSetIfChanged(ref _workshopId, value);
    }

     // This method watches the parent directory for the download folder.
        public void StartTrackingDownload(string appId, string workshopId)
        {
            DownloadFolder = Path.Combine(DownloadFolder, "steamapps", "workshop", "downloads");

            if (!Directory.Exists(DownloadFolder))
            {
                Console.WriteLine("Base folder does not exist. Waiting for it to be created.");
                return;
            }

            FolderWatcher = new FileSystemWatcher(DownloadFolder)
            {
                Filter = "*",
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = true
            };

            FolderWatcher.Created += (s, e) =>
            {
                var dirInfo = new DirectoryInfo(e.FullPath);
                // Check if the created folder's name is the workshopId and its parent's name is appId.
                if (dirInfo.Name.Equals(workshopId, StringComparison.OrdinalIgnoreCase) &&
                    dirInfo.Parent?.Name.Equals(appId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    DownloadFolder = e.FullPath;
                    FolderWatcher.EnableRaisingEvents = false; // Stop watching parent folder.
                    AttachDownloadWatcher(DownloadFolder);
                }
            };
            FolderWatcher.EnableRaisingEvents = true;
        }

        // Attach a watcher to the newly created download folder.
        private void AttachDownloadWatcher(string folderPath)
        {
            // Initialize current size.
            CurrentSize = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                            .Sum(file => new FileInfo(file).Length);

            DownloadWatcher = new FileSystemWatcher(folderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
            };

            // Instead of immediately recalculating on every event,
            // we reset a debounce timer.
            DownloadWatcher.Changed += OnDownloadFolderChanged;
            DownloadWatcher.Created += OnDownloadFolderChanged;
            DownloadWatcher.Deleted += OnDownloadFolderChanged;
            DownloadWatcher.Renamed += OnDownloadFolderChanged;

            DownloadWatcher.EnableRaisingEvents = true;

            // Create debounce timer, set to trigger after 160ms of inactivity.
            ProgressDebounceTimer = new Timer(_ => RecalculateProgress(), null, Timeout.Infinite, Timeout.Infinite);
        }

        // When a file event occurs, restart the debounce timer.
        private void OnDownloadFolderChanged(object sender, FileSystemEventArgs e)
        {
            // Restart debounce timer for 500ms delay.
            ProgressDebounceTimer?.Change(160, Timeout.Infinite);
        }

        // Recalculate the progress once there have been no file events for 500ms.
        private void RecalculateProgress()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(DownloadFolder) && Directory.Exists(DownloadFolder))
                {
                    long newSize = Directory.GetFiles(DownloadFolder, "*", SearchOption.AllDirectories)
                        .Sum(file => new FileInfo(file).Length);
                    CurrentSize = newSize;
                    int downloadPercentage = (int)(Math.Round((double)CurrentSize / TotalSize, 2) * 100);
                    ProgressUpdated?.Invoke(downloadPercentage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recalculating download progress: {ex.Message}");
            }
        }
}