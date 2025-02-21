using System.Threading.Tasks;
using System;
using Avalonia;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using Avalonia.Controls.ApplicationLifetimes;


namespace Padma.Services;

public class FolderPicker
{
    private readonly string _settingsPath = "/home/lagita/RiderProjects/Padma/Padma/appsettings.json";
    public string SelectedPath { get; private set; }
    public string FolderPathView { get; private set; }
    public event Func<string, Task>? LogAsync;

    public FolderPicker()
    {
        InitializeFromSettings();
    }

    private void InitializeFromSettings()
    {
        try
        {
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma");
            var settings = JObject.Parse(File.ReadAllText(_settingsPath));
            var downloadPath = settings["download_path"]?.ToString();
            
            SelectedPath = downloadPath == "default" ? defaultPath : downloadPath ?? defaultPath;
            FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
        }
        catch
        {
            // Fallback to default if settings file can't be read
            SelectedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma");
            FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
        }
    }

    public void UpdatePaths(string newSelectedPath)
    {
        SelectedPath = newSelectedPath;
        FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
    }
    
    public async Task PickFolder()
    {
        try
        {
            var folder = await DoOpenFilePickerAsync();
            if (folder != null && folder.Any())
            {
                SelectedPath = folder.First().Path.LocalPath;
                FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
            }
        }
        catch (Exception e)
        {
            await LogAsync(e.Message);
        }
    }
    public async Task OpenFolder(string folderPath)
    {
        try
        {
            Process.Start("xdg-open", $"{folderPath}");
        }
        catch (Exception e)
        {
            await LogAsync(e.Message);
        }
    }
    
    private async Task<IReadOnlyList<IStorageFolder>?> DoOpenFilePickerAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            await LogAsync("Missing StorageProvider instance.");
            return null; 
        }

        var folder = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder",
            AllowMultiple = false
        });

        return folder;
    }
}