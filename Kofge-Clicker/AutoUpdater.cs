using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace KofgeClicker;

internal static class AutoUpdater
{
    private const string ApplyUpdateArgument = "--apply-update";
    private const string ElevatedUpdateArgument = "--elevated-update";
    private const int MinimumExecutableSize = 1024 * 1024;
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(45);

    internal static bool TryHandleInstallerMode(string[] args, Action<string> log)
    {
        if (args.Length < 3 || !string.Equals(args[0], ApplyUpdateArgument, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var targetPath = Path.GetFullPath(args[1]);
        if (!string.Equals(Path.GetFileName(targetPath), UpdateChecker.ReleaseAssetName, StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(args[2], out var parentProcessId)
            || parentProcessId <= 0)
        {
            log("Updater rejected invalid command-line arguments");
            return true;
        }

        var elevatedAttempt = args.Any(argument =>
            string.Equals(argument, ElevatedUpdateArgument, StringComparison.OrdinalIgnoreCase));
        try
        {
            ApplyUpdate(targetPath, parentProcessId, log);
        }
        catch (UnauthorizedAccessException ex) when (!elevatedAttempt)
        {
            log($"Updater needs elevation: {ex.Message}");
            RelaunchInstallerElevated(targetPath, parentProcessId, log);
        }
        catch (Exception ex)
        {
            log($"Updater failed: {ex}");
        }

        return true;
    }

    internal static async Task<string?> StageUpdateAsync(
        UpdateInfo update,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(UpdateDirectory);
        var safeTag = SanitizeTag(update.TagName);
        var stagedPath = Path.Combine(UpdateDirectory, $"Kofge-Clicker-{safeTag}.update.exe");
        if (File.Exists(stagedPath)
            && await ValidateExecutableAsync(stagedPath, update, cancellationToken).ConfigureAwait(false))
        {
            return stagedPath;
        }

        TryDeleteFile(stagedPath);
        var downloadPath = stagedPath + ".download";
        TryDeleteFile(downloadPath);

        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler) { Timeout = DownloadTimeout };
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Kofge-Clicker", UpdateChecker.NormalizeVersion(AppVersion.Display)));

            using var response = await client.GetAsync(
                update.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = new FileStream(
                downloadPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(destination, 1024 * 1024, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!await ValidateExecutableAsync(downloadPath, update, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException("The downloaded update failed validation.");
            }

            File.Move(downloadPath, stagedPath, true);
            Log($"Update staged: {update.TagName}, path={stagedPath}");
            return stagedPath;
        }
        catch
        {
            TryDeleteFile(downloadPath);
            throw;
        }
    }

    internal static bool StartInstaller(string stagedPath, string targetPath, int parentProcessId)
    {
        if (!File.Exists(stagedPath)
            || string.IsNullOrWhiteSpace(targetPath)
            || !IsStrictlyNewerExecutable(stagedPath, targetPath))
        {
            return false;
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        var startInfo = new ProcessStartInfo(stagedPath)
        {
            UseShellExecute = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(targetDirectory)
                ? AppContext.BaseDirectory
                : targetDirectory
        };
        startInfo.ArgumentList.Add(ApplyUpdateArgument);
        startInfo.ArgumentList.Add(Path.GetFullPath(targetPath));
        startInfo.ArgumentList.Add(parentProcessId.ToString());
        return Process.Start(startInfo) is not null;
    }

    internal static void CleanupStaleFiles()
    {
        if (!Directory.Exists(UpdateDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(UpdateDirectory))
        {
            try
            {
                if (file.EndsWith(".download", StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteFile(file);
                    continue;
                }

                if (!file.EndsWith(".update.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var stagedVersion = FileVersionInfo.GetVersionInfo(file).FileVersion;
                if (string.IsNullOrWhiteSpace(stagedVersion)
                    || !UpdateChecker.IsNewerVersion(stagedVersion, AppVersion.Display))
                {
                    TryDeleteFile(file);
                }
            }
            catch
            {
                TryDeleteFile(file);
            }
        }
    }

    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                AppPaths.StartupLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Update: {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static string UpdateDirectory => Path.Combine(AppPaths.DataDirectory, "Updates");

    private static async Task<bool> ValidateExecutableAsync(
        string path,
        UpdateInfo update,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists
                || file.Length < MinimumExecutableSize
                || update.AssetSize is > 0 && file.Length != update.AssetSize.Value)
            {
                return false;
            }

            await using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var header = new byte[2];
                if (await stream.ReadAsync(header, cancellationToken).ConfigureAwait(false) != header.Length
                    || header[0] != (byte)'M'
                    || header[1] != (byte)'Z')
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(update.Sha256Digest))
                {
                    stream.Position = 0;
                    var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (!string.Equals(
                            Convert.ToHexString(hash),
                            update.Sha256Digest,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
            return !string.IsNullOrWhiteSpace(fileVersion)
                && UpdateChecker.VersionsEqual(update.TagName, fileVersion);
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyUpdate(string targetPath, int parentProcessId, Action<string> log)
    {
        WaitForParentExit(parentProcessId, log);

        var updaterPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(updaterPath) || !File.Exists(updaterPath))
        {
            throw new FileNotFoundException("The staged updater executable is unavailable.");
        }

        if (!IsStrictlyNewerExecutable(updaterPath, targetPath))
        {
            var updaterVersion = FileVersionInfo.GetVersionInfo(updaterPath).FileVersion ?? "unknown";
            var installedVersion = FileVersionInfo.GetVersionInfo(targetPath).FileVersion ?? "unknown";
            throw new InvalidDataException(
                $"Downgrade or same-version installation blocked: candidate={updaterVersion}, installed={installedVersion}.");
        }

        var targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException("The application directory is unavailable.");
        Directory.CreateDirectory(targetDirectory);

        var replacementPath = targetPath + ".update-new";
        var backupPath = targetPath + ".update-backup";
        TryDeleteFile(replacementPath);
        TryDeleteFile(backupPath);
        File.Copy(updaterPath, replacementPath, true);

        var replaced = false;
        try
        {
            if (File.Exists(targetPath))
            {
                File.Replace(replacementPath, targetPath, backupPath, true);
            }
            else
            {
                File.Move(replacementPath, targetPath);
            }

            replaced = true;
            log($"Updater replaced target: {targetPath}");
            var process = Process.Start(new ProcessStartInfo(targetPath)
            {
                UseShellExecute = true,
                WorkingDirectory = targetDirectory
            });
            if (process is null)
            {
                throw new InvalidOperationException("The updated application could not be started.");
            }

            TryDeleteFile(backupPath);
            log("Updater launched the updated application");
        }
        catch
        {
            if (replaced && File.Exists(backupPath))
            {
                try
                {
                    File.Copy(backupPath, targetPath, true);
                    log("Updater restored the previous executable after a failure");
                }
                catch (Exception rollbackError)
                {
                    log($"Updater rollback failed: {rollbackError.Message}");
                }
            }

            throw;
        }
        finally
        {
            TryDeleteFile(replacementPath);
        }
    }

    private static void WaitForParentExit(int parentProcessId, Action<string> log)
    {
        try
        {
            using var parent = Process.GetProcessById(parentProcessId);
            if (!parent.HasExited && !parent.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
            {
                throw new TimeoutException("The running application did not close in time.");
            }
        }
        catch (ArgumentException)
        {
            log("Updater parent process had already exited");
        }
    }

    private static void RelaunchInstallerElevated(string targetPath, int parentProcessId, Action<string> log)
    {
        var updaterPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(updaterPath))
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo(updaterPath)
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
            };
            startInfo.ArgumentList.Add(ApplyUpdateArgument);
            startInfo.ArgumentList.Add(targetPath);
            startInfo.ArgumentList.Add(parentProcessId.ToString());
            startInfo.ArgumentList.Add(ElevatedUpdateArgument);
            Process.Start(startInfo);
            log("Updater elevation requested");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            log("Updater elevation was canceled by the user");
        }
        catch (Exception ex)
        {
            log($"Updater elevation failed: {ex.Message}");
        }
    }

    private static string SanitizeTag(string tag)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(tag.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "update" : safe;
    }

    private static bool IsStrictlyNewerExecutable(string candidatePath, string installedPath)
    {
        try
        {
            if (!File.Exists(candidatePath) || !File.Exists(installedPath))
            {
                return false;
            }

            var candidateVersion = FileVersionInfo.GetVersionInfo(candidatePath).FileVersion;
            var installedVersion = FileVersionInfo.GetVersionInfo(installedPath).FileVersion;
            return !string.IsNullOrWhiteSpace(candidateVersion)
                && !string.IsNullOrWhiteSpace(installedVersion)
                && UpdateChecker.IsNewerVersion(candidateVersion, installedVersion);
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
