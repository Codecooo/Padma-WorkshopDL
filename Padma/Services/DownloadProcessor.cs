using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Padma.Models;

namespace Padma.Services;

public class DownloadProcessor
{
    private readonly AppIdFinder _appIdFinder;
    private readonly DownloadMods _downloadMods;
    private readonly FolderPicker _folderPicker;
    private readonly SaveHistory _historyService;

    // Collection to track queued download items
    private readonly List<DownloadItem> _queuedItems = new();
    private readonly StellarisAutoInstall _stellarisAutoInstall;
    private CancellationTokenSource _cts;
    public bool IsDownloadingQueue;

    public DownloadProcessor(
        DownloadMods downloadMods,
        AppIdFinder appIdFinder,
        StellarisAutoInstall stellarisAutoInstall,
        SaveHistory historyService,
        FolderPicker folderPicker)
    {
        _downloadMods = downloadMods;
        _appIdFinder = appIdFinder;
        _stellarisAutoInstall = stellarisAutoInstall;
        _historyService = historyService;
        _folderPicker = folderPicker;
    }

    public event Func<string, Task>? LogAsync;
    public event Action<string>? WorkshopTitleChanged;

    /// <summary>
    ///     Extract workshop information and prepare for download
    /// </summary>
    /// <param name="workshopUrl"></param>
    /// <returns></returns>
    public async Task<DownloadItem> ExtractWorkshopInfo(string workshopUrl)
    {
        await _appIdFinder.ExtractWorkshopId(workshopUrl);
        await _appIdFinder.AppFinder();

        var item = new DownloadItem
        {
            WorkshopId = _appIdFinder.WorkshopId,
            AppId = _appIdFinder.AppId,
            Title = _appIdFinder.ModTitle,
            Url = workshopUrl,
            SizeInfo = _appIdFinder.FileSizeInfo,
            SizeBytes = _appIdFinder.FileSizeBytes,
            DestinationFolder = _folderPicker.SelectedPath
        };

        // Add to queued items
        _queuedItems.Add(item);

        return item;
    }

    /// <summary>
    ///     Calculate total size of all queued downloads
    /// </summary>
    /// <returns></returns>
    public string GetTotalDownloadSize()
    {
        var totalBytes = _queuedItems.Sum(item => item.SizeBytes);
        return _appIdFinder.CalculateFileSize(totalBytes);
    }

    /// <summary>
    ///     Process downloads one by one, could be used for single download or multiple download
    ///     It just calling DownloadMods class and then auto install stellaris mods and save download history
    /// </summary>
    /// <param name="item"></param>
    /// <param name="autoInstallStellaris"></param>
    /// <param name="saveToHistory"></param>
    /// <returns></returns>
    public async Task<DownloadResult> ProcessDownload(
        DownloadItem item,
        bool autoInstallStellaris,
        bool saveToHistory)
    {
        WorkshopTitleChanged?.Invoke(item.Title);
        await LogAsync($"Starting download for {item.Title}");

        var result = await _downloadMods.DownloadMod(item);

        if (result.Success)
        {
            await LogAsync($"Download completed for {item.Title}");

            // Handle stellaris auto-install if enabled
            if (autoInstallStellaris && item.AppId is "281990")
            {
                await LogAsync($"Workshop item {item.WorkshopId} is a Stellaris mod");
                result.DownloadPath = $"\"{_stellarisAutoInstall.StellarisDocPath}\"";
                await _stellarisAutoInstall.RunStellarisAutoInstallMods
                (
                    Path.Combine(_folderPicker.FolderPathView, item.AppId, item.WorkshopId),
                    item.Title
                );
            }

            // Save to history if enabled
            if (saveToHistory)
                await _historyService.SaveHistoryAsync(
                    item.Title,
                    item.Url,
                    result.DownloadPath,
                    item.SizeInfo,
                    item.SizeBytes);
        }

        _queuedItems.Clear();
        return result;
    }

    /// <summary>
    ///     Process all queued downloads for multiple downloads it's just calling
    ///     ProcessDownload method every loop.
    /// </summary>
    /// <param name="autoInstallStellaris"></param>
    /// <param name="saveToHistory"></param>
    /// <returns></returns>
    public async Task<List<DownloadResult>> ProcessQueuedDownloads(bool autoInstallStellaris, bool saveToHistory)
    {
        IsDownloadingQueue = true;
        var results = new List<DownloadResult>();

        // Make a copy of the queue to avoid issues if queue is modified during iteration
        var itemsToProcess = new List<DownloadItem>(_queuedItems);

        await LogAsync($"Starting to process {itemsToProcess.Count} queued downloads");

        _cts = new CancellationTokenSource();

        foreach (var items in itemsToProcess)
        {
            // Exit the loop if cancellation is requested
            if (_cts.Token.IsCancellationRequested)
            {
                await LogAsync("Download cancellation requested. Stopping all queues");
                break;
            }

            var item = items;
            var result = await ProcessDownload(item, autoInstallStellaris, saveToHistory);
            results.Add(result);

            // Remove from queue if processed, only if not cancelled
            if (!_cts.Token.IsCancellationRequested) _queuedItems.Remove(item);
        }

        IsDownloadingQueue = false;
        return results;
    }

    /// <summary>
    ///     Cancel current download
    /// </summary>
    /// <returns></returns>
    public Task CancelCurrentDownloads()
    {
        switch (IsDownloadingQueue)
        {
            case true:
            {
                _cts.Cancel();
                return _downloadMods.CancelDownload();
            }
            case false:
            {
                return _downloadMods.CancelDownload();
            }
        }
    }

    /// <summary>
    ///     Clear download queue
    /// </summary>
    public void ClearQueuedDownloads()
    {
        _queuedItems.Clear();
    }
}