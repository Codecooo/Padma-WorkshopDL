using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace Padma.Services;

public class StellarisAutoInstall
{
    private readonly string _stellarisDocPath;
    public event Func<string, Task>? LogAsync;

    public StellarisAutoInstall()
    {
        _stellarisDocPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/share/Paradox Interactive/Stellaris/mod"
        );
    }

    private async Task<bool> TryCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            // Test write permissions by attempting to create a temporary file
            var testFile = Path.Combine(path, ".write_test");
            await File.WriteAllTextAsync(testFile, string.Empty);
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task RunStellarisAutoInstallMods(string modsDownloadPath)
    {
        try
        {
            if (string.IsNullOrEmpty(modsDownloadPath))
                throw new ArgumentException("Download path cannot be empty", nameof(modsDownloadPath));

            if (!await TryCreateDirectory(_stellarisDocPath))
            {
                await LogAsync?.Invoke($"Error: No write permission for {_stellarisDocPath}");
                return;
            }

            await LogAsync?.Invoke($"Installing mods to {_stellarisDocPath}");
            await MoveStellarisMods(modsDownloadPath);
        }
        catch (Exception ex)
        {
            await LogAsync?.Invoke($"Error during mod installation: {ex.Message}");
            throw;
        }
    }

    private async Task MoveStellarisMods(string downloadPath)
    {
        try
        {
            // Handle ZIP files with error handling
            var compressedFiles = Directory.GetFiles(downloadPath, "*.zip");
            foreach (var zipFile in compressedFiles)
            {
                try
                {
                    using (FileStream fs = File.Open(zipFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we can open the file with ReadWrite access, we have proper permissions
                    }
                    ZipFile.ExtractToDirectory(zipFile, downloadPath, true);
                    File.Delete(zipFile);
                }
                catch (UnauthorizedAccessException)
                {
                    await LogAsync?.Invoke($"No permission to access {zipFile}");
                    continue;
                }
                catch (Exception ex)
                {
                    await LogAsync?.Invoke($"Error extracting {zipFile}: {ex.Message}");
                    continue;
                }
            }

            // Find and process descriptor
            var descriptor = Directory.GetFiles(downloadPath, "descriptor.mod").FirstOrDefault();
            if (descriptor == null)
            {
                await LogAsync?.Invoke($"No descriptor.mod found in {downloadPath}");
                return;
            }

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(descriptor);
            }
            catch (UnauthorizedAccessException)
            {
                await LogAsync?.Invoke($"No permission to read {descriptor}");
                return;
            }

            var modNameRegex = new Regex(@"name=""(.*?)""", RegexOptions.IgnoreCase);
            var modTitle = lines
                .Select(l => modNameRegex.Match(l))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(modTitle))
            {
                await LogAsync?.Invoke("Could not find mod name in descriptor.mod");
                return;
            }

            // Update descriptor
            var modDescriptor = lines.Where(line => !line.TrimStart().StartsWith("path=")).ToList();
            modDescriptor.Add($"path=\"{Path.Combine(_stellarisDocPath, modTitle)}\"");

            // Ensure target paths are available
            var targetModPath = Path.Combine(_stellarisDocPath, modTitle);
            var targetDescriptorPath = Path.Combine(_stellarisDocPath, $"{modTitle}.mod");

            try
            {
                // Clean up existing files if necessary
                if (Directory.Exists(targetModPath))
                {
                    Directory.Delete(targetModPath, true);
                }

                // Move files with explicit file mode
                await File.WriteAllLinesAsync(targetDescriptorPath, modDescriptor);
                Directory.Move(downloadPath, targetModPath);

                await LogAsync?.Invoke($"Successfully installed mod: {modTitle}");
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogAsync?.Invoke($"Permission denied while installing mod: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            await LogAsync?.Invoke($"Error moving mod files: {ex.Message}");
            throw;
        }
    }
}