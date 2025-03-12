using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Padma.Services;

public class AppIdFinder
{
    private readonly DownloadProgressTracker _downloadProgressTracker;
    private double _fileSize;

    // Proper constructor injection
    public AppIdFinder(DownloadProgressTracker downloadProgressTracker)
    {
        _downloadProgressTracker = downloadProgressTracker;
    }

    public string AppId { get; private set; }
    public string WorkshopId { get; private set; } = string.Empty;
    public string ModTitle { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string FileSizeInfo { get; private set; }
    public event Func<string, Task>? LogAsync;

    public async Task ExtractWorkshopId(string workshopUrl)
    {
        // Match a numeric ID at the end of the URL path
        var regex = new Regex(@"id=(\d+)");
        var match = regex.Match(workshopUrl);

        if (match.Success) WorkshopId = match.Groups[1].Value; // Assign first
        if (string.IsNullOrEmpty(WorkshopId))
        {
            await LogAsync("Invalid Workshop ID.");
            return;
        }

        await LogAsync($"Extracted Workshop ID: {WorkshopId}");
    }

    /// <summary>
    ///     Find the necessary info about the workhop item through SteamWebAPI
    ///     Calculate the file size, title, appID, and thumbnail URL
    /// </summary>
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
                // Send the request to SteamWebAPI
                var response = await client.PostAsync
                (
                    "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/",
                    formData
                );

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(json);

                    // Extract the AppID, title, thumbnail, and filesize from the JSON response
                    var appId = data["response"]?["publishedfiledetails"]?[0]?["consumer_app_id"]?.Value<string>();
                    var modTitle = data["response"]?["publishedfiledetails"]?[0]?["title"]?.Value<string>();
                    var thumbnailUrl = data["response"]?["publishedfiledetails"]?[0]?["preview_url"]?.Value<string>();
                    var fileSize = data["response"]?["publishedfiledetails"]?[0]?["file_size"]?.Value<long>();
                    if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(modTitle) &&
                        !string.IsNullOrWhiteSpace(thumbnailUrl) && fileSize > 0)
                    {
                        // Assign the required information like ModTitle, AppID, thumbnail, and FileSize in bytes
                        ModTitle = modTitle;
                        AppId = appId;
                        ThumbnailUrl = thumbnailUrl;
                        FileSizeBytes = (long)fileSize;
                        _fileSize = (double)(fileSize / 1_048_576.0); // Convert to MB

                        switch (Math.Floor(_fileSize))
                        {
                            // Calculate the filesize in bytes to readable format GB, MB or KB
                            case >= 1000:
                                _fileSize /= 1024;
                                FileSizeInfo =
                                    $"{_fileSize:F1} GB"; // Calculate to GB if the integral value is larger than 1000 MB
                                break;
                            case < 1:
                            {
                                var fileSizeKb = FileSizeBytes / 1024.0;
                                FileSizeInfo = $"{fileSizeKb:F1} KB"; // Calculate to KB if the value is less than 1 MB 
                                break;
                            }
                            default:
                                FileSizeInfo = $"{_fileSize:F1} MB"; // If all conditions fail revert back to MB
                                break;
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

    /// <summary>
    ///     Set the value of the DownloadProgressTracker class to the appropriate information once succesfully
    ///     retrieved all necessary info through SteamWebAPI
    /// </summary>
    public void SetValuesOfProgressTracker()
    {
        _downloadProgressTracker.AppId = AppId;
        _downloadProgressTracker.WorkshopId = WorkshopId;
        _downloadProgressTracker.TotalSize = FileSizeBytes;
    }
}