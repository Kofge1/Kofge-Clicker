using System.Collections;
using System.Drawing.Imaging;
using System.Reflection;
using System.Security.Cryptography;
using KofgeClicker;

internal static class UiAppearanceChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(MainForm).Assembly;
    private static readonly Type Appearance = App.GetType("KofgeClicker.UiAppearance", true)!;
    private static readonly Type Pixel = App.GetType("KofgeClicker.PixelUiPreview", true)!;

    internal static void PreferencesAndIsolation()
    {
        var normalize = Appearance.GetMethod("Normalize", Members)!;
        Assert((string)normalize.Invoke(null, [" pIxEl "])! == "Pixel", "Pixel preference normalization failed.");
        foreach (var invalid in new string?[] { null, "", "Unknown", "Classic" })
            Assert((string)normalize.Invoke(null, [invalid])! == "Classic", "Invalid/missing themes must fall back to Classic.");
        var active = (string)Appearance.GetProperty("ActiveStyle", Members)!.GetValue(null)!;
        Appearance.GetMethod("Initialize", Members)!.Invoke(null, [active == "Pixel" ? "Classic" : "Pixel"]);
        Assert((string)Appearance.GetProperty("ActiveStyle", Members)!.GetValue(null)! == active,
            "The active theme changes after palette initialization instead of waiting for restart.");
        var previewBuild = (bool)Pixel.GetProperty("IsPreviewBuild", Members)!.GetValue(null)!;
        var paths = App.GetType("KofgeClicker.AppPaths", true)!;
        var productionFolder = (string)paths.GetProperty("DataDirectory", Members)!.GetValue(null)!;
        Assert(Path.GetFileName(productionFolder) == (previewBuild ? "Kofge-Clicker.PixelPreview" : "Kofge-Clicker"),
            "Choosing Pixel switched the production data folder to preview data.");

        using var data = new TestData();
        var read = Appearance.GetMethod("ReadSavedStyle", Members)!;
        var portable = Path.Combine(data.Directory, "portable.ini");
        var legacy = Path.Combine(data.Directory, "legacy.ini");
        var missing = Path.Combine(data.Directory, "missing.ini");
        new IniFile(portable).WriteString("Main", "UiStyle", "Pixel");
        new IniFile(legacy).WriteString("Main", "UiStyle", "Classic");
        Assert((string)read.Invoke(null, [missing, portable, legacy])! == "Pixel", "Portable theme preference was ignored.");
        Assert((string)read.Invoke(null, [missing, missing, legacy])! == "Classic", "Legacy fallback lost its theme preference.");
        var ini = new IniFile(data.SettingsPath);
        ini.WriteString("Main", "UiStyle", active);
        ini.WriteString("Main", "CPS", "35");
        ini.WriteString("Profile_guard", "Name", "Keep profile");
        var macroDirectory = Path.Combine(data.Directory, "Macros");
        var storageType = App.GetType("KofgeClicker.MacroStorage", true)!;
        var storage = Activator.CreateInstance(storageType, Members, null, [macroDirectory], null)!;
        var macro = Activator.CreateInstance(App.GetType("KofgeClicker.MacroDefinition", true)!, true)!;
        macro.GetType().GetProperty("Name")!.SetValue(macro, "Keep macro");
        storageType.GetMethod("Save", Members)!.Invoke(storage, [macro]);
        var macroPath = System.IO.Directory.GetFiles(macroDirectory, "*.json").Single();
        var hash = SHA256.HashData(File.ReadAllBytes(macroPath));
        using (var main = new MainForm())
        {
            var selector = (PillDropdown)Field(main, "_cmbUiStyle");
            Assert(selector.SelectedIndex == (active == "Pixel" ? 1 : 0), "Theme selector does not reflect persisted preference.");
            var palette = UiTheme.AppBackground;
            var font = UiTheme.BodyFont;
            var save = typeof(MainForm).GetMethod("SaveUiStyleSelection", Members)!;
            var next = active == "Pixel" ? 0 : 1;
            Assert((bool)save.Invoke(main, [next])!, "Selecting another theme did not request a restart.");
            var selected = next == 1 ? "Pixel" : "Classic";
            Assert(ini.ReadString("Main", "UiStyle") == selected, "Theme selection was not saved to Main.");
            Assert(UiTheme.AppBackground == palette && ReferenceEquals(font, UiTheme.BodyFont), "A pending theme change partially modified the current UI.");
            typeof(MainForm).GetMethod("SaveSettings", Members)!.Invoke(main, [false, true]);
            Assert(ini.ReadString("Main", "UiStyle") == selected && ini.ReadString("Profile_guard", "Name") == "Keep profile"
                && ini.ReadInt("Main", "CPS") == 35, "Saving appearance changed unrelated settings.");
            Assert(SHA256.HashData(File.ReadAllBytes(macroPath)).SequenceEqual(hash), "Changing appearance rewrote a macro file.");
            var profileKeys = (string[])typeof(MainForm).GetField("ProfileDataKeys", Members)!.GetValue(null)!;
            Assert(!profileKeys.Contains("UiStyle"), "Appearance became profile-specific.");
            Assert(!(bool)save.Invoke(main, [next])! && !(bool)save.Invoke(main, [-1])!, "Repeated/invalid selection triggers a restart.");
            Assert(!(bool)save.Invoke(main, [active == "Pixel" ? 1 : 0])!, "Reverting to the active style still requests a restart.");
            var canRestart = typeof(MainForm).GetMethod("CanRestartForUiStyleChange", Members)!;
            Assert((bool)canRestart.Invoke(main, null)!, "Idle application cannot restart for a theme change.");
            foreach (var name in new[] { "_macroCountdownCts", "_macroPlaybackCountdownCts" })
            {
                using var countdown = new CancellationTokenSource();
                var field = typeof(MainForm).GetField(name, Members)!;
                field.SetValue(main, countdown);
                Assert(!(bool)canRestart.Invoke(main, null)!, "A countdown allows an unsafe theme restart.");
                field.SetValue(main, null);
            }
            var saving = typeof(MainForm).GetField("_macroSaveInProgress", Members)!;
            saving.SetValue(main, true);
            Assert(!(bool)canRestart.Invoke(main, null)!, "Saving a recording allows an unsafe theme restart.");
            saving.SetValue(main, false);
            foreach (var state in new[] { (Owner: "_macroRecorder", Flag: "_isRecording"), (Owner: "_macroPlayer", Flag: "_isPlaying") })
            {
                var owner = Field(main, state.Owner);
                var flag = owner.GetType().GetField(state.Flag, Members)!;
                flag.SetValue(owner, true);
                Assert(!(bool)canRestart.Invoke(main, null)!, "An active macro allows an unsafe theme restart.");
                flag.SetValue(owner, false);
            }
        }
        foreach (var language in new[] { "ru", "en" })
        {
            ini.WriteString("Main", "Language", language);
            using var main = new MainForm();
            var selector = (Control)Field(main, "_cmbUiStyle");
            var target = (Control)Field(main, "_lblTargetWindow");
            Assert(selector.Parent == target.Parent && selector.Top > target.Bottom
                && selector.Right <= selector.Parent!.ClientSize.Width - 20,
                "Appearance selection overlaps the target window settings.");
            foreach (var hint in Children(selector.Parent!).OfType<Label>().Where(label => label.Top == 276))
                Assert(UiTheme.MeasureText(hint.Text, hint.Font, new Size(hint.Width, int.MaxValue), TextFormatFlags.WordBreak).Height <= hint.Height,
                    "The restart hint does not fit its panel.");
            Snapshot(main, language + "-appearance-options");
            var items = (Array)typeof(MainForm).GetMethod("GetWhatsNewItems", Members)!.Invoke(null, null)!;
            Assert(items.Length == 3, "v1.1.0 release information is incomplete.");
            using var dialog = (Form)Activator.CreateInstance(App.GetType("KofgeClicker.WhatsNewDialog", true)!, Members, null, ["v1.1.0", items], null)!;
            foreach (var label in Children(dialog).OfType<Label>().Where(label => label.Height == 46))
                Assert(UiTheme.MeasureText(label.Text, label.Font, new Size(label.Width, int.MaxValue), TextFormatFlags.WordBreak).Height <= label.Height,
                    "The v1.1.0 update description is clipped.");
            Snapshot(dialog, language + "-appearance-whats-new");
        }
        Assert(App.GetName().Version == new Version(1, 1, 0, 0), "Assembly version was not updated to 1.1.0.");
    }

    internal static void SharedPalette()
    {
        Assert(UiTheme.AppBackground == Color.FromArgb(9, 18, 29)
            && UiTheme.CardOuter == Color.FromArgb(17, 38, 57)
            && UiTheme.CardInner == Color.FromArgb(13, 47, 68)
            && UiTheme.Surface == Color.FromArgb(8, 30, 44)
            && UiTheme.Accent == Color.FromArgb(0, 167, 249), "The shared palette does not match the approved pixel colors.");
        foreach (var value in new[] { Color.FromArgb(245, 190, 110), Color.FromArgb(242, 68, 68), Color.FromArgb(91, 202, 120) })
            Assert(UiTheme.ColorFromArgb(value.A, value.R, value.G, value.B) == value, "The palette replaced a warning/success color.");
        Assert(UiTheme.ColorFromArgb(73, 59, 67, 106).A == 73, "Palette mapping changed opacity.");
        using var path = UiTheme.CreateRoundedRectPath(new RectangleF(0, 0, 100, 40), 12);
        Assert(path.PathTypes.Any(type => (type & 7) == 3) == !PixelAllSurfaceChecks.Enabled,
            "The palette changed the selected style's rounded/pixel geometry.");
        Assert((UiTheme.BodyFont.FontFamily.Name == "Monocraft") == PixelAllSurfaceChecks.Enabled,
            "Sharing colors changed the selected style's font.");
        using var data = new TestData();
        new IniFile(data.SettingsPath).WriteString("Main", "UiStyle", PixelAllSurfaceChecks.Enabled ? "Pixel" : "Classic");
        using var main = new MainForm();
        foreach (var toggle in Children(main).OfType<ToggleSwitchCheckBox>())
            Assert(toggle.CheckedFillColor == UiTheme.Accent && toggle.CheckedBorderColor == UiTheme.AccentBorder
                && toggle.UncheckedFillColor == Color.FromArgb(20, 53, 73) && toggle.UncheckedBorderColor == UiTheme.Border,
                "A toggle still uses the old classic palette.");
        var menu = (ContextMenuStrip)Field(main, "_trayMenu");
        var font = menu.Font;
        var renderer = menu.Renderer;
        var themeType = PixelAllSurfaceChecks.Enabled ? Pixel : App.GetType("KofgeClicker.ClassicMenuTheme", true)!;
        themeType.GetMethod(PixelAllSurfaceChecks.Enabled ? "ApplyMenu" : "Apply", Members)!.Invoke(null, [menu]);
        Assert(ReferenceEquals(font, menu.Font) && ReferenceEquals(renderer, menu.Renderer),
            "Sharing menu colors replaced the menu font or renderer on repeated application.");
        var profiles = menu.Items.OfType<ToolStripMenuItem>().Single(item => item.HasDropDownItems);
        Assert(menu.BackColor == UiTheme.Surface && profiles.DropDown.BackColor == UiTheme.Surface,
            "The tray/profile menus retained the Windows palette.");
        Assert((profiles.DropDown.Font.FontFamily.Name == "Monocraft") == PixelAllSurfaceChecks.Enabled,
            "A nested menu switched to the other style's font.");
        for (var page = 0; page < 7; page++) Snapshot(main, "shared-palette-tab-" + page, page);
        var popup = Field(Field(main, "_hoverTooltips"), "_popup");
        Assert(((Form)popup).BackColor == UiTheme.CardOuter, "The hover popup retained its old background.");
        var items = (Array)typeof(MainForm).GetMethod("GetWhatsNewItems", Members)!.Invoke(null, null)!;
        using var dialog = (Form)Activator.CreateInstance(App.GetType("KofgeClicker.WhatsNewDialog", true)!, Members, null, ["v1.1.0", items], null)!;
        Snapshot(dialog, "shared-palette-whats-new");
    }

    private static void Snapshot(Form form, string name, int page = 6)
    {
        var directory = Environment.GetEnvironmentVariable("KOFGE_APPEARANCE_SNAPSHOTS");
        if (string.IsNullOrEmpty(directory)) return;
        if (form is MainForm main)
        {
            ((AtomicPageHost)Field(main, "_pageHost")).SelectedIndex = page;
            typeof(MainForm).GetMethod("UpdateTabHeaderVisuals", Members)!.Invoke(main, null);
        }
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.ShowInTaskbar = false;
        form.Opacity = 0;
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        System.IO.Directory.CreateDirectory(directory);
        bitmap.Save(Path.Combine(directory, name + ".png"), ImageFormat.Png);
    }

    private static IEnumerable<Control> Children(Control root)
    {
        foreach (Control control in root.Controls)
        {
            yield return control;
            foreach (var child in Children(control)) yield return child;
        }
    }
    private static object Field(object owner, string name) => owner.GetType().GetField(name, Members)!.GetValue(owner)!;
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private sealed class TestData : IDisposable
    {
        internal string Directory { get; } = Path.Combine(Path.GetTempPath(), "Kofge-Appearance-" + Guid.NewGuid().ToString("N"));
        internal string SettingsPath => Path.Combine(Directory, "settings.ini");
        public TestData()
        {
            Pixel.GetProperty("SnapshotDataDirectory", Members)!.SetValue(null, Directory);
            System.IO.Directory.CreateDirectory(Directory);
            new IniFile(SettingsPath).WriteString("Main", "Language", "ru");
        }
        public void Dispose()
        {
            Pixel.GetProperty("SnapshotDataDirectory", Members)!.SetValue(null, null);
            var full = Path.GetFullPath(Directory);
            if (full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                try { System.IO.Directory.Delete(full, true); } catch (IOException) { }
        }
    }
}
