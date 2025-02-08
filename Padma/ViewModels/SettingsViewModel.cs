using CommunityToolkit.Mvvm.Input;
using Padma.Models;

namespace Padma.ViewModels;

public partial class SettingsViewModel
{
    private readonly SaveHistory _saveHistory;

    public SettingsViewModel(SaveHistory saveHistory)
    {
        _saveHistory = saveHistory;
    }
    
    [RelayCommand]
    public void RememberHistoryToggle()
        => _saveHistory.HistoryEnabled = !_saveHistory.HistoryEnabled;
}