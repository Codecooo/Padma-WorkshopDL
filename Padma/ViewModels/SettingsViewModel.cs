using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Padma.Models;
using Padma.Services;
using ReactiveUI;

namespace Padma.ViewModels;

public partial class SettingsViewModel : ReactiveObject
{
    private AppSettingsJsonClass? _appSettingsJson;
    private readonly string _appSettingsPath;
    private readonly FolderPicker _folderPicker;
    private readonly HomeViewModel _homeViewModel;
    private readonly SaveHistory _saveHistory;
    private readonly JsonNode? _settings;
    private bool _disableHistoryChecked;
    private bool _disableStellarisInstallChecked;
    private string _folderPathView;

    public SettingsViewModel(SaveHistory saveHistory, FolderPicker folderPicker, HomeViewModel homeViewModel)
    {
        _saveHistory = saveHistory;
        _folderPicker = folderPicker;
        _homeViewModel = homeViewModel;
        _folderPathView = _folderPicker.FolderPathView;
        _appSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Padma", "appsettings.json");

        if (!OperatingSystem.IsWindows())
        {
            _appSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Padma", "appsettings.json");
        }

        if (!File.Exists(_appSettingsPath)) CreateAppSettings();

        // Read the app settings from file 
        var appSettingsContent = File.ReadAllText(_appSettingsPath);
        _settings = JsonNode.Parse(appSettingsContent);
        ReadAppSettings();
    }

    private void ReadAppSettings()
    {
        var historyEnabled = bool.Parse(_settings["history_enabled"].ToString());
        var stellarisAutoEnabled = bool.Parse(_settings["auto_install_stellaris_mods"].ToString());
        var downloadPath = _settings["download_path"].ToString();

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
        _appSettingsJson = new ()
        {
            HistoryEnabled = true,
            DownloadPath = "default",
            AutoInstallStellarisMods = true
        };
        string jsonString = JsonSerializer.Serialize(_appSettingsJson, JsonSerializerGenerator.Default.AppSettingsJsonClass);
        File.WriteAllText(_appSettingsPath, jsonString);    
    }

    [RelayCommand]
    private void RememberHistoryToggle()
    {
        _saveHistory.HistoryEnabled = !_saveHistory.HistoryEnabled;
        _settings["history_enabled"] = _saveHistory.HistoryEnabled;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }

    [RelayCommand]
    private void DisableStellarisAutoInstallMods()
    {
        _homeViewModel.StellarisAutoInstallEnabled = !_homeViewModel.StellarisAutoInstallEnabled;
        _settings["auto_install_stellaris_mods"] = _homeViewModel.StellarisAutoInstallEnabled;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _saveHistory.DeleteHistory();
    }

    [RelayCommand]
    private void ResetPadma()
    {
        _saveHistory.DeleteHistory();
        var steamappsPath = Path.Combine(_folderPicker.SelectedPath, "steamapps");
        var steamcmdPath = Path.Combine(_folderPicker.SelectedPath, "steamcmd");
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
        FolderPathView = _folderPicker.FolderPathView;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
    }

    [RelayCommand]
    private async Task SelectFolderPath()
    {
        await _folderPicker.PickFolder();
        _settings["download_path"] = _folderPicker.SelectedPath;
        File.WriteAllText(_appSettingsPath, _settings.ToString());
        FolderPathView = _folderPicker.FolderPathView;
    }

    #region ReactiveUI Public Properties

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

    #endregion
}

public class AppSettingsJsonClass
{
    public bool HistoryEnabled { get; set; }
    public bool AutoInstallStellarisMods { get; set; }
    public string DownloadPath { get; set; }
}