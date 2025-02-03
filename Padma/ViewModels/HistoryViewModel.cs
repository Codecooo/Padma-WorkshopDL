using System;
using System.Collections.ObjectModel;
using System.Linq;
using Padma.Models;
using ReactiveUI;

namespace Padma.ViewModels;

public class HistoryViewModel : ReactiveObject
{
    private readonly SaveHistory _history;
    private ObservableCollection<LiteDbHistory> _historyList = new();
    private ObservableCollection<LiteDbHistory> _filteredHistory = new();
    private bool _dontHaveHistory;
    private string? _searchText;
    
    public HistoryViewModel(SaveHistory db)
    {
        _history = db;
        LoadHistory();

        // React to changes in SearchText user enters
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(SearchHistory);
        
        // React to any changes with FilteredHistory and if so it subscribe to NoHistory
        // Where the value of NoHistory based on whether FilteredHistory contains any list and negates it
        // Used to set visibility of placeholder textblock
        this.WhenAnyValue(x => x.FilteredHistory)
            .Subscribe(_ => NoHistory = !FilteredHistory.Any());
        
        // React to changes signal in SaveHistory and then subscribe to LoadHistory method
        // Will be used to automatically refresh the UI if new download added or history is cleared in the database
        _history.HistoryChangedSignal
            .Subscribe(_ => LoadHistory());
    }
    
    /// <summary>
    /// Automatically notifies UI with set of download history in history.db set _historyList the value
    /// </summary>
    public ObservableCollection<LiteDbHistory> HistoryList
    {
        get => _historyList;
        set => this.RaiseAndSetIfChanged(ref _historyList, value);
    }
    
    /// <summary>
    /// Automatically notifies UI with set of download history in history.db set _filteredHistory the value
    /// This will be used when user search things 
    /// </summary>
    public ObservableCollection<LiteDbHistory> FilteredHistory
    {
        get => _filteredHistory;
        set => this.RaiseAndSetIfChanged(ref _filteredHistory, value);
    }
    
    /// <summary>
    /// Get the value user enter in the search and then notify the UI
    /// </summary>
    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    
    /// <summary>
    /// Load the history lists from history.db
    /// </summary>
    private void LoadHistory()
    {
        var allhistory = _history.GetAllHistoryList().ToList();
        HistoryList = new ObservableCollection<LiteDbHistory>(allhistory); 
        FilteredHistory = new ObservableCollection<LiteDbHistory>(allhistory); 
    }
    
    /// <summary>
    /// Method to actually do searching the database based on the search user enter.
    /// Probably need to debounce so it doesn't search all character??
    /// </summary>
    /// <param name="searchText"></param>
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
    
    /// <summary>
    /// Set the visibility of placeholder textblock Nothing to show here based if it has download history or not
    /// </summary>
    public bool NoHistory
    {
        get => _dontHaveHistory;
        set => this.RaiseAndSetIfChanged(ref _dontHaveHistory, value);
    }
}