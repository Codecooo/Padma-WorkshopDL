using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Padma.Services;

public class ThumbnailLoader
{
    public event Func<string, Task>? LogAsync;

    public async Task<Bitmap?> LoadThumbnail(string url)
    {
        try
        {
            using var httpClient = new HttpClient(); 
            LogAsync?.Invoke($"Loading thumbnail from {url}");
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                // Create an Avalonia Bitmap from the stream
                var bitmap = new Bitmap(stream);

                LogAsync?.Invoke($"Successfully loaded thumbnail from {url}");
                return bitmap;
            }
        }
        catch (Exception e)
        {
            LogAsync?.Invoke($"Failed to load thumbnail from {url}: {e.Message}");
            return null;
        }
    }
}