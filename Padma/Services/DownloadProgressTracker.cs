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
    public Timer? ProgressDebounceTimer;
    private string _workshopId = string.Empty;
    private string _appId = string.Empty;
    public string DownloadFolder = string.Empty;
    private bool _isTracking;
    
    public DownloadProgressTracker()
    {
        this.WhenAnyValue(x => x.AppId, x => x.WorkshopId,
                (appId, workshopId) => !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(workshopId))
            .Where(valid => valid)
            .Subscribe(_ => StartTrackingDownload(AppId, WorkshopId));
    }

    public string AppId
    {
        get => _appId;
        set => this.RaiseAndSetIfChanged(ref _appId, value);
    }

    public string WorkshopId
    {
        get => _workshopId;
        set => this.RaiseAndSetIfChanged(ref _workshopId, value);
    }

    public void Reset()
    {
        _isTracking = false;
        CurrentSize = 0;
        DisposeWatchers();
        ProgressUpdated?.Invoke(0);
    }

    private void DisposeWatchers()
    {
        DownloadWatcher?.Dispose();
        DownloadWatcher = null;
        FolderWatcher?.Dispose();
        FolderWatcher = null;
        ProgressDebounceTimer?.Dispose();
        ProgressDebounceTimer = null;
    }

    public void StartTrackingDownload(string appId, string workshopId)
    {
        if (_isTracking) Reset();
        _isTracking = true;
        
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
            if (dirInfo.Name.Equals(workshopId, StringComparison.OrdinalIgnoreCase) &&
                dirInfo.Parent?.Name.Equals(appId, StringComparison.OrdinalIgnoreCase) == true)
            {
                DownloadFolder = e.FullPath;
                FolderWatcher.EnableRaisingEvents = false;
                AttachDownloadWatcher(DownloadFolder);
            }
        };
        FolderWatcher.EnableRaisingEvents = true;
    }

    private void AttachDownloadWatcher(string folderPath)
    {
        CurrentSize = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                        .Sum(file => new FileInfo(file).Length);

        DownloadWatcher = new FileSystemWatcher(folderPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
        };

        DownloadWatcher.Changed += OnDownloadFolderChanged;
        DownloadWatcher.Created += OnDownloadFolderChanged;
        DownloadWatcher.Deleted += OnDownloadFolderChanged;
        DownloadWatcher.Renamed += OnDownloadFolderChanged;

        DownloadWatcher.EnableRaisingEvents = true;

        ProgressDebounceTimer = new Timer(_ => RecalculateProgress(), null, Timeout.Infinite, Timeout.Infinite);
    }

    private void OnDownloadFolderChanged(object sender, FileSystemEventArgs e)
    {
        if (!_isTracking) return;
        ProgressDebounceTimer?.Change(50, Timeout.Infinite);
    }

    private void RecalculateProgress()
    {
        try
        {
            if (!_isTracking) return;
            
            if (!string.IsNullOrWhiteSpace(DownloadFolder) && Directory.Exists(DownloadFolder))
            {
                long newSize = Directory.GetFiles(DownloadFolder, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
                CurrentSize = newSize;
                int downloadPercentage = (int)(Math.Round((double)CurrentSize / TotalSize, 2) * 100);
                
                if (downloadPercentage >= 100)
                {
                    _isTracking = false;
                }
                
                ProgressUpdated?.Invoke(downloadPercentage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recalculating download progress: {ex.Message}");
        }
    }
}