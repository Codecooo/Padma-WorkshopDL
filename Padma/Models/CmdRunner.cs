using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Padma.Models;

public class CmdRunner
{
    public bool Success;
    public string SteamCmdDirPath = string.Empty;
    public string SteamCmdFilePath = string.Empty;
    public string DownloadPath = string.Empty;
    public event Func<string, Task>? LogAsync;

    public async Task RunSteamCmd(string WorkshopId, string AppId)
    { 
        SteamCmdDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Padma", "SteamCMD");   
        SteamCmdFilePath = Path.Combine(SteamCmdDirPath, "steamcmd.sh");
        try
        {
            if (!Directory.Exists(SteamCmdDirPath))
            {
                Directory.CreateDirectory(SteamCmdDirPath);
                await LogAsync($"Directory {SteamCmdDirPath} created.");
            }

            string[] files = Directory.GetFiles(SteamCmdDirPath, "steamcmd.sh", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                await LogAsync($"Found steamcmd.sh in {string.Join(", ", files)}");
                await ModDownloader(WorkshopId, AppId);
            }
            else
            {
                await SteamCmdDownloader();
                await ModDownloader(WorkshopId, AppId);
            }
        }
        catch (Exception ex)
        {
            await LogAsync($"Error during download: {ex.Message}");
        }
    }

    public async Task SteamCmdDownloader()
    {
        string command = $"cd {SteamCmdDirPath} && curl -qL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh";
        string arguments = $"-c \"{command}\"";
        await LogAsync("Installing steamcmd..");
        try
        {
            await RunBash(arguments);            
            await LogAsync($"steamcmd successfully extracted to {SteamCmdDirPath}");
        }
        catch (Exception ex)
        {
            await LogAsync($"Error: {ex.Message}");
        }
    }

    public async Task ModDownloader(string WorkshopId, string AppId)
    {
        string arguments = $"-c \"\\\"{SteamCmdFilePath}\\\" +force_install_dir \\\"{DownloadPath}\\\" +login anonymous +workshop_download_item {AppId} {WorkshopId} +quit\"";        
        try
        {
            await RunBash(arguments);
        }
        catch (Exception ex)
        {
            await LogAsync($"An error occurred: {ex.Message}");
            Success = false;
        }
    }

    private async Task RunBash(string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash", 
            Arguments = $"{arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Subscribe to output events
        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                await LogAsync($"Output: {e.Data}");
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                await LogAsync($"Error: {e.Data}");
        };
        
        process.Start();
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        
        await LogAsync($"SteamCMD process exited with code {process.ExitCode}");
        if (process.ExitCode == 0)
        {
            Success = true;
        }
    }

    public async Task KillSteamCmd()
    {
        Success = false;
        await LogAsync("Killing steamcmd...");
        foreach (var process in Process.GetProcessesByName("steamcmd"))
        {
            process.Kill();
        }
        await LogAsync("Download has been canceled");
    }
}