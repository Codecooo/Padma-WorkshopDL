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
        _httpClient = new HttpClient();
    }

    // Property for the thumbnail
    public Bitmap Thumbnail { get; set; }
    public event Func<string, Task>? LogAsync;

    public async Task<Bitmap> LoadThumbnail(string url)
    {
        try
        {
            if (LogAsync is not null)
                await LogAsync($"Loading thumbnail from {url}");
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                // Create an Avalonia Bitmap from the stream
                var bitmap = new Bitmap(stream);

                Thumbnail = bitmap;
                if (LogAsync is not null)
                    await LogAsync($"Successfully loaded thumbnail from {url}");
                return bitmap;
            }
        }
        catch (Exception e)
        {
            if (LogAsync is not null)
                await LogAsync($"Failed to load thumbnail from {url}: {e.Message}");
            throw;
        }
    }
}