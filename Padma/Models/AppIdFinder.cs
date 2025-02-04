using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Padma.Models;

public class AppIdFinder
{
    public string AppId;
    public string ModTitle;
    public string ThumbnailUrl;
    private double FileSize;
    public long FileSizeBytes;
    public string FileSizeInfo;
    public event Func<string, Task>? LogAsync;

    public async Task AppFinder(string workshopId)
    {
        using (var client = new HttpClient())
        {
            // Prepare the POST request data
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("itemcount", "1"),
                new KeyValuePair<string, string>("publishedfileids[0]", workshopId)
            });

            try
            {
                await LogAsync($"Finding AppId for workshop item {workshopId}...");
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
                        FileSize = (double)(fileSize / 1_048_576.0);
                        if (Math.Floor(FileSize) >= 1000)
                        {
                            FileSize /= 1024;
                            FileSizeInfo = $"{FileSize:F1} GB";
                        }
                        else
                        {
                            FileSizeInfo = $"{FileSize:F1} MB";
                        }
                        await LogAsync($"Found AppId {AppId} for workshop item {workshopId}");
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
}