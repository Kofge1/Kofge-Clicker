using System.Collections;
using System.Diagnostics;
using System.Reflection;
using KofgeClicker;

internal static class UiPerformanceOptionChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(MainForm).Assembly;

    internal static void PersistenceAndVisibility()
    {
        const string key = "ShowUiPerformance";
        var path = Path.Combine(Path.GetTempPath(), $"Kofge-UiPreference-{Guid.NewGuid():N}.ini");
        try
        {
            var settings = new AppSettings();
            Assert(!settings.ShowUiPerformance, "UI diagnostics must be disabled by default.");
            var ini = new IniFile(path);
            Assert(!ini.ReadBool("Main", key, false), "An old INI without the preference enabled diagnostics.");
            ini.UpdateSections(
            [
                ("Main", new List<KeyValuePair<string, string>> { new("Language", "ru") }),
                ("Profile_Default", new List<KeyValuePair<string, string>> { new("CPS", "40") })
            ], flushToDisk: true);
            using var window = new TestWindow();
            using var button = new AccentButton { Bounds = new Rectangle(0, 0, 130, 40), AnimateInteractions = true };
            using var badge = (Control)Activator.CreateInstance(App.GetType("KofgeClicker.UiFrameRateBadge")!, Members, null, null, null)!;
            badge.Bounds = new Rectangle(140, 0, 118, 40);
            badge.Visible = settings.ShowUiPerformance;
            window.Controls.Add(button);
            window.Controls.Add(badge);
            window.Show();
            Application.DoEvents();
            var timer = (System.Windows.Forms.Timer)badge.GetType().GetField("_refreshTimer", Members)!.GetValue(badge)!;
            Assert(!badge.Visible && !timer.Enabled, "Hidden diagnostics started its refresh timer.");
            button.GetType().GetMethod("OnMouseEnter", Members)!.Invoke(button, [EventArgs.Empty]);
            Assert(!timer.Enabled, "An animation started the hidden diagnostics timer.");
            foreach (var show in new[] { true, false, true, false })
            {
                settings.ShowUiPerformance = show;
                badge.Visible = settings.ShowUiPerformance;
                ini.UpdateSections(
                [
                    ("Main", new List<KeyValuePair<string, string>> { new(key, show ? "1" : "0") })
                ], flushToDisk: true);
                var reloaded = new AppSettings { ShowUiPerformance = new IniFile(path).ReadBool("Main", key, false) };
                Assert(reloaded.ShowUiPerformance == show && badge.Visible == show,
                    "UI diagnostics visibility or persistence disagrees with the option.");
                Assert(show || !timer.Enabled, "Hiding diagnostics left its refresh timer running.");
                Assert(ini.ReadString("Profile_Default", "CPS", "") == "40"
                    && ini.ReadString("Main", "Language", "") == "ru",
                    "The diagnostics preference changed another setting.");
            }
            ini.UpdateSections(
            [
                ("Profile_Other", new List<KeyValuePair<string, string>> { new("CPS", "15") })
            ], flushToDisk: true);
            Assert(!new IniFile(path).ReadBool("Main", key, false), "A profile update changed the global UI preference.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    internal static void LocalizedLayoutAndVersion()
    {
        var localization = App.GetType("KofgeClicker.LocalizationService")!;
        foreach (var language in new[] { "English", "Russian" })
        {
            var strings = (IDictionary)localization.GetField(language, Members)!.GetValue(null)!;
            var text = (string)strings["Options.ShowUiPerformance"]!;
            var tooltip = (string)strings["Tooltips.OptionUiPerformance"]!;
            Assert(!string.IsNullOrWhiteSpace(text) && tooltip.Contains("FPS"), "Missing diagnostics localization.");
            using var font = UiTheme.CreateFont("Segoe UI", 14f);
            var wrapped = (string)typeof(MainForm).GetMethod("WrapLabelText", Members)!
                .Invoke(null, [text, 300, font])!;
            var lines = wrapped.Split('\n');
            foreach (var line in lines)
            {
                var size = TextRenderer.MeasureText(line, font, Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                Assert(size.Width <= 300, "The UI diagnostics option label is clipped.");
            }
            var height = Math.Max(28, lines.Length > 1
                ? Math.Max(42, font.Height + (lines.Length - 1) * (font.Height - 3) + 2) : 24);
            Assert(278 + 21 - height / 2 + height <= 348, "The UI option extends beyond its card.");
            using var titleFont = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold);
            using var bodyFont = UiTheme.CreateFont("Segoe UI", 11.5f);
            foreach (var entry in new[] { "SmoothUi", "FasterUi", "UiDiagnostics" })
            {
                var title = (string)strings[$"WhatsNew.{entry}Title"]!;
                var description = (string)strings[$"WhatsNew.{entry}Text"]!;
                Assert(!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(description),
                    "Missing localized release notes.");
                var titleSize = TextRenderer.MeasureText(title, titleFont, new Size(556, int.MaxValue), TextFormatFlags.SingleLine);
                var bodySize = TextRenderer.MeasureText(description, bodyFont, new Size(556, int.MaxValue), TextFormatFlags.WordBreak);
                Assert(titleSize.Width <= 556 && titleSize.Height <= 24 && bodySize.Height <= 46,
                    $"Release notes text is clipped: {language}/{entry}, title={titleSize}, body={bodySize}.");
            }
        }
        var display = (string)App.GetType("KofgeClicker.AppVersion")!.GetField("Display", Members)!.GetRawConstantValue()!;
        var version = Version.Parse(display.TrimStart('v', 'V'));
        var expected = new Version(version.Major, version.Minor, version.Build, 0);
        Assert(App.GetName().Version == expected, "Display and assembly versions differ.");
        Assert(Version.Parse(FileVersionInfo.GetVersionInfo(App.Location).FileVersion!) == expected,
            "Display and file versions differ.");
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private sealed class TestWindow : Form
    {
        internal TestWindow()
        {
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(100, 120);
            ClientSize = new Size(270, 70);
        }
        protected override bool ShowWithoutActivation => true;
    }
}
