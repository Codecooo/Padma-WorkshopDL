using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Padma.Services;

public class CmdRunner
{
    private const int MaxRetries = 6;
    private const int RetryDelaySeconds = 10;
    private const int DownloadTimeoutMinutes = 30;
    private readonly FolderPicker _folderPicker;

    public string DownloadPath = string.Empty;
    public string SteamCmdDirPath = string.Empty;
    public string SteamCmdFilePath = string.Empty;
    public bool Success;

    public CmdRunner(FolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
    }

    public event Func<string, Task>? LogAsync;

    public async Task RunSteamCmd(string workshopId, string appId)
    {
        SteamCmdDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Padma", "steamcmd");

        if (!OperatingSystem.IsWindows())
        {
            SteamCmdDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Padma", "steamcmd");
        }

        SteamCmdFilePath = OperatingSystem.IsWindows()
            ? Path.Combine(SteamCmdDirPath, "steamcmd.exe")
            : Path.Combine(SteamCmdDirPath, "steamcmd.sh");
        DownloadPath = _folderPicker.SelectedPath;
        try
        {
            if (!Directory.Exists(SteamCmdDirPath))
            {
                Directory.CreateDirectory(SteamCmdDirPath);
                await LogAsync($"Directory {SteamCmdDirPath} created.");
            }

            if (File.Exists(SteamCmdFilePath))
            {
                await LogAsync($"Found {Path.GetFileName(SteamCmdFilePath)} in {SteamCmdDirPath}");
                await ModDownloader(workshopId, appId);
            }
            else
            {
                await SteamCmdDownloader();
                await ModDownloader(workshopId, appId);
            }
        }
        catch (Exception e)
        {
            await LogAsync($"Error trying to run SteamCmd: {e.Message}");
        }
    }

    /// <summary>
    ///     Download Steamcmd if no executable is found, in Windows it will use HttpClient and extract them because there is no
    ///     reliable way to do it in Windows except this. For Linux/Mac it will use bash with commands. Mainly because we need
    ///     to mark the shell script as executable.
    /// </summary>
    public async Task SteamCmdDownloader()
    {
        var downloadCommandOrUrl = GetSteamCmdUrlOrCommand();
        string archiveDownloadPath;
        var extractionPath = SteamCmdDirPath;

        try
        {
            await LogAsync("No steamcmd detected, Downloading steamcmd now...");
            await LogAsync("It take a while to update and download steamcmd, please wait...");
            using var httpClient = new HttpClient();

            if (OperatingSystem.IsWindows())
            {
                archiveDownloadPath = Path.Combine(SteamCmdDirPath, "steamcmd.zip");
                await LogAsync($"Downloading steamcmd to {archiveDownloadPath}");
                using (var stream = await httpClient.GetStreamAsync(downloadCommandOrUrl))
                using (var fileStream = new FileStream(archiveDownloadPath, FileMode.Create, FileAccess.Write,
                           FileShare.None, 8192, true))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await LogAsync($"Successfully downloaded steamcmd to {archiveDownloadPath}");

                await LogAsync($"Extracting steamcmd to {extractionPath}");
                ZipFile.ExtractToDirectory(archiveDownloadPath, extractionPath, true);
                File.Delete(archiveDownloadPath);
                await LogAsync($"Successfully extracted steamcmd to {extractionPath}");
            }
            else // Linux or macOS, using bash so it could function properly and simple
            {
                var arguments = GetBashArgumentsForCommand(downloadCommandOrUrl);
                await RunBash(arguments, CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            await LogAsync($"Error during SteamCMD download/extraction: {e.Message}");
        }
    }

    public async Task ModDownloader(string workshopId, string appId)
    {
        var retryCount = 0;
        var downloadComplete = false;
        var timeoutErrorReceived = false;

        do
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(DownloadTimeoutMinutes));

            try
            {
                // Format the command, quoting SteamCmdFilePath and DownloadPath
                var command = GetSteamCmdCommand(workshopId, appId);
                var arguments = GetBashArgumentsForCommand(command);
                await LogAsync($"Running steamcmd with {arguments}");
                await RunBash(arguments, cts.Token);

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
            catch (Exception e)
            {
                timeoutErrorReceived = false;
                await LogAsync($"Error during download: {e.Message}");
            }
        } while (!downloadComplete && retryCount < MaxRetries && timeoutErrorReceived);

        if (!downloadComplete)
        {
            await LogAsync($"Failed to download {workshopId}");
            Success = false;
        }
    }


    private async Task RunBash(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        if (OperatingSystem.IsWindows())
            process.StartInfo = new ProcessStartInfo
            {
                FileName = SteamCmdFilePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = SteamCmdDirPath
            };
        else
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        // Use 3 ms interval and StringBuilder for not freezing the UI
        var logsBuffer = new StringBuilder();
        var logTimer = new Timer(3);
        logTimer.Elapsed += async (sender, args) =>
        {
            var messagesToSend = string.Empty;

            lock (logsBuffer)
            {
                if (logsBuffer.Length > 0)
                {
                    messagesToSend = logsBuffer.ToString().TrimEnd();
                    logsBuffer.Clear();
                }
            }

            if (!string.IsNullOrEmpty(messagesToSend)) await LogAsync(messagesToSend);
        };
        logTimer.Start();

        var tcs = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (logsBuffer)
                {
                    logsBuffer.Append($"Output: {e.Data} ");
                }

                if (e.Data.Contains("Success. Downloaded")) Success = true;
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                lock (logsBuffer)
                {
                    logsBuffer.Append($"Error: {e.Data} ");
                }
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
                logsBuffer.Append($"Error killing process: {ex.Message}");
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

        lock (logsBuffer)
        {
            logsBuffer.Append($"SteamCmd exited with code {process.ExitCode}");
        }
    }

    private string GetSteamCmdUrlOrCommand()
    {
        var downloadUrlOrCommand = string.Empty;
        if (OperatingSystem.IsLinux())
            downloadUrlOrCommand =
                $"cd {SteamCmdDirPath} && curl -sqL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh";
        if (OperatingSystem.IsWindows())
            downloadUrlOrCommand = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        if (OperatingSystem.IsMacOS())
            downloadUrlOrCommand =
                $"cd {SteamCmdDirPath} && curl -sqL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh";
        return downloadUrlOrCommand;
    }

    private string GetBashArgumentsForCommand(string command)
    {
        return OperatingSystem.IsWindows() ? command : $"-c \"{command}\"";
    }

    private string GetSteamCmdCommand(string workshopId, string appId)
    {
        if (OperatingSystem.IsWindows())
            // Windows template: uses three placeholders.
            return string.Format(
                @"+force_install_dir ""{0}"" +login anonymous +workshop_download_item {1} {2} +quit",
                DownloadPath,
                appId,
                workshopId);

        // Linux/Mac template: uses four placeholders.
        return string.Format(
            @"""{0}"" +force_install_dir ""{1}"" +login anonymous +workshop_download_item {2} {3} +quit",
            SteamCmdFilePath,
            DownloadPath,
            appId,
            workshopId);
    }

    public async Task KillSteamCmd()
    {
        try
        {
            Success = false;
            await LogAsync("Killing steamcmd...");
            foreach (var process in Process.GetProcessesByName("steamcmd")) process.Kill();
            await LogAsync("Download has been canceled");
        }
        catch (Exception e)
        {
            await LogAsync($"Error killing steamcmd: {e.Message}");
        }
    }
}