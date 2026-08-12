namespace KofgeClicker;

internal static class StartupLanguageResolver
{
    private const int PrimaryLanguageMask = 0x03FF;
    private const int RussianPrimaryLanguage = 0x19;

    internal static string Resolve(IniFile ini)
    {
        var savedLanguage = ini.ReadString("Main", "Language", string.Empty);
        if (!string.IsNullOrWhiteSpace(savedLanguage))
        {
            return LocalizationService.NormalizeLanguageCode(savedLanguage);
        }

        var detectedLanguage = DetectActiveKeyboardLanguage();
        ini.WriteString("Main", "Language", detectedLanguage);
        return detectedLanguage;
    }

    private static string DetectActiveKeyboardLanguage()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            var threadId = foregroundWindow != IntPtr.Zero
                ? NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _)
                : 0;
            var keyboardLayout = NativeMethods.GetKeyboardLayout(threadId);
            var languageId = unchecked((int)((long)keyboardLayout & 0xFFFF));
            if ((languageId & PrimaryLanguageMask) == RussianPrimaryLanguage)
            {
                return LocalizationService.RussianLanguageCode;
            }
        }
        catch
        {
        }

        return LocalizationService.DefaultLanguageCode;
    }
}
