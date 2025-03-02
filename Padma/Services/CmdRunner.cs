using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Padma.Services;


public class CmdRunner
{
    private const int MaxRetries = 6;
    private const int RetryDelaySeconds = 10;
    private const int DownloadTimeoutMinutes = 30;
    private readonly FolderPicker _folderPicker;
    private readonly DownloadProgressTracker _downloadTracker;
    
    public string DownloadPath = string.Empty;
    public string SteamCmdDirPath = string.Empty;
    public string SteamCmdFilePath = string.Empty;
    public bool Success;

    public CmdRunner(FolderPicker folderPicker, DownloadProgressTracker downloadTracker)
    {
        _folderPicker = folderPicker;
        _downloadTracker = downloadTracker;
    }

    public event Func<string, Task>? LogAsync;

    /// <summary>
    /// Run steamcmd on bash, first check if the actual steamcmd is in the Padma directory if its not
    /// it will proceed to download steamcmd, then download the mod based on the WorkshopID and AppID
    /// If steamcmd found in the Padma directory it will send straight to download the mods
    /// </summary>
    /// <param name="workshopId"></param>
    /// <param name="appId"></param>
    public async Task RunSteamCmd(string workshopId, string appId)
    {
        SteamCmdDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Padma", "SteamCMD");
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
                // Start tracking SteamCMD installation progress
                await SteamCmdDownloader();
                await ModDownloader(workshopId, appId);
            }
        }
        catch (Exception ex)
        {
            await LogAsync($"Error during download: {ex.Message}");
        }
    }

    /// <summary>
    /// Download steamcmd with bash based on official Valve instructions for linux x86-64
    /// I could honestly do this with HTTP client but since I already have bash method might as well incorporate it
    /// </summary>
    public async Task SteamCmdDownloader()
    {
        var command =
            $"cd {SteamCmdDirPath} && curl -qL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh";
        var arguments = $"-c \"{command}\"";
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


    /// <summary>
    ///     Download the mods with bash for steamcmd, it provides delay if the process encounter any timeout error
    ///     usual for large size downloads. The default is 6 max attempts, this should suffice unless the user has really
    ///     bad internet speed or the size is abnormally large.
    /// </summary>
    /// <param name="workshopId"></param>
    /// <param name="appId"></param>
    public async Task ModDownloader(string workshopId, string appId)
    {
        var retryCount = 0;
        var downloadComplete = false;
        var timeoutErrorReceived = false;

        // Download Mods with 6 retry attempts for timeout error and cancellationtoken if the download session is exceeding
        // 30 minutes. Code will loop until it is either completed, exceeding 6 retry attempts or no timeout error received
        do
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(DownloadTimeoutMinutes));

            try
            {
                var arguments =
                    $"-c \"\\\"{SteamCmdFilePath}\\\" +force_install_dir \\\"{DownloadPath}\\\" +login anonymous +workshop_download_item {appId} {workshopId} +quit\"";

                var downloadTask = RunBash(arguments, cts.Token);
                await downloadTask;

                // Check if download was successful
                if (Success)
                {
                    downloadComplete = true;
                    timeoutErrorReceived = false;
                    await LogAsync("Download completed successfully");
                }
            }
            catch (OperationCanceledException)
            {
                timeoutErrorReceived = true;
                await LogAsync("Download timed out");
                retryCount++;
                if (retryCount < MaxRetries && timeoutErrorReceived)
                {
                    await LogAsync($"Waiting {RetryDelaySeconds} seconds before retry...");
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                }
            }
            catch (Exception ex)
            {
                timeoutErrorReceived = false;
                await LogAsync($"Error during download: {ex.Message}");
            }
        } while (!downloadComplete && retryCount < MaxRetries && timeoutErrorReceived);

        if (!downloadComplete)
        {
            await LogAsync($"Failed to download after {MaxRetries} attempts");
            Success = false;
        }
    }

    /// <summary>
    ///     Run bash method used for both downloading steamcmd and running steamcmd to download mods
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="OperationCanceledException"></exception>
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
                if (e.Data.Contains("Success. Downloaded")) Success = true;
            }
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) await LogAsync($"Error: {e.Data}");
        };

        process.Exited += (sender, args) => { tcs.TrySetResult(true); };

        cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill();
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
    }


    public async Task KillSteamCmd()
    {
        Success = false;
        await LogAsync("Killing steamcmd...");
        foreach (var process in Process.GetProcessesByName("steamcmd")) process.Kill();
        await LogAsync("Download has been canceled");
    }
}