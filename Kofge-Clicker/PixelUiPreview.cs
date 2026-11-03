using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KofgeClicker;

internal static partial class PixelUiPreview
{
    internal static string? SnapshotDataDirectory { get; set; }
    internal static bool IsSnapshotRendering => SnapshotDataDirectory is not null;
    internal static bool Enabled => UiAppearance.ActiveStyle == UiAppearance.Pixel;
    internal static bool IsPreviewBuild
    {
        get
        {
#if PIXEL_UI_PREVIEW
            return true;
#else
            return false;
#endif
        }
    }

    internal static readonly Color Background = UiPalette.Background;
    internal static readonly Color Frame = UiPalette.Frame;
    internal static readonly Color Card = UiPalette.Card;
    internal static readonly Color Surface = UiPalette.Surface;
    internal static readonly Color Border = UiPalette.Border;
    internal static readonly Color Accent = UiPalette.Accent;
    internal static readonly Color AccentEdge = UiPalette.AccentEdge;
    internal static readonly Color Text = UiPalette.Text;
    private static readonly ConditionalWeakTable<Control, object> Styled = new();
    private static readonly ConditionalWeakTable<Control, object> UniformText = new();
    private static readonly Lazy<PreviewFonts> Fonts = new(() => new PreviewFonts());
    private const int TextCacheLimit = 256;
    private const long TextCacheByteLimit = 16L * 1024 * 1024;
    private const float NativeFontSize = 9f;
    private const float TextWidthScale = 0.85f;
    private const int MinimumLineGap = 6;
    private static readonly Dictionary<TextKey, Bitmap> TextCache = new();
    private static readonly Queue<TextKey> TextCacheOrder = new();
    private static long _textCacheBytes;
    private static readonly Dictionary<TextKey, Size> MeasurementCache = new();
    private static readonly Queue<TextKey> MeasurementOrder = new();
    private const int GlyphCacheLimit = 512;
    private static readonly Dictionary<GlyphKey, Glyph> GlyphCache = new();
    private static readonly Queue<GlyphKey> GlyphCacheOrder = new();

