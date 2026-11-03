using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal static class WindowsStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static string RunValueName => PixelUiPreview.IsPreviewBuild ? "Kofge-Clicker.PixelPreview" : "Kofge-Clicker";
    private const string LegacyRunValueName = "AutoClicker";
    private static string ShortcutName => RunValueName + ".lnk";
    private const string LegacyShortcutName = "AutoClicker.lnk";
    private static bool IncludeLegacyEntries => !PixelUiPreview.IsPreviewBuild;

    internal sealed record Operations(
        Action<string, string> CreateShortcut,
        Action<string?> DeleteFile,
        Action<string> WriteRunValue,
        Action DeleteRunValues);

    internal static string Sync(bool enabled, string executablePath)
    {
        // Off-screen UI tests must never register their runner with Windows.
        if (PixelUiPreview.IsSnapshotRendering) return "snapshot-isolated";
        var shortcutPath = GetShortcutPath(ShortcutName);
        var legacyShortcutPath = IncludeLegacyEntries ? GetShortcutPath(LegacyShortcutName) : null;
        return SyncCore(enabled, executablePath, shortcutPath, legacyShortcutPath,
            new Operations(CreateShortcut, DeleteFileIfPresent, WriteRunValue, DeleteRunValues));
    }

    internal static string SyncCore(bool enabled, string executablePath, string shortcutPath,
        string? legacyShortcutPath, Operations operations)
    {
        if (!enabled)
        {
            try
            {
                operations.DeleteFile(legacyShortcutPath);
                operations.DeleteFile(shortcutPath);
                operations.DeleteRunValues();
                return "disabled";
            }
            catch (Exception disableError)
            {
                return $"failed:disable/{disableError.GetType().Name}";
            }
        }

        try
        {
            operations.DeleteFile(legacyShortcutPath);
            operations.CreateShortcut(shortcutPath, executablePath);
            operations.DeleteRunValues();
            return "startup-folder";
        }
        catch (Exception shortcutError)
        {
            try
            {
                operations.DeleteFile(shortcutPath);
                operations.WriteRunValue(executablePath);
                return $"registry-fallback:{shortcutError.GetType().Name}";
            }
            catch (Exception registryError)
            {
                return $"failed:{shortcutError.GetType().Name}/{registryError.GetType().Name}";
            }
        }
    }

    private static string GetShortcutPath(string fileName)
    {
        var startupDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startupDirectory, fileName);
    }

    private static void CreateShortcut(string shortcutPath, string executablePath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows Script Host could not be created.");
            dynamic shellObject = shell;
            shortcut = shellObject.CreateShortcut(shortcutPath);
            dynamic shortcutObject = shortcut;
            shortcutObject.TargetPath = executablePath;
            shortcutObject.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
            shortcutObject.Description = "Start Kofge-Clicker with Windows";
            shortcutObject.IconLocation = $"{executablePath},0";
            shortcutObject.Save();
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void WriteRunValue(string executablePath)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath)
            ?? throw new InvalidOperationException("Windows startup registry key is unavailable.");
        runKey.SetValue(RunValueName, $"\"{executablePath}\"", RegistryValueKind.String);
        if (IncludeLegacyEntries) runKey.DeleteValue(LegacyRunValueName, false);
    }

    private static void DeleteRunValues()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        runKey?.DeleteValue(RunValueName, false);
        if (IncludeLegacyEntries) runKey?.DeleteValue(LegacyRunValueName, false);
    }

    private static void DeleteFileIfPresent(string? path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
