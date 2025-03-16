using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Padma.Services;

public class FolderPicker
{
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Padma", "appsettings.json");

    public FolderPicker()
    {
        if (OperatingSystem.IsWindows()) _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        InitializeFromSettings();
    }

    public string SelectedPath { get; private set; }
    public string FolderPathView { get; private set; }
    public event Func<string, Task>? LogAsync;

    /// <summary>
    ///     Initialize the download location based on the appsettings.json
    /// </summary>
    private void InitializeFromSettings()
    {
        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Padma");

        if (!OperatingSystem.IsWindows())
            defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Padma");

        try
        {
            var settings = JsonNode.Parse(File.ReadAllText(_settingsPath));
            var downloadPath = settings["download_path"]?.ToString();

            SelectedPath = downloadPath == "default" ? defaultPath : downloadPath ?? defaultPath;
            SelectedPath = SelectedPath.TrimEnd(Path.DirectorySeparatorChar);
            FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
        }
        catch
        {
            // Fallback to default if settings file can't be read
            SelectedPath = defaultPath.TrimEnd(Path.DirectorySeparatorChar);
            FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
        }
    }

    public void UpdatePaths(string newSelectedPath)
    {
        SelectedPath = newSelectedPath.TrimEnd(Path.DirectorySeparatorChar);
        FolderPathView = Path.Combine(SelectedPath, "steamapps", "workshop", "content");
    }

    public async Task PickFolder()
    {
        try
        {
            var folder = await DoOpenFilePickerAsync();
            if (folder != null && folder.Any())
            {
                SelectedPath = folder.First().Path.LocalPath.TrimEnd(Path.DirectorySeparatorChar);
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
            if (OperatingSystem.IsLinux()) Process.Start("xdg-open", $"{folderPath}");
            if (OperatingSystem.IsWindows()) Process.Start("explorer", $"{folderPath}");
            if (OperatingSystem.IsMacOS()) Process.Start("open", $"{folderPath}");
        }
        catch (Exception e)
        {
            await LogAsync(e.Message);
        }
    }

    /// <summary>
    ///     Open FolderPicker storage method from Avalonia so user could choose desired folder path
    /// </summary>
    /// <returns></returns>
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