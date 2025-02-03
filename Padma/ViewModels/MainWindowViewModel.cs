namespace Padma.ViewModels;

public class MainWindowViewModel
{
    public SupportedGamesViewModel GamesViewModel { get; }
    public HistoryViewModel HistoryViewModel { get; }
    public HomeViewModel HomeViewModel { get; }

    public MainWindowViewModel(SupportedGamesViewModel gamesViewModel, HistoryViewModel historyViewModel, HomeViewModel homeViewModel)
    {
        GamesViewModel = gamesViewModel;
        HistoryViewModel = historyViewModel;
        HomeViewModel = homeViewModel;
    }
}