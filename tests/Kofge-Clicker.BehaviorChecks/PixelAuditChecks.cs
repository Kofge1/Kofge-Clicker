using System.Collections;
using System.Drawing.Imaging;
using System.Reflection;
using KofgeClicker;

internal static class PixelAuditChecks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly Type Preview = typeof(MainForm).Assembly.GetType("KofgeClicker.PixelUiPreview", true)!;
    private static Font Font(float size) => (Font)Preview.GetMethod("Font", Members)!.Invoke(null, [size])!;

    internal static void Measurement()
    {
        var measure = Preview.GetMethod("MeasureText", Members)!;
        const string text = "Очень длинное название профиля / A very long profile name";
        Size Measure(Size size, bool wrap) => (Size)measure.Invoke(null, [text, Font(20), size, wrap])!;
        var full = Measure(Size.Empty, false);
        var constrained = Measure(new Size(100, 100), false);
        Assert(full.Width > 100 && constrained.Width == full.Width && constrained.Height == full.Height,
            "Text measurement silently ellipsizes the string before font fitting.");
        var wrapped = Measure(new Size(100, 1000), true);
        Assert(wrapped.Width <= 100 && wrapped.Height > full.Height, "Word-wrapped measurement no longer respects its width.");
    }

    internal static void NestedMenus()
    {
        using var menu = new ContextMenuStrip();
        var profiles = new ToolStripMenuItem("Профили");
        profiles.DropDownItems.Add(new ToolStripMenuItem("Игровой профиль") { Checked = true });
        menu.Items.Add(profiles);
        Preview.GetMethod("ApplyMenu", Members)!.Invoke(null, [menu]);
        Preview.GetMethod("StyleMenuItems", Members)!.Invoke(null, [menu.Items, menu.Font]);
        var nested = profiles.DropDown;
        Assert(nested.Renderer.GetType().Name == "PixelMenuRenderer" && nested.BackColor == menu.BackColor
            && nested.Font.FontFamily.Name == "Monocraft" && !nested.DropShadowEnabled,
            "The nested profile menu retained Windows styling.");
        var clicks = 0;
        profiles.DropDownItems[0].Click += (_, _) => clicks++;
        profiles.DropDownItems[0].PerformClick();
        Assert(clicks == 1, "Nested menu styling changed profile activation.");
        var renderer = nested.Renderer;
        var extra = new ToolStripMenuItem("Ещё профили");
        extra.DropDownItems.Add("Тест");
        profiles.DropDownItems.Add(extra);
        Preview.GetMethod("StyleMenuItems", Members)!.Invoke(null, [menu.Items, menu.Font]);
        Assert(ReferenceEquals(renderer, nested.Renderer) && extra.DropDown.Renderer.GetType().Name == "PixelMenuRenderer",
            "Repeated menu styling replaces renderers or misses dynamically added submenus.");
    }

    internal static void CacheMemory()
    {
        var draw = Preview.GetMethod("DrawText", Members)!;
        using var target = new Bitmap(1200, 1000, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(target);
        for (var index = 0; index < 8; index++)
            draw.Invoke(null, [graphics, "Large cached surface " + index, Font(20),
                new Rectangle(Point.Empty, target.Size), Color.White, ContentAlignment.TopLeft]);
        var cache = (IDictionary)Preview.GetField("TextCache", Members)!.GetValue(null)!;
        long bytes = 0;
        foreach (Bitmap bitmap in cache.Values) bytes += (long)bitmap.Width * bitmap.Height * 4;
        Assert(bytes <= 16 * 1024 * 1024, $"Text cache retains {bytes} bytes despite its entry-count limit.");
        Assert(cache.Count <= 256, "The text bitmap cache lost its entry limit.");
        Assert((long)Preview.GetField("_textCacheBytes", Members)!.GetValue(null)! == bytes,
            "Bitmap eviction left an inaccurate cache memory counter.");
        var count = cache.Count;
        draw.Invoke(null, [graphics, "Oversized uncached surface", Font(20), new Rectangle(0, 0, 2200, 2000),
            Color.White, ContentAlignment.TopLeft]);
        Assert(cache.Count == count && (long)Preview.GetField("_textCacheBytes", Members)!.GetValue(null)! == bytes,
            "An oversized bitmap was retained or unnecessarily evicted reusable entries.");
    }

    internal static void ThemeLifecycle()
    {
        using var parent = new Panel();
        using var legacyFont = new Font("Segoe UI", 12f);
        using var input = new TextBox { Text = "Имя профиля", Font = legacyFont, Enabled = false };
        using var toggle = new ToggleSwitchCheckBox { Checked = true, Enabled = true };
        parent.Controls.AddRange([input, toggle]);
        var apply = Preview.GetMethod("ApplyTree", Members)!;
        apply.Invoke(null, [parent]);
        var font = input.Font;
        apply.Invoke(null, [parent]);
        Assert(ReferenceEquals(font, input.Font) && input.Font.FontFamily.Name == "Monocraft",
            "Repeated theming allocates a replacement font.");
        Assert(!input.Enabled && toggle.Enabled && toggle.Checked && input.Text == "Имя профиля",
            "The pixel theme modified functional control state.");
        Assert(legacyFont.GetHeight() > 0, "The theme disposed a caller-owned font.");
        using (var temporary = UiTheme.CreateFont("Segoe UI", 14f))
            Assert(temporary.FontFamily.Name == "Monocraft", "Font creation lost the pixel theme.");
        Assert(Font(14 * 96f / 72f).GetHeight() > 0, "Disposing a temporary font poisoned the shared font cache.");
        var dynamicInput = new TextBox { Text = "Dynamic", Font = legacyFont };
        parent.Controls.Add(dynamicInput);
        Assert(dynamicInput.Font.FontFamily.Name == "Monocraft" && dynamicInput.Text == "Dynamic",
            "Dynamically added controls bypassed the pixel theme.");
    }

    internal static void GraphicsState()
    {
        using var bitmap = new Bitmap(240, 80);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.TranslateTransform(3, 4);
        graphics.SetClip(new Rectangle(10, 10, 90, 40));
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        using var originalTransform = graphics.Transform;
        var transform = originalTransform.Elements;
        var clip = graphics.ClipBounds;
        var mode = graphics.InterpolationMode;
        var draw = Preview.GetMethod("DrawText", Members)!;
        draw.Invoke(null, [graphics, "Кликер / Clicker", Font(20), new Rectangle(0, 0, 220, 60), Color.White, ContentAlignment.TopLeft]);
        using var currentTransform = graphics.Transform;
        Assert(currentTransform.Elements.SequenceEqual(transform) && graphics.ClipBounds == clip && graphics.InterpolationMode == mode,
            "Pixel text changed the caller's graphics state.");
        var foreground = 0;
        for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
            {
                var alpha = bitmap.GetPixel(x, y).A;
                if (alpha == 0) continue;
                foreground++;
                Assert(new Rectangle(13, 14, 90, 40).Contains(x, y), "Pixel text escaped the supplied graphics clip.");
            }
        Assert(foreground > 0, "The clipped text test did not render any pixels.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
