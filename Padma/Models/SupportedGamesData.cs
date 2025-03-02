using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public SupportedGames()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "list_supported_games.db");
        var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Padma", "data", "list_supported_games.db");

        if (!File.Exists(targetPath)) File.Copy(dbPath, targetPath);
    }

    public IEnumerable<SupportedGamesData> GetAllGames()
    {
        var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Padma", "data", "list_supported_games.db");
        using (var db = new LiteDatabase(targetPath))
        {
            var supportedGamesData = db.GetCollection<SupportedGamesData>("supported_games");
            return supportedGamesData.FindAll().ToList();
        }
    }
}