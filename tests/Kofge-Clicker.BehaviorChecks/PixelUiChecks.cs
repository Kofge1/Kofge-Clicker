using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using KofgeClicker;

internal static class PixelUiChecks
{
    private const BindingFlags Members = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly Type Preview = typeof(MainForm).Assembly.GetType("KofgeClicker.PixelUiPreview", true)!;
    private static Font Font(float size) => (Font)Preview.GetMethod("Font", Members)!.Invoke(null, [size])!;
    private static void Mark(Control control) => Preview.GetMethod("Mark", Members)!.Invoke(null, [control]);

    internal static void FontAndLayout()
    {
        var font = Font(20);
        Assert(font.FontFamily.Name == "Monocraft", "The embedded pixel font fell back to another family.");
        var dc = CreateCompatibleDC(IntPtr.Zero);
        var handle = font.ToHfont();
        var previous = SelectObject(dc, handle);
        try
        {
            const string text = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюяABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-×";
            var glyphs = new ushort[text.Length];
            Assert(GetGlyphIndices(dc, text, text.Length, glyphs, 1) != uint.MaxValue &&
                glyphs.All(index => index != 0xFFFF), "The pixel font is missing Russian, English or numeric glyphs.");
        }
        finally
        {
            SelectObject(dc, previous);
            DeleteObject(handle);
            DeleteDC(dc);
        }
        foreach (var item in new[]
        {
            (Text: "Удержание", Size: 20f, Width: 146, Height: 40),
            (Text: "Переключение", Size: 20f, Width: 146, Height: 40),
            (Text: "Назначить", Size: 16f, Width: 96, Height: 40),
            (Text: "Естеств. клики", Size: 20f, Width: 178, Height: 32),
            (Text: "Кликер", Size: 24f, Width: 200, Height: 32),
            (Text: "v1.0.9", Size: 24f, Width: 88, Height: 40),
            (Text: "100", Size: 32f, Width: 170, Height: 37)
        })
        {
            var draw = Preview.GetMethod("DrawText", Members)!;
            using var constrained = new Bitmap(item.Width, item.Height);
            using var roomy = new Bitmap(500, 100);
            foreach (var target in new[] { constrained, roomy })
                using (var graphics = Graphics.FromImage(target))
                    draw.Invoke(null, [graphics, item.Text, Font(item.Size), new Rectangle(Point.Empty, target.Size), Color.White, ContentAlignment.TopLeft]);
            for (var y = 0; y < roomy.Height; y++)
                for (var x = 0; x < roomy.Width; x++)
                {
                    var expected = roomy.GetPixel(x, y).A == 255;
                    var actual = x < constrained.Width && y < constrained.Height && constrained.GetPixel(x, y).A == 255;
                    Assert(expected == actual, $"Pixel text is clipped: {item.Text}.");
                }
        }
        Assert(UiTheme.BodyFont.FontFamily.Name != "Monocraft", "The preview changed the normal application font.");
    }

