using System.IO;
using System.Threading.Tasks;

namespace Padma.Services;

public class DownloadMods(CmdRunner cmdRunner, DownloadProgressTracker progressTracker)
{
    public async Task<DownloadResult> DownloadMod(DownloadItem item)
    {
        // Set up the download tracking
        progressTracker.DownloadFolder = item.DestinationFolder;
        progressTracker.AppId = item.AppId;
        progressTracker.WorkshopId = item.WorkshopId;
        progressTracker.TotalSize = item.SizeBytes;

        // Perform the download
        await cmdRunner.RunSteamCmd(item.WorkshopId, item.AppId, item.DestinationFolder);

        // Return the result
        return new DownloadResult
        {
            Success = cmdRunner.Success,
            DownloadPath = Path.Combine(item.DestinationFolder, "steamapps", "workshop", "content", item.AppId,
                item.WorkshopId),
            Item = item
        };
    }

    public Task CancelDownload()
    {
        return cmdRunner.KillSteamCmd();
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