using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace Padma.Models;

public class SupportedGamesData
{
    [BsonId] public int Id { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class SupportedGames
{
    private readonly string _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),"Padma", "data", "list_supported_games.db");

    public SupportedGames()
    {
        string sourcePath = Path.Combine(AppContext.BaseDirectory, "data", "list_supported_games.db");

        if (!OperatingSystem.IsWindows())
        {
            _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma",
                "data", "list_supported_games.db");
        }
        
        if (!File.Exists(_dbPath)) File.Copy(sourcePath, _dbPath);
    }

    public event Func<string, Task>? LogAsync;

    public IEnumerable<SupportedGamesData>? GetAllGames()
    {
        try
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var supportedGamesData = db.GetCollection<SupportedGamesData>("supported_games");
                return supportedGamesData.FindAll().ToList();
            }
        }
        catch (Exception e)
        {
            LogAsync?.Invoke($"Failed to get supported games: {e.Message}");
            return null;
        }
    }
}