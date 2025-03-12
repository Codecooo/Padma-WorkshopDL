using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
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
    private static readonly object DbLock = new();
    private readonly string _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Padma", "data");
    public bool HistoryEnabled = true;

    public SaveHistory()
    {
        if (!OperatingSystem.IsWindows())
            _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
                "data");

        if (!Directory.Exists(_dbPath))
            Directory.CreateDirectory(_dbPath);

        _dbPath = Path.Combine(_dbPath, "history.db");
    }

    public Subject<Unit> HistoryChangedSignal { get; } = new();

    public event Func<string, Task>? LogAsync;

    /// <summary>
    ///     Method to save the download to the LiteDb if the history is enabled
    ///     It has to use lock keyword and shared connection because Windows doesn't like when multiple threads open the same file
    ///     We use dd/mm/yy for date because that's what the rest of the world uses
    /// </summary>
    public async Task SaveHistoryAsync(string workshopTitle, string? workshopUrl, string downloadLocation,
        string downloadSize, long downloadSizeBytes)
    {
        LogAsync?.Invoke($"Saving history for {workshopTitle}");
        try
        {
            lock (DbLock)
            {
                using (var db = new LiteDatabase($"Filename={_dbPath};Connection=shared"))
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
        }
        catch (Exception e)
        {
            await LogAsync($"Failed to save history: {e.Message}");
        }
    }

    public IEnumerable<LiteDbHistory> GetAllHistoryList()
    {
        lock (DbLock)
        {
            using (var db = new LiteDatabase($"Filename={_dbPath};Connection=shared"))
            {
                var history = db.GetCollection<LiteDbHistory>("history");
                return history.FindAll().ToList();
            }   
        }
    }

    public void DeleteHistory()
    {
        lock (DbLock)
        {
            using (var db = new LiteDatabase($"Filename={_dbPath};Connection=shared"))
            {
                var history = db.GetCollection<LiteDbHistory>("history");
                history.DeleteAll();
                HistoryChangedSignal.OnNext(Unit.Default);
            }   
        }
    }
}