    internal static void RenderingAndInteractionIsolation()
    {
        using var normal = new AccentButton { Text = "Default", Width = 140, Height = 40 };
        using var before = new Bitmap(140, 40, PixelFormat.Format32bppArgb);
        normal.DrawToBitmap(before, normal.ClientRectangle);
        using var host = new RoundedPanel
        {
            Width = 440, Height = 240, FillColor = Color.FromArgb(13, 47, 68),
            BorderColor = Color.FromArgb(36, 78, 105), UseAntialiasedEdges = true,
            DrawShadow = false, CacheBackgroundDrawing = true
        };
        Mark(host);
        using var button = new AccentButton { Text = "Pixel", Bounds = new Rectangle(20, 20, 140, 40), Font = Font(20), Primary = true, UseFilledBorderRing = true };
        using var toggle = new ToggleSwitchCheckBox { Bounds = new Rectangle(180, 20, 110, 40), Font = Font(20), UseSlidingKnob = false };
        using var hold = new SegmentRadioButton { Text = "Hold", Bounds = new Rectangle(20, 80, 140, 40), Font = Font(20) };
        using var alternate = new SegmentRadioButton { Text = "Toggle", Bounds = new Rectangle(170, 80, 140, 40), Font = Font(20) };
        using var slider = new ModernSlider { Bounds = new Rectangle(20, 150, 380, 40), Minimum = 1, Maximum = 100, Value = 40 };
        host.Controls.AddRange([button, toggle, hold, alternate, slider]);
        foreach (Control control in host.Controls) Mark(control);
        var bounds = host.Controls.Cast<Control>().Select(control => control.Bounds).ToArray();
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        button.PerformClick();
        Assert(clicks == 1, "Styling changed button click delivery.");
        toggle.Checked = true;
        hold.Checked = true;
        alternate.Checked = true;
        Assert(toggle.Checked && alternate.Checked && !hold.Checked, "Styling changed toggle or radio-group behavior.");
        var changes = 0;
        slider.ValueChanged += (_, _) => changes++;
        slider.Value = 65;
        Assert(changes == 1 && slider.Value == 65, "Styling changed slider values.");
        using var image = new Bitmap(host.Width, host.Height, PixelFormat.Format32bppArgb);
        host.DrawToBitmap(image, host.ClientRectangle);
        Assert(host.Controls.Cast<Control>().Select(control => control.Bounds).SequenceEqual(bounds), "Styling moved controls.");
        Assert(image.GetPixel(220, 220).ToArgb() == host.FillColor.ToArgb(), "The preview panel did not render its surface.");
        using var after = new Bitmap(140, 40, PixelFormat.Format32bppArgb);
        normal.DrawToBitmap(after, normal.ClientRectangle);
        for (var y = 0; y < after.Height; y++)
            for (var x = 0; x < after.Width; x++)
                Assert(before.GetPixel(x, y) == after.GetPixel(x, y), "Styling one control changed an unstyled control.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void NativeTextGrid()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        const string sample = "Аа Вв Нн Паттерн Назначить";
        foreach (var size in new[] { 8f, 16f, 20f, 24f, 32f })
        {
            using var bitmap = new Bitmap(600, 60, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
                draw.Invoke(null, [graphics, sample, Font(size), new Rectangle(0, 0, 600, 60), Color.White, ContentAlignment.TopLeft]);
            var ink = 0;
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var alpha = bitmap.GetPixel(x, y).A;
                    Assert(alpha is 0 or 180 or 255, "Pixel text contains interpolated edge pixels.");
                    if (alpha == 255) ink++;
                }
            Assert(ink > 40, "Pixel text lost its glyphs.");
        }
        using var normalDpi = new Bitmap(200, 40);
        using var highDpi = new Bitmap(200, 40);
        highDpi.SetResolution(144, 144);
        foreach (var target in new[] { normalDpi, highDpi })
            using (var graphics = Graphics.FromImage(target))
                draw.Invoke(null, [graphics, "Аа Вв Нн", Font(20), new Rectangle(0, 0, 200, 40), Color.White, ContentAlignment.MiddleCenter]);
        for (var y = 0; y < normalDpi.Height; y++)
            for (var x = 0; x < normalDpi.Width; x++)
                Assert(normalDpi.GetPixel(x, y) == highDpi.GetPixel(x, y), "Pixel-unit fonts were scaled twice at high DPI.");
        // Dynamic binding labels must not grow the raster cache without a bound.
        using var scratch = new Bitmap(200, 40);
        using (var graphics = Graphics.FromImage(scratch))
            for (var index = 0; index < 280; index++)
                draw.Invoke(null, [graphics, $"F{index}", Font(20), new Rectangle(0, 0, 200, 40), Color.White, ContentAlignment.MiddleCenter]);
        var cache = (System.Collections.IDictionary)Preview.GetField("TextCache", Members)!.GetValue(null)!;
        Assert(cache.Count <= 256, "The preview text cache is unbounded.");

        var samplePath = Environment.GetEnvironmentVariable("KOFGE_PIXEL_TEXT_SAMPLE");
        if (string.IsNullOrWhiteSpace(samplePath)) return;
        using var comparison = new Bitmap(1000, 320);
        using (var graphics = Graphics.FromImage(comparison))
        {
            graphics.Clear(Color.FromArgb(13, 47, 68));
            for (var row = 0; row < 4; row++)
            {
                var size = new[] { 16f, 20f, 24f, 32f }[row];
                var bounds = new Rectangle(20, row * 80 + 10, 960, 32);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                using var format = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap };
                using var brush = new SolidBrush(Color.White);
                graphics.DrawString(sample, Font(size), brush, bounds, format);
                bounds.Y += 32;
                draw.Invoke(null, [graphics, sample, Font(size), bounds, Color.White, ContentAlignment.TopLeft]);
            }
        }
        comparison.Save(samplePath, ImageFormat.Png);
    }

