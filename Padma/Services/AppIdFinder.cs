using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.Services;

public class AppIdFinder
{
    private readonly DownloadProgressTracker _downloadProgressTracker;
    public string AppId;
    public string WorkshopId = string.Empty;
    public string ModTitle;
    public string ThumbnailUrl;
    private double _fileSize;
    public long FileSizeBytes;
    public string FileSizeInfo;
    public event Func<string, Task>? LogAsync;

    public AppIdFinder()
    {
        _downloadProgressTracker = App.ServiceProvider.GetRequiredService<DownloadProgressTracker>();
    }
    
    public async Task ExtractWorkshopId(string workshopUrl)
    {
        // Match a numeric ID at the end of the URL path
        var regex = new Regex(@"id=(\d+)");
        var match = regex.Match(workshopUrl);

        if (match.Success)
        {
            WorkshopId = match.Groups[1].Value; // Assign first
        }
        if (string.IsNullOrEmpty(WorkshopId))
        {
            await LogAsync("Invalid Workshop ID.");
            return;
        }

        await LogAsync($"Extracted Workshop ID: {WorkshopId}");
    }
    
    public async Task AppFinder()
    {
        using (var client = new HttpClient())
        {
            // Prepare the POST request data
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("itemcount", "1"),
                new KeyValuePair<string, string>("publishedfileids[0]", WorkshopId)
            });

            try
            {
                await LogAsync($"Finding AppId for workshop item {WorkshopId}...");
                // Send the request to Steam's API
                var response = await client.PostAsync(
                    "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/",
                    formData
                );

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var data = JObject.Parse(json);

                    // Extract the AppID from the JSON response
                    var appId = data["response"]?["publishedfiledetails"]?[0]?["consumer_app_id"]?.Value<string>();
                    var modTitle = data["response"]?["publishedfiledetails"]?[0]?["title"]?.Value<string>();
                    var thumbnailUrl = data["response"]?["publishedfiledetails"]?[0]?["preview_url"]?.Value<string>();
                    var fileSize = data["response"]?["publishedfiledetails"]?[0]?["file_size"]?.Value<long>();
                    if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(modTitle) &&
                        !string.IsNullOrWhiteSpace(thumbnailUrl) && fileSize > 0)
                    {
                        ModTitle = modTitle;
                        AppId = appId;
                        ThumbnailUrl = thumbnailUrl;
                        FileSizeBytes = (long)fileSize;
                        _fileSize = (double)(fileSize / 1_048_576.0);
                        if (Math.Floor(_fileSize) >= 1000)
                        {
                            _fileSize /= 1024;
                            FileSizeInfo = $"{_fileSize:F1} GB";
                        }
                        else if (Math.Floor(_fileSize) < 1)
                        {
                            FileSizeBytes /= 1024;
                            FileSizeInfo = $"{_fileSize:F1} KB";
                        }
                        else
                        {
                            FileSizeInfo = $"{_fileSize:F1} MB";
                        }
                        await LogAsync($"Found AppId {AppId} for workshop item {WorkshopId}");
                    }
                    else
                    {
                        await LogAsync("AppID not found in response.");
                        AppId = "0";
                    }
                }
                else
                {
                    await LogAsync(
                        $"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Handle errors (e.g., network issues)
                await LogAsync($"Error finding AppID or Filesize: {ex.Message}");
            }
        }
    }
    
    public void SetValuesOfProgressTracker()
    {
        _downloadProgressTracker.AppId = AppId;
        _downloadProgressTracker.WorkshopId = WorkshopId;
        _downloadProgressTracker.TotalSize = FileSizeBytes;
    }
}