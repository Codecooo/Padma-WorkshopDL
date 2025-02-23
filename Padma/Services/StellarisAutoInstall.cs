using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Padma.Services;

public class StellarisAutoInstall
{
    public readonly string StellarisDocPath;

    public StellarisAutoInstall()
    {
        StellarisDocPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Paradox Interactive",
            "Stellaris", "mod"
        );
    }

    public event Func<string, Task>? LogAsync;

    /// <summary>
    ///     Just for testing whether or not we have permission to do all of this
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private async Task<bool> TryCreateDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            // Test write permissions by attempting to create a temporary file
            var testFile = Path.Combine(path, ".write_test");
            await File.WriteAllTextAsync(testFile, "Yay we got permission!");
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task RunStellarisAutoInstallMods(string modsDownloadPath, string workshopTitle)
    {
        try
        {
            if (string.IsNullOrEmpty(modsDownloadPath))
                throw new ArgumentException("Download path cannot be empty", nameof(modsDownloadPath));

            if (!await TryCreateDirectory(StellarisDocPath))
            {
                await LogAsync?.Invoke($"Error: No write permission for {StellarisDocPath}");
                return;
            }

            await LogAsync?.Invoke($"Installing mods to {StellarisDocPath}");
            await MoveStellarisMods(modsDownloadPath, workshopTitle);
        }
        catch (Exception ex)
        {
            await LogAsync?.Invoke($"Error during mod installation: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Automatically rename descriptor.mod to mod title and move it alongside the mod directory
    ///     to the Stellaris game path.
    /// </summary>
    /// <param name="downloadPath"></param>
    private async Task MoveStellarisMods(string downloadPath, string workshopTitle)
    {
        try
        {
            // Handle ZIP files with error handling
            var compressedFiles = Directory.GetFiles(downloadPath, "*.zip");
            foreach (var zipFile in compressedFiles)
                try
                {
                    using (var fs = File.Open(zipFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If we can open the file with ReadWrite access, we have proper permissions
                    }

                    ZipFile.ExtractToDirectory(zipFile, downloadPath, true);
                    File.Delete(zipFile);
                }
                catch (InvalidDataException ex) when (ex.Message.Contains("bzip2"))
                {
                    // If it's a bzip2 error, log it and continue - the content might already be extracted
                    await LogAsync?.Invoke(
                        $"Skipping bzip2 compressed file {zipFile} - content may already be extracted. If not install it manually");
                }
                catch (UnauthorizedAccessException)
                {
                    await LogAsync?.Invoke($"No permission to access {zipFile}");
                }
                catch (Exception ex)
                {
                    await LogAsync?.Invoke($"Error extracting {zipFile}: {ex.Message}");
                }

            // Find and process descriptor
            var descriptor = Directory.GetFiles(downloadPath, "descriptor.mod").FirstOrDefault();
            if (descriptor == null)
            {
                await LogAsync?.Invoke($"No descriptor.mod found in {downloadPath}");
                return;
            }

            List<string> descriptorLines;
            try
            {
                descriptorLines = (await File.ReadAllLinesAsync(descriptor)).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                await LogAsync?.Invoke($"No permission to read {descriptor}");
                return;
            }

            // get the value of mod title in the descriptor.mod
            var modNameRegex = new Regex(@"name=""(.*?)""", RegexOptions.IgnoreCase);
            var modTitle = descriptorLines
                .Select(l => modNameRegex.Match(l))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(modTitle))
            {
                await LogAsync?.Invoke("Could not find mod name in descriptor.mod, using workshop title");
                modTitle = workshopTitle;
                descriptorLines.Add($"name=\"{workshopTitle}\"");
            }

            // Update descriptor - use descriptorLines which now contains the new title if it was added
            var modDescriptor = descriptorLines.Where(line => !line.TrimStart().StartsWith("path=")).ToList();
            modDescriptor.Add($"path=\"{Path.Combine(StellarisDocPath, modTitle)}\"");

            // Ensure target paths are available
            var targetModPath = Path.Combine(StellarisDocPath, modTitle);
            var targetDescriptorPath = Path.Combine(StellarisDocPath, $"{modTitle}.mod");

            try
            {
                // Write the updated descriptor content
                await File.WriteAllLinesAsync(descriptor, modDescriptor);

                if (Directory.Exists(targetModPath))
                    Directory.Delete(targetModPath, true);

                // Move files with explicit file mode
                File.Move(descriptor, targetDescriptorPath, true);
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