    internal static void PositionIndependentGlyphs()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        foreach (var size in new[] { 16f, 20f, 24f, 32f })
            foreach (var letter in new[] { "К", "к", "А", "а", "В", "в", "Н", "н" })
        {
            using var reference = new Bitmap(400, 80);
            using (var graphics = Graphics.FromImage(reference))
                draw.Invoke(null, [graphics, letter, Font(size), new Rectangle(Point.Empty, reference.Size), Color.White, ContentAlignment.TopLeft]);
            var expected = InkShapes(reference).Single();
            foreach (var width in new[] { 180, 217, 320 })
                foreach (var alignment in new[] { ContentAlignment.TopLeft, ContentAlignment.MiddleCenter, ContentAlignment.BottomRight })
                {
                    using var sample = new Bitmap(400, 100);
                    using (var graphics = Graphics.FromImage(sample))
                        draw.Invoke(null, [graphics, $"А{letter}Н{letter}В{letter}{letter}", Font(size), new Rectangle(7, 9, width, 70), Color.White, alignment]);
                    var letters = InkShapes(sample);
                    Assert(letters.Count == 7, "Glyph-position check lost a letter or merged neighboring letters.");
                    foreach (var index in new[] { 1, 3, 5, 6 })
                        Assert(letters[index] == expected, $"The same {letter} has different pixels at {size}px, width {width}, alignment {alignment}, index {index}.");
                }
        }
    }

    internal static void GlyphLayoutAndCache()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        using var reference = new Bitmap(200, 160);
        using var lf = new Bitmap(200, 160);
        using var crlf = new Bitmap(200, 160);
        using (var graphics = Graphics.FromImage(reference))
            draw.Invoke(null, [graphics, "К", Font(20), new Rectangle(0, 0, 200, 160), Color.White, ContentAlignment.TopLeft]);
        using (var graphics = Graphics.FromImage(lf))
            draw.Invoke(null, [graphics, "К К К\nК", Font(20), new Rectangle(0, 0, 25, 160), Color.White, ContentAlignment.TopLeft]);
        using (var graphics = Graphics.FromImage(crlf))
            draw.Invoke(null, [graphics, "К К К\r\nК", Font(20), new Rectangle(0, 0, 25, 160), Color.White, ContentAlignment.TopLeft]);
        var singleInk = 0;
        var wrappedInk = 0;
        for (var y = 0; y < lf.Height; y++)
            for (var x = 0; x < lf.Width; x++)
            {
                if (reference.GetPixel(x, y).A == 255) singleInk++;
                if (lf.GetPixel(x, y).A == 255) wrappedInk++;
                Assert(lf.GetPixel(x, y) == crlf.GetPixel(x, y), "CRLF and LF text have different layout.");
            }
        Assert(wrappedInk == singleInk * 4, "Wrapping dropped or clipped a glyph.");

        using var scratch = new Bitmap(200, 60);
        using (var graphics = Graphics.FromImage(scratch))
        {
            var bounds = new Rectangle(7, 9, 40, 40);
            draw.Invoke(null, [graphics, "Переключение", Font(20), bounds, Color.White, ContentAlignment.MiddleCenter]);
            var ink = 0;
            for (var y = 0; y < scratch.Height; y++)
                for (var x = 0; x < scratch.Width; x++)
                {
                    var pixel = scratch.GetPixel(x, y);
                    Assert(bounds.Contains(x, y) || pixel.A == 0, "Ellipsized text escaped its bounds.");
                    if (pixel.A == 255) ink++;
                }
            Assert(ink > 0 && ink < singleInk * 12, "Ellipsized text was blank or untrimmed.");
            for (var index = 0; index < 220; index++)
                draw.Invoke(null, [graphics, "КНВ", Font(20), new Rectangle(0, 0, 200, 60), Color.FromArgb(255, index, 100, 200), ContentAlignment.TopLeft]);
        }
        var cache = (System.Collections.IDictionary)Preview.GetField("GlyphCache", Members)!.GetValue(null)!;
        Assert(cache.Count <= 512, "The preview glyph cache is unbounded.");
        using var rebuilt = new Bitmap(200, 160);
        using (var graphics = Graphics.FromImage(rebuilt))
            draw.Invoke(null, [graphics, "К", Font(20), new Rectangle(0, 0, 199, 160), Color.White, ContentAlignment.TopLeft]);
        Assert(InkShapes(rebuilt).Single() == InkShapes(reference).Single(), "Evicted glyphs changed shape after rebuilding.");
    }

    internal static void BorderContainment()
    {
        using var path = (System.Drawing.Drawing2D.GraphicsPath)Preview.GetMethod("Path", Members)!.Invoke(null,
            [new RectangleF(0.5f, 0.5f, 145, 31)])!;
        var bounds = path.GetBounds();
        Assert(bounds.Right <= 145 && bounds.Bottom <= 31, "The preview outline extends past the last drawable pixel.");
        using var pill = new InfoPill { Width = 146, Height = 32, Text = "F2", Font = Font(20), BorderColor = Color.Magenta };
        using var toggle = new ToggleSwitchCheckBox
        {
            Width = 112, Height = 40, Font = Font(20), UseSlidingKnob = false,
            UncheckedBorderColor = Color.Magenta
        };
        using var radio = new SegmentRadioButton { Width = 146, Height = 40, Text = "Удержание", Font = Font(20) };
        using var button = new AccentButton { Width = 220, Height = 42, Text = "Закрыть", Font = Font(20), UseFilledBorderRing = true };
        foreach (var control in new Control[] { pill, toggle, radio, button })
        {
            Mark(control);
            using var bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, control.ClientRectangle);
            var expected = control == radio || control == button ? Color.FromArgb(36, 78, 105) : Color.Magenta;
            var left = control == pill || control == button ? 0 : 1;
            var right = control == pill || control == button ? control.Width - 1 : control.Width - 2;
            foreach (var point in new[] { new Point(control.Width / 2, control.Height - 1), new Point(control.Width / 2, 0),
                new Point(left, control.Height / 2), new Point(right, control.Height / 2) })
                Assert(bitmap.GetPixel(point.X, point.Y).ToArgb() == expected.ToArgb(),
                    $"The outline is missing on {control.GetType().Name} at {point}.");
        }
    }

    internal static void OpticalTextAlignment()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        foreach (var text in new[] { "ВЫКЛ", "Назначить", "Удержание", "Переключение", "F2", "RU", "Применить", "Закрыть" })
            foreach (var size in new[] { 16f, 20f })
            {
                using var bitmap = new Bitmap(260, 80);
                var bounds = new Rectangle(7, 9, 240, 52);
                using (var graphics = Graphics.FromImage(bitmap))
                    draw.Invoke(null, [graphics, text, Font(size), bounds, Color.White, ContentAlignment.MiddleCenter]);
                var ink = ForegroundBounds(bitmap);
                Assert(Math.Abs((ink.Left - bounds.Left) - (bounds.Right - ink.Right)) <= 1,
                    $"Horizontal visible-text centering is wrong: {text}, {size}px.");
                Assert(Math.Abs((ink.Top - bounds.Top) - (bounds.Bottom - ink.Bottom)) <= 1,
                    $"Vertical visible-text centering is wrong: {text}, {size}px.");
            }
        using var button = new AccentButton { Width = 220, Height = 42, Text = "Применить", Font = Font(20), UseFilledBorderRing = true };
        using var radio = new SegmentRadioButton { Width = 146, Height = 40, Text = "Переключение", Font = Font(20) };
        using var pill = new InfoPill { Width = 146, Height = 32, Text = "F2", Font = Font(20), ForeColor = Color.FromArgb(239, 248, 255) };
        using var toggle = new ToggleSwitchCheckBox
        {
            Width = 112, Height = 40, Font = Font(20), UseSlidingKnob = false,
            UncheckedLabelColor = Color.FromArgb(239, 248, 255)
        };
        foreach (var control in new Control[] { button, radio, pill, toggle })
        {
            Mark(control);
            using var bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, control.ClientRectangle);
            using var mask = new Bitmap(bitmap.Width, bitmap.Height);
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() == Color.FromArgb(239, 248, 255).ToArgb())
                        mask.SetPixel(x, y, Color.White);
            var ink = ForegroundBounds(mask);
            Assert(!ink.IsEmpty && Math.Abs(ink.Left - (control.Width - ink.Right)) <= 1
                && Math.Abs(ink.Top - (control.Height - ink.Bottom)) <= 1,
                $"The control did not center its foreground: {control.GetType().Name}.");
        }
        var samplePath = Environment.GetEnvironmentVariable("KOFGE_PIXEL_CONTROL_SAMPLE");
        if (!string.IsNullOrWhiteSpace(samplePath))
        {
            using var sample = new Bitmap(960, 200);
            using var graphics = Graphics.FromImage(sample);
            graphics.Clear(Color.FromArgb(13, 47, 68));
            var x = 24;
            foreach (var control in new Control[] { button, radio, pill, toggle })
            {
                using var snapshot = new Bitmap(control.Width, control.Height);
                control.DrawToBitmap(snapshot, control.ClientRectangle);
                graphics.DrawImageUnscaled(snapshot, x, 24 + (42 - control.Height) / 2);
                x += control.Width + 24;
            }
            draw.Invoke(null, [graphics, "Кликер выключен\nDefault  ·  F2 → LMB  ·  41 CPS  ·  Станд.  ·  Фиксир.", Font(16),
                new Rectangle(24, 100, 912, 76), Color.White, ContentAlignment.MiddleCenter]);
            sample.Save(samplePath, ImageFormat.Png);
        }
    }

    internal static void MultilineSpacing()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        using var bitmap = new Bitmap(780, 64);
        using (var graphics = Graphics.FromImage(bitmap))
            draw.Invoke(null, [graphics, "Кликер выключен\nDefault  ·  F2 → LMB  ·  41 CPS  ·  Станд.  ·  Фиксир.", Font(16),
                new Rectangle(Point.Empty, bitmap.Size), Color.White, ContentAlignment.MiddleCenter]);
        var rows = Enumerable.Range(0, bitmap.Height)
            .Where(y => Enumerable.Range(0, bitmap.Width).Any(x => bitmap.GetPixel(x, y).A == 255)).ToArray();
        var groups = new List<(int Top, int Bottom)>();
        foreach (var y in rows)
        {
            if (groups.Count == 0 || y > groups[^1].Bottom + 1) groups.Add((y, y));
            else groups[^1] = (groups[^1].Top, y);
        }
        Assert(groups.Count == 2, "Status text did not form two intact visible lines.");
        Assert(groups[1].Top - groups[0].Bottom - 1 >= 6, "The status lines are too close together.");
        Assert(Math.Abs(groups[0].Top - (bitmap.Height - groups[1].Bottom - 1)) <= 1,
            "The two-line status block is not centered.");
    }

    internal static void WrappedLabelConsistency()
    {
        using var label = (Label)Activator.CreateInstance(typeof(MainForm).Assembly.GetType("KofgeClicker.CompactWrappedLabel", true)!)!;
        label.Size = new Size(140, 43);
        label.Text = "Клавиша\nзапуска";
        label.Font = Font(20);
        label.ForeColor = Color.White;
        label.BackColor = Color.Black;
        label.TextAlign = ContentAlignment.MiddleLeft;
        Mark(label);
        using var actual = new Bitmap(label.Width, label.Height);
        using var expected = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(actual, label.ClientRectangle);
        using (var graphics = Graphics.FromImage(expected))
            Preview.GetMethod("DrawText", Members)!.Invoke(null,
                [graphics, label.Text, label.Font, label.ClientRectangle, Color.White, label.TextAlign]);
        for (var y = 0; y < actual.Height; y++)
            for (var x = 0; x < actual.Width; x++)
                Assert((actual.GetPixel(x, y).ToArgb() == Color.White.ToArgb()) == (expected.GetPixel(x, y).A == 255),
                    "A wrapped preview label bypassed the canonical glyph renderer.");
    }

    internal static void UniformStatusGrid()
    {
        var draw = Preview.GetMethod("DrawUniformText", Members)!;
        foreach (var text in new[] { "Default", "Кликер", "LMB CPS", "АВЕНКМОРСТХ авекмнорстх", "ABEKMHOPCTX abekmhopctx" })
        {
            using var bitmap = new Bitmap(780, 64);
            using (var graphics = Graphics.FromImage(bitmap))
                draw.Invoke(null, [graphics, text, Font(16), new Rectangle(Point.Empty, bitmap.Size), Color.White, ContentAlignment.TopLeft]);
            var ink = ForegroundBounds(bitmap);
            Assert(!ink.IsEmpty && ink.Width % 2 == 0 && ink.Height % 2 == 0, "The status text is not aligned to a 2px grid.");
            for (var y = ink.Top; y < ink.Bottom; y += 2)
                for (var x = ink.Left; x < ink.Right; x += 2)
                {
                    var filled = bitmap.GetPixel(x, y).A == 255;
                    Assert((bitmap.GetPixel(x + 1, y).A == 255) == filled
                        && (bitmap.GetPixel(x, y + 1).A == 255) == filled
                        && (bitmap.GetPixel(x + 1, y + 1).A == 255) == filled,
                        $"Uneven native pixel size in status text: {text} at {x},{y}.");
                }
        }
        using var status = new Bitmap(781, 64);
        using (var graphics = Graphics.FromImage(status))
            draw.Invoke(null, [graphics, "Кликер выключен\r\n" + new string('W', 200), Font(16),
                new Rectangle(Point.Empty, status.Size), Color.White, ContentAlignment.MiddleCenter]);
        var rows = Enumerable.Range(0, status.Height)
            .Where(y => Enumerable.Range(0, status.Width).Any(x => status.GetPixel(x, y).A == 255)).ToArray();
        var groups = new List<(int Top, int Bottom)>();
        foreach (var y in rows)
        {
            if (groups.Count == 0 || y > groups[^1].Bottom + 1) groups.Add((y, y));
            else groups[^1] = (groups[^1].Top, y);
        }
        Assert(groups.Count == 2 && groups[1].Top - groups[0].Bottom - 1 >= 6,
            "A long status summary wrapped into extra lines or lost its line gap.");
        Assert(Math.Abs(groups[0].Top - (status.Height - groups[1].Bottom - 1)) <= 1,
            "The uniform status text is not centered.");
    }

    internal static void UniformStatusIsolation()
    {
        var regular = Preview.GetMethod("DrawText", Members)!;
        var uniform = Preview.GetMethod("DrawUniformText", Members)!;
        const string text = "Кликер выключен\nDefault  ·  F2 → LMB  ·  41 CPS  ·  Станд.  ·  Фиксир.";
        using var before = new Bitmap(781, 64);
        using var after = new Bitmap(781, 64);
        using var expected = new Bitmap(781, 64);
        using (var graphics = Graphics.FromImage(before))
            regular.Invoke(null, [graphics, text, Font(16), new Rectangle(Point.Empty, before.Size), Color.White, ContentAlignment.MiddleCenter]);
        using (var graphics = Graphics.FromImage(expected))
            uniform.Invoke(null, [graphics, text, Font(16), new Rectangle(Point.Empty, expected.Size), Color.White, ContentAlignment.MiddleCenter]);
        using (var graphics = Graphics.FromImage(after))
            regular.Invoke(null, [graphics, text, Font(16), new Rectangle(Point.Empty, after.Size), Color.White, ContentAlignment.MiddleCenter]);
        using var label = (Label)Activator.CreateInstance(typeof(MainForm).Assembly.GetType("KofgeClicker.SurfaceLabel", true)!)!;
        label.Size = before.Size;
        label.Text = text;
        label.Font = Font(16);
        label.ForeColor = Color.White;
        label.BackColor = Color.Black;
        label.TextAlign = ContentAlignment.MiddleCenter;
        Preview.GetMethod("MarkUniformText", Members)!.Invoke(null, [label]);
        using var actual = new Bitmap(label.Width, label.Height);
        label.DrawToBitmap(actual, label.ClientRectangle);
        var changed = false;
        for (var y = 0; y < before.Height; y++)
            for (var x = 0; x < before.Width; x++)
            {
                Assert(before.GetPixel(x, y) == after.GetPixel(x, y), "Status styling changed the regular pixel renderer.");
                changed |= before.GetPixel(x, y) != expected.GetPixel(x, y);
                Assert((actual.GetPixel(x, y).ToArgb() == Color.White.ToArgb()) == (expected.GetPixel(x, y).A == 255),
                    "The status label did not use its uniform pixel renderer.");
            }
        Assert(changed && label.Font.Size == 16 && label.Text == text, "The status option was ineffective or changed the content/font setting.");
        var samplePath = Environment.GetEnvironmentVariable("KOFGE_PIXEL_STATUS_SAMPLE");
        if (string.IsNullOrWhiteSpace(samplePath)) return;
        using var sample = new Bitmap(829, 204);
        using (var graphics = Graphics.FromImage(sample))
        {
            graphics.Clear(Color.FromArgb(17, 38, 57));
            regular.Invoke(null, [graphics, text, Font(16), new Rectangle(24, 10, 781, 80), Color.White, ContentAlignment.MiddleCenter]);
            uniform.Invoke(null, [graphics, text, Font(16), new Rectangle(24, 110, 781, 80), Color.White, ContentAlignment.MiddleCenter]);
        }
        sample.Save(samplePath, ImageFormat.Png);
    }

    private static Rectangle ForegroundBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).A == 255)
                {
                    left = Math.Min(left, x); top = Math.Min(top, y);
                    right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static List<string> InkShapes(Bitmap bitmap)
    {
        var shapes = new List<string>();
        var x = 0;
        bool Ink(int px, int py) => bitmap.GetPixel(px, py).A == 255;
        bool Column(int px) => Enumerable.Range(0, bitmap.Height).Any(y => Ink(px, y));
        while (x < bitmap.Width)
        {
            if (!Column(x)) { x++; continue; }
            var left = x;
            while (x < bitmap.Width && Column(x)) x++;
            var right = x;
            var top = 0;
            while (!Enumerable.Range(left, right - left).Any(px => Ink(px, top))) top++;
            var bottom = bitmap.Height - 1;
            while (!Enumerable.Range(left, right - left).Any(px => Ink(px, bottom))) bottom--;
            var pattern = new System.Text.StringBuilder($"{right - left}x{bottom - top + 1}:");
            for (var y = top; y <= bottom; y++)
                for (var px = left; px < right; px++) pattern.Append(Ink(px, y) ? '1' : '0');
            shapes.Add(pattern.ToString());
        }
        return shapes;
    }

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetGlyphIndicesW")]
    private static extern uint GetGlyphIndices(IntPtr dc, string text, int count, [Out] ushort[] glyphs, uint flags);
}
