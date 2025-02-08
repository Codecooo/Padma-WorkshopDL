
namespace Padma.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        SupportedGamesViewModel supportedGamesViewModel,
        HistoryViewModel historyViewModel,
        HomeViewModel homeViewModel,
        SettingsViewModel settingsViewModel)
    {
        SupportedGamesViewModel = supportedGamesViewModel;
        HistoryViewModel = historyViewModel;
        HomeViewModel = homeViewModel;
        SettingsViewModel = settingsViewModel;
    }

    public SupportedGamesViewModel SupportedGamesViewModel { get; }
    public HistoryViewModel HistoryViewModel { get; }
    public HomeViewModel HomeViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
}
