using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Padma.Models;
using Padma.Services;
using Padma.ViewModels;

namespace Padma.Views
{
    public partial class MainWindow : Window
    {
        // Parameterless constructor required by XAML.
        public MainWindow() : this(
            Design.IsDesignMode 
                ? CreateDesignTimeViewModel() 
                : App.ServiceProvider.GetRequiredService<MainWindowViewModel>())
        { }

        // For runtime, the DI container will resolve MainWindowViewModel.
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            // Set the DataContext on the UI thread.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DataContext = viewModel;
            });
        }

        // Optional: Create a design-time view model if necessary.
        private static MainWindowViewModel CreateDesignTimeViewModel()
        {
            return new MainWindowViewModel(
                new SupportedGamesViewModel(new SupportedGames()),
                new HistoryViewModel(new SaveHistory()),
                new HomeViewModel(),
                new SettingsViewModel(App.ServiceProvider.GetRequiredService<SaveHistory>())
            );
        }
    }
}