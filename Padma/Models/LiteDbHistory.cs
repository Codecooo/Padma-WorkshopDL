using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LiteDB;

namespace Padma.Models;

public class LiteDbHistory
{
    [BsonId] public int Id { get; set; }

    public string Date { get; set; }
    public string WorkshopTitle { get; set; }
    public string? WorkshopUrl { get; set; }
    public string DownloadLocation { get; set; }
    public string DownloadStatus { get; set; }
    public string DownloadSize { get; set; }
    public long DownloadSizeBytes { get; set; }
}

public class SaveHistory
{
    public bool HistoryEnabled = true;
    public SaveHistory()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
            "data");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        if (!File.Exists($"{path}/history.db"))
            File.Create($"{path}/history.db").Close();
    }

    public Subject<Unit> HistoryChangedSignal { get; } = new();

    public event Func<string, Task>? LogAsync;

    public async Task SaveHistoryAsync(string workshopTitle, string? workshopUrl, string downloadLocation,
        string downloadSize, long downloadSizeBytes)
    {
        if (LogAsync != null)
            await LogAsync($"Saving history for {workshopTitle}");

        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
            "data", "history.db");
        using (var db = new LiteDatabase(dbPath))
        {
            var history = db.GetCollection<LiteDbHistory>("history");
            var historyEntry = new LiteDbHistory
            {
                Date = DateTime.Now.ToString("g", new CultureInfo("en-GB")),
                WorkshopTitle = workshopTitle,
                WorkshopUrl = workshopUrl,
                DownloadLocation = downloadLocation,
                DownloadSize = downloadSize,
                DownloadSizeBytes = downloadSizeBytes
            };

            history.Insert(historyEntry);
            HistoryChangedSignal.OnNext(Unit.Default);
        }
    }

    public IEnumerable<LiteDbHistory> GetAllHistoryList()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
            "data", "history.db");
        using (var db = new LiteDatabase(dbPath))
        {
            var history = db.GetCollection<LiteDbHistory>("history");
            return history.FindAll().ToList();
        }
    }

    public void DeleteHistory()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
            "data", "history.db");
        using (var db = new LiteDatabase(dbPath))
        {
            var history = db.GetCollection<LiteDbHistory>("history");
            history.DeleteAll();
            HistoryChangedSignal.OnNext(Unit.Default);
        }
    }
}