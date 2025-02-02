using Avalonia.Controls;
using Padma.Models;
using Padma.ViewModels;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.Views;

public partial class MainWindow : Window
{
    public MainWindow() 
        : this(Design.IsDesignMode 
            ? new MainWindowViewModel(new SupportedGamesViewModel(new SupportedGames()), 
                new HistoryViewModel(new SaveHistory())) 
            : App.ServiceProvider.GetRequiredService<MainWindowViewModel>())
    {
    }



    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            DataContext = viewModel;
        });
    }
}