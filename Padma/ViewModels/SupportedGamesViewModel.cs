using System;
using System.Collections.ObjectModel;
using System.Linq;
using Padma.Models;
using ReactiveUI;

namespace Padma.ViewModels;

public class SupportedGamesViewModel : ReactiveObject
{
    private readonly SupportedGames _db;
    private ObservableCollection<SupportedGamesData> _filteredGames = new();
    private ObservableCollection<SupportedGamesData> _games = new();
    private string? _searchText;

    public SupportedGamesViewModel(SupportedGames db)
    {
        _db = db;
        LoadGames();

        // React to changes in SearchText
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(SearchGames);
    }

    public ObservableCollection<SupportedGamesData> Games
    {
        get => _games;
        set => this.RaiseAndSetIfChanged(ref _games, value);
    }

    public ObservableCollection<SupportedGamesData> FilteredGames
    {
        get => _filteredGames;
        set => this.RaiseAndSetIfChanged(ref _filteredGames, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    private void LoadGames()
    {
        var allGames = _db.GetAllGames().ToList();
        Games = new ObservableCollection<SupportedGamesData>(allGames);
        FilteredGames = new ObservableCollection<SupportedGamesData>(allGames); // Initialize with all games
    }

    private void SearchGames(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // If search text is empty, show all games
            FilteredGames = new ObservableCollection<SupportedGamesData>(Games);
        }
        else
        {
            // Filter games based on search text (case-insensitive)
            var filtered = Games
                .Where(g => g.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            g.AppId.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredGames = new ObservableCollection<SupportedGamesData>(filtered);
        }
    }
}