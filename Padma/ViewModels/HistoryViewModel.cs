using System;
using System.Collections.ObjectModel;
using System.Linq;
using Padma.Models;
using ReactiveUI;
using Avalonia.Threading;

namespace Padma.ViewModels;

public class HistoryViewModel : ReactiveObject
{
    private readonly SaveHistory _history;
    private ObservableCollection<LiteDbHistory> _historyList = new();
    private ObservableCollection<LiteDbHistory> _filteredHistory = new();
    private string? _searchText;

    public HistoryViewModel(SaveHistory db)
    {
        _history = db;
        LoadHistory();

        // React to changes in SearchText
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(SearchHistory);
        _history.HistoryChangedSignal
            .Subscribe(_ => UpdateUiIfNewAdded());
    }
    
    public ObservableCollection<LiteDbHistory> HistoryList
    {
        get => _historyList;
        set => this.RaiseAndSetIfChanged(ref _historyList, value);
    }

    public ObservableCollection<LiteDbHistory> FilteredHistory
    {
        get => _filteredHistory;
        set => this.RaiseAndSetIfChanged(ref _filteredHistory, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    private void LoadHistory()
    {
        var allhistory = _history.GetAllHistoryList().ToList();
        HistoryList = new ObservableCollection<LiteDbHistory>(allhistory); // Initialize with all games
        FilteredHistory = new ObservableCollection<LiteDbHistory>(allhistory); // Initialize with all games
    }

    private void SearchHistory(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // If search text is empty, show all games
            FilteredHistory = new ObservableCollection<LiteDbHistory>(HistoryList);
        }
        else
        {
            // Filter games based on search text (case-insensitive)
            var filtered = HistoryList
                .Where(g => g.WorkshopTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            g.WorkshopUrl.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredHistory = new ObservableCollection<LiteDbHistory>(filtered);
        }
    }

    private void UpdateUiIfNewAdded()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadHistory();
        });
    }
}