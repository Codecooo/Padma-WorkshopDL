using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Padma.Models;
using Padma.Services;
using Padma.ViewModels;

namespace Padma.Views
{
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

            // Create ViewModels with dependencies
            var supportedGamesViewModel = new SupportedGamesViewModel(supportedGames);
            var homeViewModel = new HomeViewModel(
                saveHistory,
                appIdFinder,
                cmdRunner,
                thumbnailLoader,
                downloadProgressTracker,
                folderpicker
            );
            var historyViewModel = new HistoryViewModel(saveHistory, homeViewModel);
            var settingsViewModel = new SettingsViewModel(saveHistory, folderpicker);

            return new MainWindowViewModel(
                supportedGamesViewModel,
                historyViewModel,
                homeViewModel,
                settingsViewModel
            );
        }
    }
}