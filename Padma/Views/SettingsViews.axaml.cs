using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.Views;

public partial class SettingsViews : UserControl
{
    private readonly SaveHistory _history;
    public SettingsViews()
    {
        InitializeComponent();
        // Get instance through Dependency Injection in App.axaml.cs to ensure its running on the same instance as everyone else
        _history = App.ServiceProvider.GetRequiredService<SaveHistory>();
    }
    
    /// <summary>
    /// If the disable history is enabled it will disable LiteDb feature through this method
    /// </summary>
    private void RememberHistoryToggleChanged(object? sender, RoutedEventArgs e)
    {
        // Bool HistoryEnabled will be set by negating the value of RememberHistoryToggle so if
        // its checked i.e. true HistoryEnabled set to false but if its null it will be defaulted to true
        _history.HistoryEnabled = !(RememberHistoryToggle.IsChecked ?? true);
    }

   /// <summary>
   /// Used to clear the history database
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
    private void ClearHistory(object? sender, RoutedEventArgs e)
    {
        _history.DeleteHistory();
    }
}