    static PixelUiPreview()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            foreach (var bitmap in TextCache.Values) bitmap.Dispose();
            foreach (var glyph in GlyphCache.Values) glyph.Image.Dispose();
        };
    }

    internal static bool IsStyled(Control control) => Enabled || Styled.TryGetValue(control, out _);

    internal static void Mark(Control control)
    {
        Styled.GetValue(control, _ => new object());
        control.Invalidate();
    }

    internal static void MarkUniformText(Control control)
    {
        UniformText.GetValue(control, _ => new object());
        Mark(control);
    }

    internal static bool UsesUniformText(Control control) => UniformText.TryGetValue(control, out _);

    internal static Font Font(float pixels) => Fonts.Value.Get(pixels);

    internal static void PrepareData()
    {
        if (!IsPreviewBuild || File.Exists(AppPaths.SettingsPath)) return;
        new IniFile(AppPaths.SettingsPath).UpdateSections(
        [
            ("Main", new List<KeyValuePair<string, string>>
            {
                new("Language", "ru"), new("CPS", "40"), new("AutoEnabled", "0"),
                new("StartMinimized", "0"), new("CloseToTrayOnClose", "0"),
                new("RunOnWindowsStartup", "0"), new("RunAsAdministrator", "0"),
                new("UiStyle", UiAppearance.Pixel)
            }),
            ("App", new List<KeyValuePair<string, string>>
            {
                new("FirstRunTourCompleted", "1"), new("LastSeenWhatsNewVersion", AppVersion.Display)
            })
        ], flushToDisk: true);
    }

    internal static void Configure(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.PixelOffsetMode = PixelOffsetMode.None;
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
    }

    internal static GraphicsPath Path(RectangleF bounds)
    {
        var x = (float)Math.Floor(bounds.Left);
        var y = (float)Math.Floor(bounds.Top);
        var right = x + (float)Math.Floor(bounds.Width);
        var bottom = y + (float)Math.Floor(bounds.Height);
        var step = Math.Min(3f, Math.Min(right - x, bottom - y) / 8f);
        var path = new GraphicsPath();
        path.AddPolygon(new PointF[]
        {
            new(x + step * 3, y), new(right - step * 3, y),
            new(right - step * 3, y + step), new(right - step, y + step),
            new(right - step, y + step * 3), new(right, y + step * 3),
            new(right, bottom - step * 3), new(right - step, bottom - step * 3),
            new(right - step, bottom - step), new(right - step * 3, bottom - step),
            new(right - step * 3, bottom), new(x + step * 3, bottom),
            new(x + step * 3, bottom - step), new(x + step, bottom - step),
            new(x + step, bottom - step * 3), new(x, bottom - step * 3),
            new(x, y + step * 3), new(x + step, y + step * 3),
            new(x + step, y + step), new(x + step * 3, y + step)
        });
        return path;
    }

    internal static GraphicsPath ControlPath(Control control, RectangleF bounds, float radius)
    {
        if (!IsStyled(control)) return UiTheme.CreateRoundedRectPath(bounds, radius);
        return Path(bounds);
    }

    internal static void DrawText(Graphics graphics, string text, Font font,
        Rectangle bounds, Color color, ContentAlignment alignment = ContentAlignment.MiddleCenter)
        => DrawTextCore(graphics, text, font, bounds, color, alignment, uniformGrid: false);

    internal static void DrawWrappedText(Graphics graphics, string text, Font font,
        Rectangle bounds, Color color, ContentAlignment alignment = ContentAlignment.MiddleCenter)
        => DrawTextCore(graphics, text, font, bounds, color, alignment, uniformGrid: false, wrapWords: true);

    internal static Size MeasureText(string text, Font font, Size proposed, bool wrapWords)
    {
        if (string.IsNullOrEmpty(text)) return Size.Empty;
        var pixels = font.Unit == GraphicsUnit.Pixel ? font.Size : font.SizeInPoints * 96f / 72f;
        var key = new TextKey(text, pixels, new Size(proposed.Width > 0 ? proposed.Width : int.MaxValue, int.MaxValue),
            Color.White.ToArgb(), ContentAlignment.TopLeft, false, wrapWords);
        if (MeasurementCache.TryGetValue(key, out var measured)) return measured;
        var lines = LayoutLines(key, ellipsize: false);
        var lineHeight = Math.Max(1, (int)Math.Round(Font(NativeFontSize).GetHeight(96f) * pixels / NativeFontSize));
        var previous = Rectangle.Empty;
        var content = Rectangle.Empty;
        var y = 0;
        foreach (var line in lines)
        {
            var ink = LineInkBounds(line, key);
            if (!ReferenceEquals(line, lines[0])) y += Math.Max(lineHeight, previous.Bottom - ink.Top + MinimumLineGap);
            previous = ink;
            if (ink.IsEmpty) continue;
            ink.Offset(0, y);
            content = content.IsEmpty ? ink : Rectangle.Union(content, ink);
        }
        if (MeasurementCache.Count >= TextCacheLimit) MeasurementCache.Remove(MeasurementOrder.Dequeue());
        MeasurementCache.Add(key, content.Size);
        MeasurementOrder.Enqueue(key);
        return content.Size;
    }

    internal static void DrawUniformText(Graphics graphics, string text, Font font,
        Rectangle bounds, Color color, ContentAlignment alignment = ContentAlignment.MiddleCenter)
        => DrawTextCore(graphics, text, font, bounds, color, alignment, uniformGrid: true);

    private static void DrawTextCore(Graphics graphics, string text, Font font,
        Rectangle bounds, Color color, ContentAlignment alignment, bool uniformGrid, bool wrapWords = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || string.IsNullOrEmpty(text)) return;
        var pixels = font.Unit == GraphicsUnit.Pixel ? font.Size : font.SizeInPoints * graphics.DpiY / 72f;
        var key = new TextKey(text, pixels, bounds.Size, color.ToArgb(), alignment, uniformGrid, wrapWords);
        var cached = true;
        if (!TextCache.TryGetValue(key, out var bitmap))
        {
            bitmap = RasterizeText(key);
            var bytes = (long)bitmap.Width * bitmap.Height * 4;
            cached = bytes <= TextCacheByteLimit;
            if (cached)
            {
                while (TextCache.Count >= TextCacheLimit || _textCacheBytes + bytes > TextCacheByteLimit)
                {
                    var oldest = TextCacheOrder.Dequeue();
                    var evicted = TextCache[oldest];
                    _textCacheBytes -= (long)evicted.Width * evicted.Height * 4;
                    TextCache.Remove(oldest);
                    evicted.Dispose();
                }
                TextCache.Add(key, bitmap);
                TextCacheOrder.Enqueue(key);
                _textCacheBytes += bytes;
            }
        }
        try
        {
            var state = graphics.Save();
            try
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(bitmap, bounds, 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
            }
            finally
            {
                graphics.Restore(state);
            }
        }
        finally
        {
            if (!cached) bitmap.Dispose();
        }
    }

    private readonly record struct TextKey(string Text, float Pixels, Size Size, int Color, ContentAlignment Alignment, bool UniformGrid, bool WrapWords);
    private readonly record struct GlyphKey(string Text, float Pixels, int Color, bool UniformGrid);
    private sealed record Glyph(Bitmap Image, int Advance, int OffsetX, int OffsetY, Rectangle InkBounds);
    private readonly record struct GlyphRun(string Text, int Advance);

    private static Bitmap RasterizeText(TextKey key)
    {
        var lines = LayoutLines(key);
        var lineHeight = Math.Max(1, (int)Math.Round(Font(NativeFontSize).GetHeight(96f) * GlyphScale(key.Pixels, key.UniformGrid)));
        var ink = lines.Select(line => LineInkBounds(line, key)).ToArray();
        var offsets = new int[lines.Count];
        var content = Rectangle.Empty;
        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0)
                offsets[index] = offsets[index - 1] + Math.Max(lineHeight,
                    ink[index - 1].Bottom - ink[index].Top + MinimumLineGap);
            if (ink[index].IsEmpty) continue;
            var placed = ink[index];
            placed.Offset(0, offsets[index]);
            content = content.IsEmpty ? placed : Rectangle.Union(content, placed);
        }
        // Align the actual foreground, not the font's ascender/descender box.
        var y = key.Alignment is ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight
            ? -content.Top : key.Alignment is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
                ? key.Size.Height - content.Bottom : (key.Size.Height - content.Height) / 2 - content.Top;
        var result = new Bitmap(key.Size.Width, key.Size.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var destination = Graphics.FromImage(result);
            destination.InterpolationMode = InterpolationMode.NearestNeighbor;
            destination.PixelOffsetMode = PixelOffsetMode.Half;
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                var visible = ink[index];
                var x = key.Alignment is ContentAlignment.MiddleLeft or ContentAlignment.TopLeft or ContentAlignment.BottomLeft
                    ? -visible.Left : key.Alignment is ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight
                        ? key.Size.Width - visible.Right : (key.Size.Width - visible.Width) / 2 - visible.Left;
                foreach (var run in line)
                {
                    var glyph = GetGlyph(new GlyphKey(run.Text, key.Pixels, key.Color, key.UniformGrid));
                    destination.DrawImage(glyph.Image,
                        new Rectangle(x - glyph.OffsetX, y + offsets[index] - glyph.OffsetY, glyph.Image.Width, glyph.Image.Height),
                        0, 0, glyph.Image.Width, glyph.Image.Height, GraphicsUnit.Pixel);
                    x += run.Advance;
                }
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static Rectangle LineInkBounds(List<GlyphRun> line, TextKey key)
    {
        var bounds = Rectangle.Empty;
        var x = 0;
        foreach (var run in line)
        {
            var glyph = GetGlyph(new GlyphKey(run.Text, key.Pixels, key.Color, key.UniformGrid));
            if (!glyph.InkBounds.IsEmpty)
            {
                var placed = glyph.InkBounds;
                placed.Offset(x - glyph.OffsetX, -glyph.OffsetY);
                bounds = bounds.IsEmpty ? placed : Rectangle.Union(bounds, placed);
            }
            x += run.Advance;
        }
        return bounds;
    }

    private static List<List<GlyphRun>> LayoutLines(TextKey key, bool ellipsize = true)
    {
        var text = key.Text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
        var wrap = !key.UniformGrid && (key.WrapWords || text.Contains('\n'));
        var lines = new List<List<GlyphRun>>();
        foreach (var paragraph in text.Split('\n'))
        {
            var runs = new List<GlyphRun>();
            var elements = StringInfo.GetTextElementEnumerator(paragraph);
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                runs.Add(new GlyphRun(element, GetGlyph(new GlyphKey(element, key.Pixels, key.Color, key.UniformGrid)).Advance));
            }
            if (!wrap || runs.Count == 0)
            {
                lines.Add(ellipsize ? Ellipsize(runs, key) : runs);
                continue;
            }
            var start = 0;
            while (start < runs.Count)
            {
                var end = start;
                var width = 0;
                var lastSpace = -1;
                while (end < runs.Count && (width + runs[end].Advance <= key.Size.Width || end == start))
                {
                    width += runs[end].Advance;
                    if (string.IsNullOrWhiteSpace(runs[end].Text)) lastSpace = end;
                    end++;
                }
                var next = end;
                if (end < runs.Count && lastSpace > start)
                {
                    end = lastSpace;
                    next = lastSpace + 1;
                }
                var line = runs.GetRange(start, end - start);
                lines.Add(ellipsize ? Ellipsize(line, key) : line);
                start = next;
                while (start < runs.Count && string.IsNullOrWhiteSpace(runs[start].Text)) start++;
            }
        }
        return lines;
    }

    private static List<GlyphRun> Ellipsize(List<GlyphRun> runs, TextKey key)
    {
        if (LineInkBounds(runs, key).Width <= key.Size.Width) return runs;
        var ellipsis = GetGlyph(new GlyphKey("\u2026", key.Pixels, key.Color, key.UniformGrid));
        var available = key.Size.Width - ellipsis.Advance;
        if (available < 0) return [];
        var count = 0;
        var width = 0;
        while (count < runs.Count && width + runs[count].Advance <= available)
            width += runs[count++].Advance;
        var trimmed = runs.GetRange(0, count);
        trimmed.Add(new GlyphRun("\u2026", ellipsis.Advance));
        return trimmed;
    }

    private static Glyph GetGlyph(GlyphKey key)
    {
        if (GlyphCache.TryGetValue(key, out var cached)) return cached;
        var glyph = RasterizeGlyph(key);
        if (GlyphCache.Count >= GlyphCacheLimit)
        {
            var oldest = GlyphCacheOrder.Dequeue();
            GlyphCache[oldest].Image.Dispose();
            GlyphCache.Remove(oldest);
        }
        GlyphCache.Add(key, glyph);
        GlyphCacheOrder.Enqueue(key);
        return glyph;
    }

    private static Glyph RasterizeGlyph(GlyphKey key)
    {
        // Every glyph starts on the same native pixel grid. Only its cached
        // image is enlarged; layout places those images at integer positions.
        const int padding = 2;
        var scale = GlyphScale(key.Pixels, key.UniformGrid);
        var scaleX = key.UniformGrid ? scale : scale * TextWidthScale;
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces
        };
        using var measurement = new Bitmap(1, 1);
        using var metrics = Graphics.FromImage(measurement);
        Configure(metrics);
        var advance = metrics.MeasureString(key.Text, Font(NativeFontSize), PointF.Empty, format).Width;
        var lineHeight = Font(NativeFontSize).GetHeight(metrics);
        using var source = new Bitmap((int)Math.Ceiling(Math.Max(NativeFontSize, advance)) + padding * 2,
            (int)Math.Ceiling(lineHeight) + padding * 2, PixelFormat.Format32bppArgb);
        using var sourceGraphics = Graphics.FromImage(source);
        Configure(sourceGraphics);
        sourceGraphics.DrawString(key.Text, Font(NativeFontSize), Brushes.White, new PointF(padding, padding), format);
        using var mask = ScaleGlyph(source, scaleX, scale);
        var ink = GlyphInkBounds(mask);
        sourceGraphics.Clear(Color.Transparent);
        using var shadow = new SolidBrush(Color.FromArgb(180, 0, 12, 23));
        using var brush = new SolidBrush(Color.FromArgb(key.Color));
        sourceGraphics.DrawString(key.Text, Font(NativeFontSize), shadow, new PointF(padding, padding + 1), format);
        sourceGraphics.DrawString(key.Text, Font(NativeFontSize), brush, new PointF(padding, padding), format);
        var glyphAdvance = Math.Max(0, (int)Math.Round(advance * scaleX));
        var offsetX = (int)Math.Round(padding * scaleX);
        if (key.UniformGrid)
        {
            // Keep every source pixel square; use whole-cell spacing rather
            // than squeezing the glyphs to fit the compact status line.
            glyphAdvance = ink.IsEmpty ? (advance > 0 ? (int)scale * 3 : 0) : ink.Width + (int)scale;
            if (!ink.IsEmpty) offsetX = ink.Left;
        }
        return new Glyph(ScaleGlyph(source, scaleX, scale), glyphAdvance,
            offsetX, (int)Math.Round(padding * scale), ink);
    }

    private static float GlyphScale(float pixels, bool uniformGrid)
        => uniformGrid ? Math.Max(1f, (float)Math.Round(pixels / NativeFontSize)) : pixels / NativeFontSize;

    private static Bitmap ScaleGlyph(Bitmap source, float scaleX, float scaleY)
    {
        var result = new Bitmap(Math.Max(1, (int)Math.Ceiling(source.Width * scaleX)),
            Math.Max(1, (int)Math.Ceiling(source.Height * scaleY)), PixelFormat.Format32bppArgb);
        try
        {
            using var destination = Graphics.FromImage(result);
            destination.InterpolationMode = InterpolationMode.NearestNeighbor;
            destination.PixelOffsetMode = PixelOffsetMode.Half;
            destination.DrawImage(source, new RectangleF(0, 0, source.Width * scaleX, source.Height * scaleY),
                new RectangleF(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static Rectangle GlyphInkBounds(Bitmap image)
    {
        var left = image.Width;
        var top = image.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
                if (image.GetPixel(x, y).A != 0)
                {
                    left = Math.Min(left, x); top = Math.Min(top, y);
                    right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    internal static void DrawHelpGlyph(Graphics graphics, Rectangle bounds, Color color)
    {
        ReadOnlySpan<byte> rows = [0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b00000, 0b00100];
        var scale = Math.Min(bounds.Width / 7, bounds.Height / 9);
        if (scale < 1) return;
        var left = bounds.Left + (bounds.Width - 5 * scale) / 2;
        var top = bounds.Top + (bounds.Height - rows.Length * scale) / 2;
        using var brush = new SolidBrush(color);
        for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < 5; x++)
                if ((rows[y] & (1 << (4 - x))) != 0)
                    graphics.FillRectangle(brush, left + x * scale, top + y * scale, scale, scale);
    }

    internal static void DrawPanel(Graphics graphics, RoundedPanel panel)
    {
        Configure(graphics);
        graphics.Clear(UiTheme.GetParentSurfaceColor(panel));
        using var path = Path(new RectangleF(0, 0, Math.Max(1, panel.Width - 1), Math.Max(1, panel.Height - 1)));
        using var fill = new SolidBrush(panel.FillColor);
        using var edge = new Pen(panel.BorderColor);
        graphics.FillPath(fill, path);
        graphics.DrawPath(edge, path);
    }

    internal static void DrawSlider(Graphics graphics, ModernSlider slider)
    {
        Configure(graphics);
        graphics.Clear(UiTheme.GetParentSurfaceColor(slider));
        var centerY = slider.Height / 2;
        var width = Math.Max(1, slider.Width - 48);
        var ratio = slider.Maximum == slider.Minimum ? 0d
            : (double)(slider.Value - slider.Minimum) / (slider.Maximum - slider.Minimum);
        var centerX = 24 + (int)Math.Round(width * ratio);
        using var track = new SolidBrush(Border);
        using var active = new SolidBrush(Accent);
        graphics.FillRectangle(track, 24, centerY - 2, width, 4);
        graphics.FillRectangle(active, 24, centerY - 2, Math.Max(1, centerX - 24), 4);
        using var path = Path(new RectangleF(centerX - 11, centerY - 13, 22, 26));
        using var thumb = new SolidBrush(Color.FromArgb(177, 229, 255));
        using var outline = new Pen(AccentEdge);
        graphics.FillPath(thumb, path);
        graphics.DrawPath(outline, path);
        graphics.FillRectangle(active, centerX - 3, centerY - 6, 6, 12);
    }

    private sealed class PreviewFonts
    {
        private readonly PrivateFontCollection _fonts = new();
        private readonly Dictionary<float, Font> _sizes = new();
        private readonly IntPtr _memory;
        private readonly IntPtr _gdiHandle;

        internal PreviewFonts()
        {
            using var stream = typeof(PixelUiPreview).Assembly.GetManifestResourceStream("PixelPreview.Monocraft.ttf")
                ?? throw new InvalidOperationException("Missing preview font.");
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            _memory = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, _memory, bytes.Length);
            uint count = 0;
            _gdiHandle = AddFontMemResourceEx(_memory, (uint)bytes.Length, IntPtr.Zero, ref count);
            _fonts.AddMemoryFont(_memory, bytes.Length);
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                foreach (var font in _sizes.Values) font.Dispose();
                _fonts.Dispose();
                if (_gdiHandle != IntPtr.Zero) RemoveFontMemResourceEx(_gdiHandle);
                Marshal.FreeCoTaskMem(_memory);
            };
        }

        internal Font Get(float pixels)
        {
            if (!_sizes.TryGetValue(pixels, out var font))
            {
                font = new Font(_fonts.Families[0], pixels, FontStyle.Regular, GraphicsUnit.Pixel);
                _sizes.Add(pixels, font);
            }
            return font;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr font, uint length, IntPtr reserved, ref uint count);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveFontMemResourceEx(IntPtr handle);
    }
}
