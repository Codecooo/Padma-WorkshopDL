using System.Threading.Tasks;
using System;
using Avalonia;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;


namespace Padma.Services;

public class FolderPicker
{
    public string SelectedPath =  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Padma");
    public string FolderPathView;
    public event Func<string, Task>? LogAsync;

    public FolderPicker()
    {
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
            Process.Start("xdg-open", folderPath);
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
            return null; // Ensure the function exits properly
        }

        var folder = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder",
            AllowMultiple = false
        });

        return folder;
    }
}