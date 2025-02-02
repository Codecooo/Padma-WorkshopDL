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
                    await LogAsync($"Raw JSON response: {json}");

                    var data = JObject.Parse(json);

                    // Extract the AppID from the JSON response
                    var appId = data["response"]?["publishedfiledetails"]?[0]?["consumer_app_id"]?.Value<string>();
                    var modTitle = data["response"]?["publishedfiledetails"]?[0]?["title"]?.Value<string>();
                    var thumbnailUrl = data["response"]?["publishedfiledetails"]?[0]?["preview_url"]?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(modTitle) &&
                        !string.IsNullOrWhiteSpace(thumbnailUrl))
                    {
                        ModTitle = modTitle;
                        AppId = appId;
                        ThumbnailUrl = thumbnailUrl;
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
                await LogAsync($"Error finding AppID: {ex.Message}");
            }
        }
    }
}