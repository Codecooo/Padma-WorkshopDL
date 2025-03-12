using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Padma.Models;
using Padma.Services;
using Padma.ViewModels;

namespace Padma.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static MainWindowViewModel CreateDesignTimeViewModel()
    {
        // Create mock services first
        var downloadProgressTracker = new DownloadProgressTracker();
        var folderpicker = new FolderPicker();
        var saveHistory = new SaveHistory();
        var supportedGames = new SupportedGames();
        var appIdFinder = new AppIdFinder(downloadProgressTracker);
        var cmdRunner = new CmdRunner(folderpicker);
        var thumbnailLoader = new ThumbnailLoader();
        var stellarisAutoInstall = new StellarisAutoInstall();

        // Create ViewModels with dependencies
        var supportedGamesViewModel = new SupportedGamesViewModel(supportedGames);
        var homeViewModel = new HomeViewModel(
            saveHistory,
            appIdFinder,
            cmdRunner,
            thumbnailLoader,
            downloadProgressTracker,
            folderpicker,
            stellarisAutoInstall,
            supportedGames
        );
        var historyViewModel = new HistoryViewModel(saveHistory);
        var settingsViewModel = new SettingsViewModel(saveHistory, folderpicker, homeViewModel);

        return new MainWindowViewModel(
            supportedGamesViewModel,
            historyViewModel,
            homeViewModel,
            settingsViewModel
        );
    }
}