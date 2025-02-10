using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using ReactiveUI;
using Padma.Services;

namespace Padma.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly SaveHistory _saveHistory;
    private readonly FolderPicker _folderPicker;
    private string _folderPathView;

    public SettingsViewModel(SaveHistory saveHistory, FolderPicker folderPicker)
    {
        _saveHistory = saveHistory;
        _folderPicker = folderPicker;
        _folderPathView = _folderPicker.FolderPathView;
    }
    
    [RelayCommand]
    public void RememberHistoryToggle()
        => _saveHistory.HistoryEnabled = !_saveHistory.HistoryEnabled;
    
    [RelayCommand]
    public void ClearHistory()
        => _saveHistory.DeleteHistory();

    [RelayCommand]
    public async Task SelectFolderPath()
    {
        await _folderPicker.PickFolder();
        FolderPathView = _folderPicker.FolderPathView;
    }

    public string FolderPathView
    {
        get => _folderPathView;
        set => this.RaiseAndSetIfChanged(ref _folderPathView, value);
    }
}