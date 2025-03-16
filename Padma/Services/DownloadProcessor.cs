using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Padma.Models;

namespace Padma.Services;

public class DownloadProcessor
{
    private readonly DownloadMods _downloadMods;
    private readonly AppIdFinder _appIdFinder;
    private readonly StellarisAutoInstall _stellarisAutoInstall;
    private readonly SaveHistory _historyService;
    private readonly FolderPicker _folderPicker;
    
    // Collection to track queued download items
    private readonly List<DownloadItem> _queuedItems = new();
    
    public event Func<string, Task>? LogAsync;
    
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
    
    /// <summary>
    /// Extract workshop information and prepare for download
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
    /// Calculate total size of all queued downloads
    /// </summary>
    /// <returns></returns>
    public string GetTotalDownloadSize()
    {
        long totalBytes = _queuedItems.Sum(item => item.SizeBytes);
        return _appIdFinder.CalculateFileSize(totalBytes);
    }
    
    /// <summary>
    /// Process downloads one by one, could be used for single download or multiple download
    /// It just calling DownloadMods class and then auto install stellaris mods and save download history
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
        try
        {
            await LogAsync($"Starting download for {item.Title}");
            
            var result = await _downloadMods.DownloadMod(item);
            
            if (result.Success)
            {
                await LogAsync($"Download completed for {item.Title}");
                
                // Handle Stellaris auto-install if enabled
                if (autoInstallStellaris && item.AppId == "281990")
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
                {
                    await _historyService.SaveHistoryAsync(
                        item.Title,
                        item.Url,
                        result.DownloadPath,
                        item.SizeInfo,
                        item.SizeBytes);
                }
                
            }
            else
            {
                await LogAsync($"Download failed for {item.Title}: {result.Error}");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await LogAsync($"Error processing download for {item.Title}: {ex.Message}");
            var failedResult = new DownloadResult
            {
                Success = false,
                Error = ex.Message,
                Item = item
            };
            
            return failedResult;
        }
    }
    
    /// <summary>
    /// Process all queued downloads for multiple downloads it just calling
    /// ProcessDownload method every loop. 
    /// </summary>
    /// <param name="autoInstallStellaris"></param>
    /// <param name="saveToHistory"></param>
    /// <returns></returns>
    public async Task<List<DownloadResult>> ProcessQueuedDownloads(
        bool autoInstallStellaris, 
        bool saveToHistory)
    {
        var results = new List<DownloadResult>();
        
        // Make a copy of the queue to avoid issues if queue is modified during iteration
        var itemsToProcess = new List<DownloadItem>(_queuedItems);
        
        await LogAsync($"Starting to process {itemsToProcess.Count} queued downloads");
        
        foreach (var item in itemsToProcess)
        {
            var result = await ProcessDownload(item, autoInstallStellaris, saveToHistory);
            results.Add(result);
            
            // Remove from queue if processed
            _queuedItems.Remove(item);
        }
        
        return results;
    }

    /// <summary>
    /// Cancel current download
    /// </summary>
    /// <returns></returns>
    public Task CancelMultipleDownload()
    {
        _queuedItems.Clear();
        return _downloadMods.CancelDownload();
    }
}