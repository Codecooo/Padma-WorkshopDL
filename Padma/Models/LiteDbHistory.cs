using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using ReactiveUI;
using System.Reactive.Subjects;
using System.Reactive;

namespace Padma.Models;

public class LiteDbHistory
{
    [BsonId] 
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string WorkshopTitle { get; set; }
    public string? WorkshopUrl { get; set; }
    public string DownloadLocation { get; set; }
    public string DownloadStatus { get; set; }
    public string DownloadSize { get; set; }
}

public class SaveHistory : ReactiveObject, IDisposable
{
    private readonly LiteDatabase _db;
    public bool HistoryEnabled = true;
    private string _downloadStatusChanged = string.Empty;    
    public Subject<Unit> HistoryChangedSignal { get; } = new Subject<Unit>();
    public SaveHistory()
    {
        _db = new LiteDatabase("/home/lagita/RiderProjects/Padma/Padma/LiteDB/history.db");
        
        this.WhenAnyValue(x => x.DownloadStatusChange)
            .Subscribe(_ => DownloadStatusChanged());
    }

    public ILiteCollection<LiteDbHistory> History => _db.GetCollection<LiteDbHistory>("history");

    public void Dispose()
    {
        _db.Dispose();
    }
    
    public event Func<string, Task>? LogAsync;

    public async Task SaveHistoryAsync(string workshopTitle, string? workshopUrl, string downloadLocation, string downloadSize)
    {
        await LogAsync($"Saving history for {workshopTitle}");
        var historyEntry = new LiteDbHistory
        {
            Date = DateTime.UtcNow,
            WorkshopTitle = workshopTitle,
            WorkshopUrl = workshopUrl,
            DownloadLocation = downloadLocation,
            DownloadStatus = DownloadStatusChange,
            DownloadSize = downloadSize
        };
        History.Insert(historyEntry);
        HistoryChangedSignal.OnNext(Unit.Default);
    }
    
    public IEnumerable<LiteDbHistory> GetAllHistoryList()
    {
        return History.FindAll();
    }
    
    public IEnumerable<LiteDbHistory> GetRecentHistoryList()
    {
        if (History.Count() != 0)
            yield return History.FindById(History.Max(x => x.Id));
    }

    public void DeleteHistory()
    {
        History.DeleteAll();
        HistoryChangedSignal.OnNext(Unit.Default);
    }

    private void DownloadStatusChanged()
    {
        if (!string.IsNullOrEmpty(_downloadStatusChanged))
        {
            var history = History.FindById(History.Max(x => x.Id));
            history.DownloadStatus = _downloadStatusChanged;
            History.Update(history);
        }
    }

    public string DownloadStatusChange
    {
        get => _downloadStatusChanged;
        set => this.RaiseAndSetIfChanged(ref _downloadStatusChanged, value);
    }
}