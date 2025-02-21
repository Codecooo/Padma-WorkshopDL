using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Padma.Services;

public class CmdRunner
{
    private readonly FolderPicker _folderPicker;
    public bool Success;
    public string SteamCmdDirPath = string.Empty;
    public string SteamCmdFilePath = string.Empty;
    public string DownloadPath = string.Empty;

    private const int MaxRetries = 6;
    private const int RetryDelaySeconds = 10;
    private const int DownloadTimeoutMinutes = 30;
    public event Func<string, Task>? LogAsync;

    public CmdRunner(FolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
    }

    public async Task RunSteamCmd(string workshopId, string appId)
    {
        SteamCmdDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Padma", "SteamCMD");
        SteamCmdFilePath = Path.Combine(SteamCmdDirPath, "steamcmd.sh");
        DownloadPath = _folderPicker.SelectedPath;
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
                await ModDownloader(workshopId, appId);
            }
            else
            {
                await SteamCmdDownloader();
                await ModDownloader(workshopId, appId);
            }
        }
        catch (Exception ex)
        {
            await LogAsync($"Error during download: {ex.Message}");
        }
    }

    public async Task SteamCmdDownloader()
    {
        string command =
            $"cd {SteamCmdDirPath} && curl -qL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh";
        string arguments = $"-c \"{command}\"";
        using var cts = new CancellationTokenSource();
        await LogAsync("Installing steamcmd..");
        try
        {
            await RunBash(arguments, cts.Token);
            await LogAsync($"steamcmd successfully extracted to {SteamCmdDirPath}");
        }
        catch (Exception ex)
        {
            await LogAsync($"Error: {ex.Message}");
        }
    }

    public async Task ModDownloader(string workshopId, string appId)
    {
        int retryCount = 0;
        bool downloadComplete = false;

        while (!downloadComplete && retryCount < MaxRetries)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(DownloadTimeoutMinutes));

            try
            {
                string arguments =
                    $"-c \"\\\"{SteamCmdFilePath}\\\" +force_install_dir \\\"{DownloadPath}\\\" +login anonymous +workshop_download_item {appId} {workshopId} +quit\"";

                await LogAsync($"Attempt {retryCount + 1} of {MaxRetries}");

                var downloadTask = RunBash(arguments, cts.Token);
                await downloadTask;

                // Check if download was successful
                if (Success)
                {
                    downloadComplete = true;
                    await LogAsync("Download completed successfully");
                }
                else
                {
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await LogAsync("Download timed out");
                retryCount++;
                if (retryCount < MaxRetries)
                {
                    await LogAsync($"Waiting {RetryDelaySeconds} seconds before retry...");
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                }
            }
            catch (Exception ex)
            {
                await LogAsync($"Error during download: {ex.Message}");
                retryCount++;
                if (retryCount < MaxRetries)
                {
                    await LogAsync($"Waiting {RetryDelaySeconds} seconds before retry...");
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                }
            }
        }

        if (!downloadComplete)
        {
            await LogAsync($"Failed to download after {MaxRetries} attempts");
            Success = false;
        }
    }

    private async Task RunBash(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var tcs = new TaskCompletionSource<bool>();

        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await LogAsync($"Output: {e.Data}");
                // Look for progress indicators in the output
                if (e.Data.Contains("Success. Downloaded"))
                {
                    Success = true;
                }
            }
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (e.Data != null && e.Data.Contains($"Downloading item  ...ERROR! Timeout downloading item"))
                await LogAsync($"Timeout, waiting to resume the download.");
            if (!string.IsNullOrEmpty(e.Data))
            {
                await LogAsync($"Error: {e.Data}");
            }
        };

        process.Exited += (sender, args) => { tcs.TrySetResult(true); };

        cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                LogAsync($"Error killing process: {ex.Message}").Wait();
            }
        });

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        await Task.WhenAny(tcs.Task, Task.Delay(-1, cancellationToken));

        if (!process.HasExited)
        {
            process.Kill();
            throw new OperationCanceledException("Download operation timed out");
        }

        await LogAsync($"SteamCmd exited with code {process.ExitCode}");
        Success = process.ExitCode == 0;
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