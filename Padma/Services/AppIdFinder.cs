using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Padma.Services;

public class AppIdFinder
{
    public string AppId = string.Empty;
    public string WorkshopId = string.Empty;
    public string ModTitle = string.Empty;
    public string ThumbnailUrl = string.Empty;
    public long FileSizeBytes;
    public string FileSizeInfo = string.Empty;

    public event Func<string, Task>? LogAsync;

    public async Task ExtractWorkshopId(string workshopUrl)
    {
        // Match a numeric ID at the end of the URL path
        var regex = new Regex(@"id=(\d+)");
        var match = regex.Match(workshopUrl);

        if (match.Success) WorkshopId = match.Groups[1].Value;  
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
                    var data = JsonNode.Parse(json);

                    // Extract the AppID, title, thumbnail, and filesize from the JSON response
                    var appId = data["response"]?["publishedfiledetails"]?[0]?["consumer_app_id"]?.GetValue<int>();
                    var modTitle = data["response"]?["publishedfiledetails"]?[0]?["title"]?.GetValue<string>();
                    var thumbnailUrl = data["response"]?["publishedfiledetails"]?[0]?["preview_url"]?.GetValue<string>();
                    var fileSize = data["response"]?["publishedfiledetails"]?[0]?["file_size"]?.GetValue<string>();
                    if (appId != null && !string.IsNullOrWhiteSpace(modTitle) &&
                        !string.IsNullOrWhiteSpace(thumbnailUrl) && !string.IsNullOrWhiteSpace(fileSize))
                    {
                        // Assign the required information like ModTitle, AppID, thumbnail, and FileSize
                        ModTitle = modTitle;
                        AppId = appId.ToString();
                        ThumbnailUrl = thumbnailUrl;
                        FileSizeBytes = Convert.ToInt64(fileSize);
                        FileSizeInfo = CalculateFileSize(FileSizeBytes);

                        await LogAsync($"Found AppId {AppId} for workshop item {WorkshopId}");
                    }
                }
                else
                {
                    await LogAsync($"API request failed with status code {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                await LogAsync($"Error fetching for workshop info: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Calculate the size of the workshop mods
    /// </summary>
    /// <param name="fileSizeBytes"></param>
    /// <returns></returns>
    public string CalculateFileSize(long fileSizeBytes)
    {
        double filesize;
        filesize = fileSizeBytes / 1_048_576.0; // Convert to MB

        switch (Math.Floor(filesize))
        {
            // Calculate the filesize in bytes to readable format GB, MB or KB
            case >= 1000:
            {
                filesize /= 1024;
                return $"{filesize:F1} GB"; // Calculate to GB if the integral value is larger than 1000 MB
            }
            case < 1:
            {
                var fileSizeKb = FileSizeBytes / 1024.0;
                return $"{fileSizeKb:F1} KB"; // Calculate to KB if the value is less than 1 MB 
            }
            default:
            {
                return $"{filesize:F1} MB"; // If all conditions fail revert back to MB
            }
        }
    }
}