using System.Reflection;
using KofgeClicker;

internal static class StartupRegistrationChecks
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Registration()
    {
        var type = typeof(MainForm).Assembly.GetType("KofgeClicker.WindowsStartupRegistration", true)!;
        var previewBuild = (bool)typeof(MainForm).Assembly.GetType("KofgeClicker.PixelUiPreview", true)!
            .GetProperty("IsPreviewBuild", Members)!.GetValue(null)!;
        var name = (string)type.GetProperty("RunValueName", Members)!.GetValue(null)!;
        var shortcutName = (string)type.GetProperty("ShortcutName", Members)!.GetValue(null)!;
        var legacy = (bool)type.GetProperty("IncludeLegacyEntries", Members)!.GetValue(null)!;
        Assert(name == (previewBuild ? "Kofge-Clicker.PixelPreview" : "Kofge-Clicker"),
            "The preview and stable build share a Windows startup registration name.");
        Assert(shortcutName == name + ".lnk" && legacy == !previewBuild,
            "The preview can clean up stable/legacy startup entries.");

        var events = new List<string>();
        var shortcutFails = false;
        var registryFails = false;
        var disableFails = false;
        Action<string, string> create = (path, exe) =>
        {
            events.Add("create:" + path + ":" + exe);
            if (shortcutFails) throw new IOException("Simulated shortcut failure.");
        };
        Action<string?> delete = path =>
        {
            if (path is null) return;
            events.Add("delete:" + path);
            if (disableFails) throw new IOException("Simulated deletion failure.");
        };
        Action<string> write = exe =>
        {
            events.Add("registry:" + name + ":" + exe);
            if (registryFails) throw new UnauthorizedAccessException("Simulated registry failure.");
        };
        Action remove = () => events.Add("remove-registry:" + name);
        var operations = Activator.CreateInstance(type.GetNestedType("Operations", Members)!,
            Members, null, [create, delete, write, remove], null)!;
        var core = type.GetMethod("SyncCore", Members)!;
        string Sync(bool enabled)
        {
            events.Clear();
            return (string)core.Invoke(null, [enabled, "preview.exe", shortcutName, legacy ? "AutoClicker.lnk" : null, operations])!;
        }

        Assert(Sync(true) == "startup-folder" && events.Contains("create:" + shortcutName + ":preview.exe")
            && events.Last() == "remove-registry:" + name, "Enabling startup did not create the requested shortcut.");
        Assert(events.Contains("delete:AutoClicker.lnk") == legacy, "Legacy cleanup uses the wrong build scope.");
        Assert(Sync(false) == "disabled" && events.Contains("delete:" + shortcutName)
            && !events.Any(e => e.StartsWith("create:")), "Disabling startup recreates its entry.");
        shortcutFails = true;
        Assert(Sync(true) == "registry-fallback:IOException" && events.Contains("registry:" + name + ":preview.exe"),
            "The startup registry fallback no longer works.");
        registryFails = true;
        Assert(Sync(true) == "failed:IOException/UnauthorizedAccessException", "A failed registration reports success.");
        disableFails = true;
        Assert(Sync(false) == "failed:disable/IOException", "A failed removal reports success.");
        if (PixelAllSurfaceChecks.Enabled) PixelAllSurfaceChecks.StartupOptions();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
