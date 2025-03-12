using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace Padma.Services;

public class DownloadProgressTracker : ReactiveObject
{
    private string _appId = string.Empty;
    private bool _isTracking;
    private string _workshopId = string.Empty;
    public long CurrentSize;
    public string DownloadFolder = string.Empty;
    public FileSystemWatcher? DownloadWatcher;
    public FileSystemWatcher? FolderWatcher;
    public Timer? ProgressDebounceTimer;
    public long TotalSize;

    public DownloadProgressTracker()
    {
        // Track changes to appID and workshopID when user provides so we could track it with FileSystemWatcher
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

    public event Func<string, Task>? LogAsync;

    public event Action<int>? ProgressUpdated;

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

    /// <summary>
    /// Start tracking download when appID and workshopID are provided with FileSystemWatcher
    /// First it would reset the download progress if it carried over, if steamcmd is installed
    /// for the first time it will create the workshop download folder so it could track it
    /// Then it will attach folder watcher by calling AttachDownloadWatcher
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="workshopId"></param>
    public void StartTrackingDownload(string appId, string workshopId)
    {
        if (_isTracking) Reset();
        _isTracking = true;

        DownloadFolder = Path.Combine(DownloadFolder, "steamapps", "workshop", "downloads");

        if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);

        FolderWatcher = new FileSystemWatcher(DownloadFolder)
        {
            Filter = "*",
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = true
        };

        FolderWatcher.Created += (s, e) =>
        {
            var expectedPath = Path.Combine(DownloadFolder, appId, workshopId);
            if (Directory.Exists(expectedPath))
            {
                DownloadFolder = expectedPath;
                AttachDownloadWatcher(DownloadFolder);
                return;
            }

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

    /// <summary>
    /// Attach FileSystemWatcher on the download folder and watch for any change in all of its
    /// directories. It will also implement a delay mechanism of 50ms so it is responsive enough while
    /// not overwhelm the computer with too much disk operations.
    /// </summary>
    /// <param name="folderPath"></param>
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

    /// <summary>
    /// Simply recalculate progress by retrieving all of the download folder current size
    /// and divide it by the Total size acquired when calling SteamwebApi in AppIdFinder class
    /// </summary>
    private async Task RecalculateProgress()
    {
        try
        {
            if (!_isTracking) return;

            if (!string.IsNullOrWhiteSpace(DownloadFolder) && Directory.Exists(DownloadFolder))
            {
                var newSize = Directory.GetFiles(DownloadFolder, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
                CurrentSize = newSize;
                var downloadPercentage = (int)(Math.Round((double)CurrentSize / TotalSize, 2) * 100);

                if (downloadPercentage >= 100) _isTracking = false;

                ProgressUpdated?.Invoke(downloadPercentage);
            }
        }
        catch (Exception e)
        {
            await LogAsync($"Error recalculating download progress: {e.Message}");
        }
    }
}