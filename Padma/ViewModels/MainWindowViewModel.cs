namespace Padma.ViewModels;

public class MainWindowViewModel
{
    public SupportedGamesViewModel GamesViewModel { get; }
    public HistoryViewModel HistoryViewModel { get; }

    public MainWindowViewModel(SupportedGamesViewModel gamesViewModel, HistoryViewModel historyViewModel)
    {
        GamesViewModel = gamesViewModel;
        HistoryViewModel = historyViewModel;
    }
}