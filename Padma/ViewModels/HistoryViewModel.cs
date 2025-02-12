using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using ReactiveUI;

namespace Padma.ViewModels;

public partial class HistoryViewModel : ReactiveObject
{
    private readonly SaveHistory _history;
    private readonly HomeViewModel _homeViewModel;
    private ObservableCollection<LiteDbHistory> _historyList = new();
    private ObservableCollection<LiteDbHistory> _filteredHistory = new();
    private bool _dontHaveHistory;
    private string? _searchText;
    
    public HistoryViewModel(SaveHistory db, HomeViewModel homeViewModel)
    {
        _history = db;
        _homeViewModel = homeViewModel;
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
    
    [RelayCommand]
    private void OpenDownloads(LiteDbHistory selectedItems) => Process.Start("xdg-open", selectedItems.DownloadLocation);
    
    
    [RelayCommand]
    public void SortOldestDownloads()
    {
        var oldestDownloads = HistoryList.OrderBy(x  => x.Date);
        FilteredHistory = new ObservableCollection<LiteDbHistory>(oldestDownloads);
    }
    
    [RelayCommand]
    public void SortRecentDownloads()
    {
        var recentDownloads = HistoryList.OrderByDescending(x  => x.Date);
        FilteredHistory = new ObservableCollection<LiteDbHistory>(recentDownloads);
    }
    
    [RelayCommand]
    public void SortBiggestDownloads()
    {
        var biggestDownloads = HistoryList.OrderByDescending(x => x.DownloadSizeBytes);
        FilteredHistory = new ObservableCollection<LiteDbHistory>(biggestDownloads);
    }
    
    [RelayCommand]
    public void SortSmallestDownloads()
    {
        var smallestDownloads = HistoryList.OrderBy(x  => x.DownloadSizeBytes);
        FilteredHistory = new ObservableCollection<LiteDbHistory>(smallestDownloads);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    

    private void LoadHistory()
    {
        var allhistory = _history.GetAllHistoryList().ToList();
        HistoryList = new ObservableCollection<LiteDbHistory>(allhistory); 
        FilteredHistory = new ObservableCollection<LiteDbHistory>(allhistory.OrderByDescending(h => h.Date)); 
    }
    
    /// <summary>
    /// Method to actually do searching the database based on the search user enter.
    /// Probably need to debounce so it doesn't search all character??
    /// </summary>
    /// <param name="searchText"></param>
    private void SearchHistory(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return;
            // Filter games based on search text (case-insensitive)
            var filtered = HistoryList
                .Where(g => g.WorkshopTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            g.WorkshopUrl.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredHistory = new ObservableCollection<LiteDbHistory>(filtered);
    }
    
    public bool NoHistory
    {
        get => _dontHaveHistory;
        set => this.RaiseAndSetIfChanged(ref _dontHaveHistory, value);
    }
}