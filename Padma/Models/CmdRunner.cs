using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Padma.Models;

public class CmdRunner
{
    public bool Success;
    public event Func<string, Task>? LogAsync;

    public async Task RunSteamCmd(string WorkshopId, string AppId)
    {
        var searchDirectory = "/home/lagita/SteamWorkshop";
        try
        {
            if (!Directory.Exists(searchDirectory))
            {
                Directory.CreateDirectory(searchDirectory);
                await LogAsync($"Directory {searchDirectory} created.");
            }

            string[] files = Directory.GetFiles(searchDirectory, "steamcmd.sh", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                await LogAsync($"Found steamcmd.sh in {string.Join(", ", files)}");
                await Task.Run(() => ModDownloader(WorkshopId, AppId));
            }
            else
            {
                await Task.Run(() => SteamCmdDownloader("/home/lagita/SteamWorkshop", "steamcmd.sh"));
                await Task.Run(() => ModDownloader(WorkshopId, AppId));
            }
        }
        catch (Exception ex)
        {
            await LogAsync($"Error during download: {ex.Message}");
        }
    }

    public async Task SteamCmdDownloader(string searchDirectory, string fileName)
    {
        var destinationPath = Path.Combine(searchDirectory, fileName);
        await LogAsync("Installing steamcmd..");
        try
        {
            using (var process = new Process
                   {
                       StartInfo = new ProcessStartInfo
                       {
                           FileName = "/bin/bash",
                           Arguments =
                               $"-c \"cd {searchDirectory} && curl -qL \"https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz\" | tar zxvf - && chmod +x steamcmd.sh\"",
                           RedirectStandardOutput = true,
                           RedirectStandardError = true,
                           UseShellExecute = false,
                           CreateNoWindow = true
                       }
                   })
            {
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        await LogAsync(e.Data);
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
            }

            await LogAsync($"steamcmd successfully extracted to {searchDirectory}");
        }
        catch (Exception ex)
        {
            await LogAsync($"Error: {ex.Message}");
        }
    }

    public async Task ModDownloader(string WorkshopId, string AppId)
    {
        var steamCmdPath = "/home/lagita/SteamWorkshop/steamcmd.sh"; // Path to steamcmd.sh
        var arguments = $"+login anonymous +workshop_download_item {AppId} {WorkshopId} +quit";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash", // For Linux
                    Arguments = $"-c \"{steamCmdPath} {arguments}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Subscribe to output events
            process.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    await LogAsync($"Output: {e.Data}");
                Success = true;
            };

            process.ErrorDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    await LogAsync($"Error: {e.Data}");
            };

            process.Start();

            // Begin reading output streams asynchronously
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            await LogAsync($"SteamCMD process exited with code {process.ExitCode}");
        }
        catch (Exception ex)
        {
            await LogAsync($"An error occurred: {ex.Message}");
            Success = false;
        }
    }
}