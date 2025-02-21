using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using ReactiveUI;
using Newtonsoft.Json.Linq;
using Padma.Services;
using Newtonsoft.Json;

namespace Padma.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private readonly SaveHistory _saveHistory;
    private readonly FolderPicker _folderPicker;
    private readonly HomeViewModel _homeViewModel;
    private string _folderPathView;
    private bool _disableStellarisInstallChecked;
    private bool _disableHistoryChecked;
    private readonly string _appSettingsPath;
    private JObject? _settings;

    public SettingsViewModel(SaveHistory saveHistory, FolderPicker folderPicker, HomeViewModel homeViewModel)
    {
        _saveHistory = saveHistory;
        _folderPicker = folderPicker;
        _homeViewModel = homeViewModel;
        _folderPathView = _folderPicker.FolderPathView;
        _appSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Padma", "appsettings.json");
        if (!File.Exists(_appSettingsPath))
            CreateAppSettings();
        
        // Read the app settings from file 
        var appSettingsContent = File.ReadAllText(_appSettingsPath);
        _settings = JObject.Parse(appSettingsContent);
        ReadAppSettings();
    }

    private void ReadAppSettings()
    {
        bool historyEnabled = bool.Parse(_settings["history_enabled"].ToString());
        bool stellarisAutoEnabled = bool.Parse(_settings["auto_install_stellaris_mods"].ToString());
        string downloadPath = _settings["download_path"].ToString();

        if (!historyEnabled)
        {
            DisableHistoryChecked = true;
            _saveHistory.HistoryEnabled = false;
        }
        
        if (!stellarisAutoEnabled)
        {
            DisableStellarisInstallChecked = true;
            _homeViewModel.StellarisAutoInstallEnabled = false;
        }
        
        if (downloadPath != "default")
        {
            _folderPicker.UpdatePaths(downloadPath);
            FolderPathView = _folderPicker.FolderPathView;
        }
    }

    private void CreateAppSettings()
    {
        File.Create(_appSettingsPath).Dispose();
        var newAppSettings = new
        {
            history_enabled = "true",
            download_path = "default",
            auto_install_stellaris_mods = "true"
        };
        var json = JsonConvert.SerializeObject(newAppSettings, Formatting.Indented);
        File.WriteAllText(_appSettingsPath, json);
    }

    [RelayCommand]
    public void RememberHistoryToggle()
    {
        _saveHistory.HistoryEnabled = !_saveHistory.HistoryEnabled;
        _settings["history_enabled"] = _saveHistory.HistoryEnabled;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }
    
    [RelayCommand]
    public void DisableStellarisAutoInstallMods()
    {
        _homeViewModel.StellarisAutoInstallEnabled = !_homeViewModel.StellarisAutoInstallEnabled;
        _settings["auto_install_stellaris_mods"] = _homeViewModel.StellarisAutoInstallEnabled;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }
    
    [RelayCommand]
    public void ClearHistory() => _saveHistory.DeleteHistory();

    [RelayCommand]
    public void ResetPadma()
    {
        _saveHistory.DeleteHistory();
        string steamappsPath = Path.Combine(_folderPicker.SelectedPath, "steamapps");
        string steamcmdPath = Path.Combine(_folderPicker.SelectedPath, "SteamCMD");
        try
        {
            Directory.Delete(steamappsPath, true);
            Directory.Delete(steamcmdPath, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        _settings["history_enabled"] = true;
        _settings["auto_install_stellaris_mods"] = true;
        _settings["download_path"] = "default";
        DisableHistoryChecked = false;
        DisableStellarisInstallChecked = false;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }

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

    public bool DisableHistoryChecked
    {
        get => _disableHistoryChecked;
        set => this.RaiseAndSetIfChanged(ref _disableHistoryChecked, value);
    }

    public bool DisableStellarisInstallChecked
    {
        get => _disableStellarisInstallChecked;
        set => this.RaiseAndSetIfChanged(ref _disableStellarisInstallChecked, value);
    }
}
