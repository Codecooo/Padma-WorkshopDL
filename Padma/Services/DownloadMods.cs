using System;
using System.IO;
using System.Threading.Tasks;

namespace Padma.Services;

public class DownloadMods
{
    private readonly CmdRunner _cmdRunner;
    private readonly AppIdFinder _appIdFinder;
    private readonly DownloadProgressTracker _progressTracker;
    
    public DownloadMods(CmdRunner cmdRunner, AppIdFinder appIdFinder, DownloadProgressTracker progressTracker)
    {
        _cmdRunner = cmdRunner;
        _appIdFinder = appIdFinder;
        _progressTracker = progressTracker;
    }
    
    public async Task<DownloadResult> DownloadMod(DownloadItem item)
    {
        try
        {
            // Set up the download tracking
            _progressTracker.DownloadFolder = item.DestinationFolder;
            _appIdFinder.SetValuesOfProgressTracker();
            
            // Perform the download
            await _cmdRunner.RunSteamCmd(item.WorkshopId, item.AppId, item.DestinationFolder);
            
            // Return the result
            return new DownloadResult
            {
                Success = _cmdRunner.Success,
                DownloadPath = Path.Combine(item.DestinationFolder, "steamapps", "workshop", "content", item.AppId, item.WorkshopId),
                Item = item
            };
        }
        catch (Exception)
        {
            return new DownloadResult
            {
                Success = false,
                Item = item
            };
        }
    }
    
    public Task CancelDownload()
    {
        return _cmdRunner.KillSteamCmd();
    }
}

public class DownloadItem
{
    public string WorkshopId { get; set; }
    public string AppId { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string DestinationFolder { get; set; }
    public string SizeInfo { get; set; }
    public long SizeBytes { get; set; }
}

public class DownloadResult
{
    public bool Success { get; set; }
    public string DownloadPath { get; set; }
    public DownloadItem Item { get; set; }
}
