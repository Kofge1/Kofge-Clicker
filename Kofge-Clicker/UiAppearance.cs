namespace KofgeClicker;

internal static class UiAppearance
{
    internal const string Classic = "Classic";
    internal const string Pixel = "Pixel";
    internal static string DefaultStyle => PixelUiPreview.IsPreviewBuild ? Pixel : Classic;
    internal static string ActiveStyle { get; private set; } = DefaultStyle;
    private static bool _initialized;

    internal static string Normalize(string? style)
        => string.Equals(style?.Trim(), Pixel, StringComparison.OrdinalIgnoreCase) ? Pixel : Classic;

    internal static void Initialize(string? style)
    {
        // Palette and font caches belong to one theme for the process lifetime.
        if (_initialized) return;
        ActiveStyle = Normalize(style ?? DefaultStyle);
        _initialized = true;
    }

    internal static string ReadSavedStyle(string localPath, string executablePath, string legacyPath)
    {
        var path = File.Exists(localPath) ? localPath : File.Exists(executablePath) ? executablePath : legacyPath;
        return Normalize(new IniFile(path).ReadString("Main", "UiStyle", DefaultStyle));
    }
}
