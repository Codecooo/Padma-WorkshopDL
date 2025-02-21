using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using ReactiveUI;
using System.Reactive.Subjects;
using System.Reactive;

namespace Padma.Models
{
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
        public long DownloadSizeBytes { get; set; }
    }

    public class SaveHistory : ReactiveObject, IDisposable
    {
        private readonly LiteDatabase _db;
        public bool HistoryEnabled = true;
        public Subject<Unit> HistoryChangedSignal { get; } = new Subject<Unit>();

        public SaveHistory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma", "data");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!File.Exists($"{path}/history.db"))
                File.Create($"{path}/history.db").Close();
            _db = new LiteDatabase($"{path}/history.db");
        }

        public ILiteCollection<LiteDbHistory> History => _db.GetCollection<LiteDbHistory>("history");

        public event Func<string, Task>? LogAsync;

        public async Task SaveHistoryAsync(string workshopTitle, string? workshopUrl, string downloadLocation, string downloadSize, long downloadSizeBytes)
        {
            if (LogAsync != null)
                await LogAsync($"Saving history for {workshopTitle}");
    
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma", "data", "history.db");
            using(var db = new LiteDatabase(dbPath))
            {
                var history = db.GetCollection<LiteDbHistory>("history");
                var historyEntry = new LiteDbHistory
                {
                    Date = DateTime.UtcNow,
                    WorkshopTitle = workshopTitle,
                    WorkshopUrl = workshopUrl,
                    DownloadLocation = downloadLocation,
                    DownloadSize = downloadSize,
                    DownloadSizeBytes = downloadSizeBytes
                };
        
                history.Insert(historyEntry);
            }
    
            HistoryChangedSignal.OnNext(Unit.Default);
        }

        
        public IEnumerable<LiteDbHistory> GetAllHistoryList()
        {
            return History.FindAll();
        }
        
        public void DeleteHistory()
        {
            History.DeleteAll();
            HistoryChangedSignal.OnNext(Unit.Default);
        }
        
        public void Dispose()
        {
            _db.Dispose();
            HistoryChangedSignal.Dispose();
        }
    }
}
