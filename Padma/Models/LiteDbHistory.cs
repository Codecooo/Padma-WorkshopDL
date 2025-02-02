using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using ReactiveUI;

namespace Padma.Models;

public class LiteDbHistory
{
    [BsonId] public int Id { get; set; }

    public DateTime Date { get; set; }
    public string WorkshopTitle { get; set; }
    public string WorkshopUrl { get; set; }
    public string DownloadLocation { get; set; }
}

public class SaveHistory : ReactiveObject, IDisposable
{
    private readonly LiteDatabase _db;
    private bool _newHistory;
    public SaveHistory()
    {
        _db = new LiteDatabase("/home/lagita/RiderProjects/Padma/Padma/LiteDB/history.db");
    }

    public ILiteCollection<LiteDbHistory> History => _db.GetCollection<LiteDbHistory>("history");

    public void Dispose()
    {
        _db.Dispose();
    }

    public bool NewHistory
    {
        get => _newHistory;
        set => this.RaiseAndSetIfChanged(ref _newHistory, value);
    }

    public event Func<string, Task>? LogAsync;

    public async Task SaveHistoryAsync(string workshopTitle, string workshopUrl, string downloadLocation)
    {
        await LogAsync($"Saving history for {workshopTitle}");
        var historyEntry = new LiteDbHistory
        {
            Date = DateTime.UtcNow,
            WorkshopTitle = workshopTitle,
            WorkshopUrl = workshopUrl,
            DownloadLocation = downloadLocation,
        };
        History.Insert(historyEntry);
        NewHistory = true;
    }
    
    public IEnumerable<LiteDbHistory> GetAllHistoryList()
    {
        return History.FindAll();
    }

    public void DeleteHistory()
    {
        History.DeleteAll();
    }
}