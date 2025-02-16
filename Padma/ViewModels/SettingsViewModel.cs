using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using ReactiveUI;
using Newtonsoft.Json.Linq;
using Padma.Services;

namespace Padma.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly SaveHistory _saveHistory;
    private readonly FolderPicker _folderPicker;
    private string _folderPathView;
    private string _appSettings;
    private bool _historyEnabled;
    private bool _isChecked;
    private string? _downloadPath;
    private string _appSettingsPath;
    private JObject? _settings;

    public SettingsViewModel(SaveHistory saveHistory, FolderPicker folderPicker)
    {
        _saveHistory = saveHistory;
        _folderPicker = folderPicker;
        _folderPathView = _folderPicker.FolderPathView;
        _appSettingsPath = "/home/lagita/RiderProjects/Padma/Padma/appsettings.json";
        _appSettings = File.ReadAllText(_appSettingsPath);
        _settings = JObject.Parse(_appSettings);
        ReadAppSettings();
    }

    private void ReadAppSettings()
    {
        _historyEnabled = bool.Parse(_settings["history_enabled"].ToString());
        _downloadPath = _settings["download_path"].ToString();

        if (!_historyEnabled)
        {
            IsChecked = true;
            _saveHistory.HistoryEnabled = false;
        }
        
        if (_downloadPath is not "default")
        {
            _folderPicker.UpdatePaths(_downloadPath);
            FolderPathView = _folderPicker.FolderPathView;
        }
    }

    [RelayCommand]
    public void RememberHistoryToggle()
    {
        _saveHistory.HistoryEnabled = !_saveHistory.HistoryEnabled;
        _settings["history_enabled"] = _saveHistory.HistoryEnabled;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }
    
    [RelayCommand]
    public void ClearHistory()
        => _saveHistory.DeleteHistory();

    [RelayCommand]
    public async Task SelectFolderPath()
    {
        await _folderPicker.PickFolder();
        _settings["download_path"] = _folderPicker.SelectedPath;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
        FolderPathView = _folderPicker.FolderPathView;
    }

    public string FolderPathView
    {
        get => _folderPathView;
        set => this.RaiseAndSetIfChanged(ref _folderPathView, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}