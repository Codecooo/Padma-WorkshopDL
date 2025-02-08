using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Padma.Services;

public class ThumbnailLoader
{
    private readonly HttpClient _httpClient;

    public ThumbnailLoader()
    {
        // Initialize the HttpClient in the constructor
        _httpClient = new HttpClient();
        LoadThumbnail("");
    }

    // Property for the thumbnail
    public Bitmap Thumbnail { get; set; }
    public event Func<string, Task>? LogAsync;

    public async Task<Bitmap> LoadThumbnail(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                url = "https://static.wikia.nocookie.net/logopedia/images/f/f7/Steam_Workshop_2011.png/revision/latest?cb=20220827024345";
            await LogAsync($"Loading thumbnail from {url}");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                // Create an Avalonia Bitmap from the stream
                var bitmap = new Bitmap(stream);

                Thumbnail = bitmap;
                await LogAsync($"Successfully loaded thumbnail from {url}");
                return bitmap;
            }
        }
        catch (Exception e)
        {
            await LogAsync($"Failed to load thumbnail from {url}: {e.Message}");
            throw;
        }
    }
}