using System.Collections;
using System.Drawing.Imaging;
using System.Linq.Expressions;
using System.Reflection;
using KofgeClicker;

internal static class PixelAllSurfaceChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(MainForm).Assembly;
    private static readonly Type Preview = App.GetType("KofgeClicker.PixelUiPreview", true)!;
    internal static bool Enabled => (bool)Preview.GetProperty("Enabled", Members)!.GetValue(null)!;

    internal static void RemovedPerformanceSetting()
    {
        Assert(App.GetType("KofgeClicker.UiFrameMetrics") is null && App.GetType("KofgeClicker.UiFrameRateBadge") is null,
            "Unused UI performance classes remain in the application.");
        Assert(typeof(AppSettings).GetProperty("ShowUiPerformance") is null, "The removed setting remains in AppSettings.");
        Assert(App.GetType("KofgeClicker.ButtonAnimationClock")!.GetProperty("Metrics", Members) is null,
            "The animation clock still samples UI performance.");
    }

    internal static void StatusOffset()
    {
        using var label = (Label)New("SurfaceLabel");
        label.Size = new Size(781, 64);
        label.Text = "Кликер выключен\nDefault · F2 → LMB · 41 CPS";
        label.Font = (Font)Preview.GetMethod("Font", Members)!.Invoke(null, [16f])!;
        label.ForeColor = Color.White;
        label.BackColor = Color.Black;
        label.TextAlign = ContentAlignment.MiddleCenter;
        Preview.GetMethod("MarkUniformText", Members)!.Invoke(null, [label]);
        using var before = new Bitmap(label.Width, label.Height);
        using var after = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(before, label.ClientRectangle);
        label.GetType().GetProperty("PixelTextOffset", Members)!.SetValue(label, new Point(0, 2));
        label.DrawToBitmap(after, label.ClientRectangle);
        for (var y = 2; y < after.Height; y++)
            for (var x = 0; x < after.Width; x++)
                Assert(before.GetPixel(x, y - 2) == after.GetPixel(x, y), "The status text did not move exactly 2px down.");
    }

    internal static void DialogSurfaces()
    {
        using var data = new SnapshotData();
        foreach (var language in new[] { "ru", "en" })
        {
            SetLanguage(language);
            using var prompt = (Form)New("PromptDialog", "Pixel preview", "Имя записи / Recording name", "Default");
            using var message = (Form)New("ThemedMessageDialog", "Pixel preview", "Проверка сообщения. Test message.\nПродолжить / Continue?", true);
            using var help = (Form)New("MacroBackgroundHelpDialog");
            using var review = (Form)New("ReviewPromptDialog");
            var itemType = App.GetType("KofgeClicker.WhatsNewItem", true)!;
            var items = Array.CreateInstance(itemType, 2);
            items.SetValue(Activator.CreateInstance(itemType, "Pixel UI", "Единый пиксельный стиль / Shared pixel style"), 0);
            items.SetValue(Activator.CreateInstance(itemType, "Проверка", "Текст, рамки, поля, кнопки и подсказки."), 1);
            using var whatsNew = (Form)New("WhatsNewDialog", "v1.1.0", items);
            using var tooltip = (Form)New("HoverTooltipWindow");
            Call(tooltip, "SetMessage", "Проверка всплывающей подсказки: русский текст и English text.");
            using var save = (Form)New("SaveConfirmationToastWindow");
            Call(save, "SetMessage", "Настройки сохранены");
            using var toast = (Form)New("ThemedNotificationToastWindow");
            Call(toast, "SetMessage", "Запись начнётся через 3 секунды", "3");
            foreach (var form in new[] { prompt, message, help, review, whatsNew, tooltip, save, toast })
                Capture(form, language + "-" + form.GetType().Name);
        }
    }

    internal static void TourPages()
    {
        using var data = new SnapshotData();
        foreach (var language in new[] { "ru", "en" })
        {
            SetLanguage(language);
            using var tour = (Form)New("FirstRunTourDialog", (Action<int>)(_ => { }), (Action<string>)(_ => { }),
                (Func<string, string>)(_ => "F2"), (Action<string>)(_ => { }), (Func<string>)(() => "hold"),
                (Func<string, bool>)(_ => false), (Action<string, bool>)((_, _) => { }));
            for (var page = 0; page <= 7; page++)
            {
                Call(tour, "ShowStep", page);
                Capture(tour, language + "-tour-" + page);
            }
        }
    }

    internal static void MainTabs()
    {
        foreach (var language in new[] { "ru", "en" })
        {
            using var data = new SnapshotData();
            var directory = (string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!;
            new IniFile(Path.Combine(directory, "settings.ini")).WriteString("Main", "Language", language);
            using var main = new MainForm();
            var host = (AtomicPageHost)Field(main, "_pageHost");
            for (var page = 0; page < host.PageCount; page++)
            {
                host.SelectedIndex = page;
                Call(main, "UpdateTabHeaderVisuals");
                Capture(main, (language == "ru" ? string.Empty : "en-") + "main-tab-" + page);
            }
            Assert(((Point)((Label)Field(main, "_lblStatus")).GetType().GetProperty("PixelTextOffset", Members)!
                .GetValue(Field(main, "_lblStatus"))!).Y == 2, "The main status lost its 2px offset.");
            Assert(typeof(MainForm).GetField("_frameRateBadge", Members) is null, "The main window retained its FPS badge.");
            var header = (Control)Field(main, "_tabHeader");
            Assert(header.Parent!.Controls.GetChildIndex(header) < header.Parent.Controls.GetChildIndex(host),
                "The page host covers the tab buttons.");
            foreach (Control button in header.Controls)
                Assert(header.ClientRectangle.Contains(button.Bounds), "A tab button is clipped by its header.");
        }
    }

    internal static void CardTitleAlignment()
    {
        foreach (var language in new[] { "ru", "en" })
        {
            using var data = new SnapshotData();
            var directory = (string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!;
            new IniFile(Path.Combine(directory, "settings.ini")).WriteString("Main", "Language", language);
            using var main = new MainForm();
            var titles = Descendants(main).OfType<Label>().Where(label => label.Name == "CardTitle").ToArray();
            Assert(titles.Length == 8, "The title check must cover all seven tabs, including both option cards.");
            foreach (var title in titles)
            {
                Assert(title.TextAlign == ContentAlignment.MiddleLeft && title.Left == 20 && title.Top == 10
                    && title.Height == 32 && title.Right <= title.Parent!.Width - 20,
                    "The title layout changed instead of centering its text.");
                var divider = title.Parent!.Controls.OfType<Panel>().Single(panel => panel.Top == 52 && panel.Height == 1);
                using var bitmap = new Bitmap(title.Width, title.Height);
                title.DrawToBitmap(bitmap, title.ClientRectangle);
                var top = bitmap.Height;
                var bottom = -1;
                for (var y = 0; y < bitmap.Height; y++)
                    for (var x = 0; x < bitmap.Width; x++)
                        if (bitmap.GetPixel(x, y).ToArgb() == title.ForeColor.ToArgb())
                        {
                            top = Math.Min(top, y);
                            bottom = Math.Max(bottom, y);
                        }
                Assert(bottom >= top, "The title did not render: " + title.Text);
                var upperGap = title.Top + top;
                var lowerGap = divider.Top - 1 - (title.Top + bottom);
                Assert(Math.Abs(upperGap - lowerGap) <= 1,
                    $"The title is not centered between the card borders: {title.Text}, gaps={upperGap}/{lowerGap}.");
            }
        }
    }

    internal static void SmallLayoutPolish()
    {
        foreach (var language in new[] { "ru", "en" })
        {
            using var data = new SnapshotData();
            var directory = (string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!;
            new IniFile(Path.Combine(directory, "settings.ini")).WriteString("Main", "Language", language);
            using var main = new MainForm();
            var help = (Label)Field(main, "_lblPatternHelp");
            var rate = (Control)Field(main, "_rbRateLocked");
            Assert(help.Top == 286 && help.Height == 60 && help.Top - rate.Parent!.Bottom == 10,
                "The pattern description did not gain its 8px gap.");
            var settings = (AppSettings)Field(main, "_settings");
            var previousPattern = settings.ClickPattern;
            var previousMode = settings.ClickRateMode;
            foreach (var mode in new[] { "Ordinary", "Amplified" })
                foreach (var pattern in new[] { "Standard", "Burst", "Double Click", "Hold then Burst" })
                {
                    settings.ClickRateMode = mode;
                    settings.ClickPattern = pattern;
                    var text = (string)Call(main, "CustomPatternHelpText")!;
                    var size = UiTheme.MeasureText(text, help.Font, new Size(help.Width, int.MaxValue), TextFormatFlags.WordBreak);
                    Assert(size.Height <= help.Height, $"The extra gap clips the pattern description: {language}/{pattern}, height={size.Height}.");
                }
            settings.ClickPattern = previousPattern;
            settings.ClickRateMode = previousMode;
            var expected = language == "ru" ? "Аварийное закрытие" : "Emergency close";
            var caption = Descendants(main).OfType<Label>().Single(label => label.Text.Replace('\n', ' ') == expected);
            Assert(language != "ru" || caption.Text == "Аварийное\nзакрытие", "The emergency action remains abbreviated.");
            var captionSize = UiTheme.MeasureText(caption.Text, caption.Font, new Size(caption.Width, int.MaxValue), TextFormatFlags.WordBreak);
            Assert(captionSize.Width <= caption.Width && captionSize.Height <= caption.Height, "The full emergency caption is clipped.");
            Assert((string)Call(main, "GetHotkeyTargetDisplayName", "panicHotkey")! == expected,
                "The hotkey assignment prompt still uses the abbreviated action name.");
            Assert(((IDictionary)Field(Field(main, "_hoverTooltips"), "_messages")).Contains(caption),
                "Wrapping the full emergency caption lost its tooltip.");
            var button = (AccentButton)Field(main, "_btnMacroBackgroundHelp");
            Assert(button.Size == new Size(22, 22) && (bool)button.GetType().GetProperty("UsePixelHelpGlyph", Members)!.GetValue(button)!,
                "The help glyph lost its compact button or did not activate.");
            using var glyph = new Bitmap(22, 22);
            using (var graphics = Graphics.FromImage(glyph))
                Preview.GetMethod("DrawHelpGlyph", Members)!.Invoke(null, [graphics, new Rectangle(0, 0, 22, 22), Color.White]);
            var count = 0;
            for (var y = 0; y < glyph.Height; y++)
                for (var x = 0; x < glyph.Width; x++)
                    if (glyph.GetPixel(x, y).A != 0)
                    {
                        count++;
                        Assert(x >= 6 && x < 16 && y >= 4 && y < 18 && glyph.GetPixel(x, y).A == 255,
                            "The question glyph is not centered or contains blurry pixels.");
                    }
            Assert(count == 36, "The question glyph lost a pixel block.");
            var host = (AtomicPageHost)Field(main, "_pageHost");
            foreach (var page in new[] { 1, 2, 4 })
            {
                host.SelectedIndex = page;
                Call(main, "UpdateTabHeaderVisuals");
                Capture(main, language + "-polish-tab-" + page);
            }
        }
    }

    internal static void JournalAndMenus()
    {
        using var data = new SnapshotData();
        using var menu = new ContextMenuStrip();
        var click = 0;
        menu.Items.Add("Профиль / Profile", null, (_, _) => click++);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Закрыть / Close");
        Preview.GetMethod("ApplyMenu", Members)!.Invoke(null, [menu]);
        Assert(menu.Renderer.GetType().Name == "PixelMenuRenderer" && menu.Font.FontFamily.Name == "Monocraft",
            "The tray/dropdown menu retained a classic renderer or font.");
        menu.Items[0].PerformClick();
        Assert(click == 1, "The pixel menu changed click delivery.");
        using var dropdown = new PillDropdown();
        dropdown.SetItems(new[] { "Default", "Игровой профиль", "Test" });
        dropdown.SelectedIndex = 1;
        Assert(((ContextMenuStrip)Field(dropdown, "_menu")).Renderer.GetType().Name == "PixelMenuRenderer"
            && dropdown.SelectedItem == "Игровой профиль", "The dropdown style changed selection or missed its popup.");
        foreach (var language in new[] { "ru", "en" })
        {
            SetLanguage(language);
            var source = New("MacroDefinition");
            source.GetType().GetProperty("Name")!.SetValue(source, "Pixel journal");
            var events = (IList)source.GetType().GetProperty("Events")!.GetValue(source)!;
            for (var index = 0; index < 90; index++)
            {
                var item = New("MacroEvent");
                item.GetType().GetProperty("Type")!.SetValue(item, Enum.Parse(App.GetType("KofgeClicker.MacroEventType")!, "MouseMove"));
                item.GetType().GetProperty("OffsetMicroseconds")!.SetValue(item, (index + 1) * 1_000L);
                item.GetType().GetProperty("X")!.SetValue(item, 926 - index);
                item.GetType().GetProperty("Y")!.SetValue(item, 85 + index);
                events.Add(item);
            }
            var eventType = App.GetType("KofgeClicker.MacroEvent")!;
            var formatter = Expression.Lambda(typeof(Func<,>).MakeGenericType(eventType, typeof(string)),
                Expression.Constant(language == "ru" ? "Курсор перемещён в 926, 85" : "Cursor moved to 926, 85"),
                Expression.Parameter(eventType)).Compile();
            var storage = New("MacroStorage", Path.Combine((string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!, "Macros"));
            using var journal = (Form)New("MacroJournalDialog", source, storage, formatter);
            Capture(journal, language + "-journal");
            var timeline = (ListView)Field(journal, "_timeline");
            Assert(timeline.VirtualMode && timeline.VirtualListSize == 90, "The pixel journal lost virtualization.");
        }
    }

    internal static void AnimationColors()
    {
        var from = Color.FromArgb(8, 30, 44);
        var to = Color.FromArgb(0, 167, 249);
        var blend = App.GetType("KofgeClicker.CheckedStateAnimation", true)!.GetMethod("Blend", Members)!;
        foreach (var amount in new[] { 0f, .25f, .5f, .75f, 1f })
        {
            var result = (Color)blend.Invoke(null, [from, to, amount])!;
            var expected = Color.FromArgb(255, (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount), (int)Math.Round(from.B + (to.B - from.B) * amount));
            Assert(result == expected, "The pixel palette quantized an animated intermediate color.");
        }
        foreach (var color in new[] { UiTheme.AppBackground, UiTheme.CardInner, UiTheme.Surface, UiTheme.Accent, UiTheme.Border })
        {
            var map = Preview.GetMethod("MapColor", Members)!;
            Assert((Color)map.Invoke(null, [color])! == color, "Repeated styling changed an existing pixel color.");
        }
        for (var index = 0; index < 300; index++) UiTheme.MeasureText("Measured " + index, UiTheme.BodyFont);
        Assert(((IDictionary)Preview.GetField("MeasurementCache", Members)!.GetValue(null)!).Count <= 256,
            "The pixel text measurement cache is unbounded.");
    }

    internal static void InputAdminAndMenus()
    {
        using var data = new SnapshotData();
        SetLanguage("ru");
        using var prompt = (Form)New("PromptDialog", "Новая запись", "Введите название новой записи.", "Макрос 2 руфдц");
        Capture(prompt, "ru-prompt-input-regression");
        var input = (TextBox)Field(prompt, "_input");
        Assert(!input.AutoSize && input.Height >= input.Font.Height + 4,
            "The pixel input still uses a clipped single-line height.");
        Assert(input.Top >= 6 && input.Bottom <= input.Parent!.ClientSize.Height - 6,
            "The recording name extends outside its input shell.");
        input.Text = "Имя новой записи";
        Assert(((string)prompt.GetType().GetProperty("InputText")!.GetValue(prompt)!) == input.Text,
            "The input layout changed the recording name.");

        using var main = new MainForm();
        var admin = (CheckBox)Field(main, "_chkRunAsAdministrator");
        var startup = (CheckBox)Field(main, "_chkRunOnStartup");
        Assert(admin.Enabled && startup.Enabled, "Visual preview styling still disables functional options.");
        var helper = App.GetType("KofgeClicker.AdminLaunchHelper", true)!;
        var requested = helper.GetMethod("IsAdminLaunchRequested", Members)!;
        var directory = (string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!;
        Assert((string)helper.GetMethod("ResolveSettingsPath", Members)!.Invoke(null, null)! == Path.Combine(directory, "settings.ini"),
            "Administrator selection escaped the preview settings directory.");
        foreach (var enabled in new[] { true, false })
        {
            Call(main, "SetTourOptionValue", "runAsAdministrator", enabled);
            Assert(admin.Checked == enabled && (bool)requested.Invoke(null, null)! == enabled,
                "The administrator option does not persist for the next preview launch.");
        }

        using var menu = new ContextMenuStrip();
        Preview.GetMethod("ApplyMenu", Members)!.Invoke(null, [menu]);
        using var item = new ToolStripMenuItem("Любое окно") { Checked = true, AutoSize = false, Size = new Size(200, 29) };
        menu.Items.Add(item);
        using var bitmap = new Bitmap(24, 24);
        using var graphics = Graphics.FromImage(bitmap);
        var background = Color.FromArgb(8, 30, 44);
        graphics.Clear(background);
        using var nativeCheck = new Bitmap(16, 16);
        menu.Renderer.DrawItemCheck(new ToolStripItemImageRenderEventArgs(graphics, item, nativeCheck, new Rectangle(4, 4, 16, 16)));
        for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
                Assert(bitmap.GetPixel(x, y).ToArgb() == background.ToArgb(), "The menu still draws a native check square.");
        menu.Renderer.DrawMenuItemBackground(new ToolStripItemRenderEventArgs(graphics, item));
        Assert(bitmap.GetPixel(10, 10).ToArgb() == Color.FromArgb(17, 38, 57).ToArgb(),
            "Removing the native check lost the selected-value row highlight.");
    }

    internal static void StartupOptions()
    {
        using var data = new SnapshotData();
        using var main = new MainForm();
        var startup = (CheckBox)Field(main, "_chkRunOnStartup");
        Assert(startup.Enabled, "The startup switch is blocked by the visual preview.");
        var directory = (string)Preview.GetProperty("SnapshotDataDirectory", Members)!.GetValue(null)!;
        var path = Path.Combine(directory, "settings.ini");
        foreach (var enabled in new[] { true, false })
        {
            startup.Checked = enabled;
            Assert(new IniFile(path).ReadBool("Main", "RunOnWindowsStartup") == enabled,
                "The startup switch does not save the selected state.");
            Call(main, "LoadSettings");
            Assert(((AppSettings)Field(main, "_settings")).RunOnWindowsStartup == enabled,
                "The startup setting does not survive reload.");
        }
        using var tour = (Form)New("FirstRunTourDialog", (Action<int>)(_ => { }), (Action<string>)(_ => { }),
            (Func<string, string>)(_ => "F2"), (Action<string>)(_ => { }), (Func<string>)(() => "hold"),
            (Func<string, bool>)(target => (bool)Call(main, "GetTourOptionValue", target)!),
            (Action<string, bool>)((target, value) => Call(main, "SetTourOptionValue", target, value)));
        Call(tour, "ShowStep", 6);
        var tourStartup = Descendants(tour).OfType<CheckBox>().Single(c => c.Tag as string == "runOnStartup");
        Assert(tourStartup.Enabled, "The tutorial still blocks the startup setting.");
        foreach (var enabled in new[] { true, false })
        {
            tourStartup.Checked = enabled;
            Assert(startup.Checked == enabled && new IniFile(path).ReadBool("Main", "RunOnWindowsStartup") == enabled,
                "The tutorial and options startup controls disagree.");
        }
    }

    private static void Capture(Form form, string name)
    {
        Preview.GetMethod("ApplyTree", Members)!.Invoke(null, [form]);
        form.ShowInTaskbar = false;
        form.TopMost = false;
        form.Opacity = 0;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        if (!form.Visible) form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        if (form.Controls.Count > 0)
        {
            var colored = 0;
            for (var y = 0; y < bitmap.Height; y += 3)
                for (var x = 0; x < bitmap.Width; x += 3)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel == Color.FromArgb(17, 38, 57) || pixel == Color.FromArgb(13, 47, 68)
                        || pixel == Color.FromArgb(8, 30, 44)) colored++;
                }
            Assert(colored > 80, "The snapshot did not paint its child controls: " + name);
        }
        Assert(form is PixelThemedForm && form.BackColor == Color.FromArgb(9, 18, 29), "A dialog retained its old window surface.");
        foreach (var control in Descendants(form))
        {
            Assert(control.Font.FontFamily.Name == "Monocraft", $"Old font on {name}/{control.GetType().Name}: {control.Font.FontFamily.Name}.");
            if (control is Label) Assert(control.GetType().Name is "SurfaceLabel" or "CompactWrappedLabel", "A native label bypasses pixel text rendering.");
            if (control is AccentButton button)
                Assert(UiTheme.MeasureText(button.Text, button.Font).Width <= button.Width,
                    "A pixel button caption does not fit: " + name + "/" + button.Text);
        }
        var output = Environment.GetEnvironmentVariable("KOFGE_PIXEL_SURFACE_SNAPSHOTS");
        if (!string.IsNullOrWhiteSpace(output))
        {
            Directory.CreateDirectory(output);
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
            if (form is MainForm)
            {
                var header = (Control)Field(form, "_tabHeader");
                using var headerImage = new Bitmap(header.Width, header.Height);
                header.DrawToBitmap(headerImage, header.ClientRectangle);
                foreach (Control button in header.Controls)
                    Assert(headerImage.GetPixel(button.Left + button.Width / 2, button.Bottom - 1).ToArgb() != header.BackColor.ToArgb(),
                        "The tab header clips a button's bottom outline.");
                headerImage.Save(Path.Combine(output, name + "-header.png"), ImageFormat.Png);
            }
        }
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static object New(string name, params object[] args)
        => Activator.CreateInstance(App.GetType("KofgeClicker." + name, true)!, Members, null, args, null)!;
    private static object Field(object owner, string name) => owner.GetType().GetField(name, Members)!.GetValue(owner)!;
    private static object? Call(object owner, string name, params object[] args)
        => owner.GetType().GetMethod(name, Members)!.Invoke(owner, args);
    private static void SetLanguage(string language)
        => App.GetType("KofgeClicker.LocalizationService", true)!.GetMethod("Initialize", Members)!.Invoke(null, [language]);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class SnapshotData : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "Kofge-Pixel-QA-" + Guid.NewGuid().ToString("N"));
        internal SnapshotData()
        {
            Assert(Enabled, "Surface snapshots require the pixel theme.");
            Preview.GetProperty("SnapshotDataDirectory", Members)!.SetValue(null, _directory);
            Directory.CreateDirectory(_directory);
            new IniFile(Path.Combine(_directory, "settings.ini")).UpdateSections(
            [
                ("Main", new List<KeyValuePair<string, string>>
                {
                    new("Language", "ru"), new("AutoEnabled", "0"),
                    new("UiStyle", Enabled ? "Pixel" : "Classic"), new("RunOnWindowsStartup", "0")
                }),
                ("App", new List<KeyValuePair<string, string>>
                {
                    new("FirstRunTourCompleted", "1"), new("LastSeenWhatsNewVersion", "v1.1.0")
                })
            ], flushToDisk: true);
        }
        public void Dispose()
        {
            Preview.GetProperty("SnapshotDataDirectory", Members)!.SetValue(null, null);
            var full = Path.GetFullPath(_directory);
            if (full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                try { Directory.Delete(full, true); } catch (IOException) { }
        }
    }
}
