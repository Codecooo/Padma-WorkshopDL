using System;
using System.Collections.Generic;
using LiteDB;

namespace Padma.Models;

public class SupportedGamesData
{
    [BsonId] public int Id { get; set; }

    public string AppId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class SupportedGames : IDisposable
{
    private readonly LiteDatabase _db;

    public SupportedGames()
    {
        _db = new LiteDatabase("/home/lagita/RiderProjects/Padma/Padma/LiteDB/list_supported_games.db");
    }

    public ILiteCollection<SupportedGamesData> SupportData => _db.GetCollection<SupportedGamesData>("supported_games");

    public void Dispose()
    {
        _db.Dispose();
    }

    public IEnumerable<SupportedGamesData> GetAllGames()
    {
        return SupportData.FindAll();
    }
}