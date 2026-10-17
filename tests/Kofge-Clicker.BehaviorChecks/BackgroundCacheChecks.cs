using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using KofgeClicker;

internal static class BackgroundCacheChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo Cache = typeof(RoundedPanel).GetField("_backgroundDrawing", Members)!;

    internal static void RenderingAndReuse()
    {
        using var parent = new Panel { BackColor = UiTheme.CardOuter };
        using var panel = new RoundedPanel
        {
            Size = new Size(180, 120), UseAntialiasedEdges = true, FillColor = UiTheme.CardInner
        };
        parent.Controls.Add(panel);
        Assert(!panel.CacheBackgroundDrawing, "Caching was enabled for unrelated panels by default.");
        foreach (var shadow in new[] { false, true })
        {
            panel.DrawShadow = shadow;
            panel.CacheBackgroundDrawing = false;
            using var direct = Render(panel);
            panel.CacheBackgroundDrawing = true;
            using var cached = Render(panel);
            EqualPixels(direct, cached);
            var surface = Cached(panel);
            using var repeated = Render(panel);
            Assert(ReferenceEquals(surface, Cached(panel)), "An unchanged surface was rebuilt.");
            EqualPixels(direct, repeated);
            // Transparent children request a clipped, translated parent paint.
            using var clipped = Render(panel, new Rectangle(37, 26, 75, 46));
            panel.CacheBackgroundDrawing = false;
            using var directClip = Render(panel, new Rectangle(37, 26, 75, 46));
            EqualPixels(directClip, clipped);
        }
    }

    internal static void InvalidationDpiAndDisposal()
    {
        using var parent = new Panel { BackColor = UiTheme.CardOuter };
        using var panel = new RoundedPanel
        {
            Size = new Size(180, 120), UseAntialiasedEdges = true, CacheBackgroundDrawing = true
        };
        parent.Controls.Add(panel);
        var dpi = 96f;
        Action[] changes =
        [
            () => panel.Size = new Size(200, 140),
            () => panel.FillColor = Color.FromArgb(45, 80, 110),
            () => panel.BorderColor = Color.Orange,
            () => panel.Radius = 15,
            () => panel.DrawShadow = false,
            () => parent.BackColor = Color.FromArgb(25, 45, 65),
            () => dpi = 144f,
            () => dpi = 192f,
            () => dpi = 96f
        ];
        foreach (var change in changes)
        {
            using var initial = Render(panel, dpi: dpi);
            var previous = Cached(panel);
            change();
            using var updated = Render(panel, dpi: dpi);
            Assert(!ReferenceEquals(previous, Cached(panel)), "A changed surface reused stale pixels.");
            AssertDisposed(previous);
            panel.CacheBackgroundDrawing = false;
            using var expected = Render(panel, dpi: dpi);
            EqualPixels(expected, updated);
            panel.CacheBackgroundDrawing = true;
        }

        using (Render(panel)) { }
        var last = Cached(panel);
        panel.CacheBackgroundDrawing = false;
        Assert(Cache.GetValue(panel) is null, "Disabling the cache retained a bitmap.");
        AssertDisposed(last);
        panel.CacheBackgroundDrawing = true;
        using (Render(panel)) { }
        panel.UseAntialiasedEdges = false;
        using (Render(panel)) { }
        Assert(Cache.GetValue(panel) is null, "Legacy painting retained a cached surface.");
        panel.UseAntialiasedEdges = true;
        using (Render(panel)) { }
        last = Cached(panel);
        panel.Dispose();
        Assert(Cache.GetValue(panel) is null, "Disposal retained a bitmap.");
        AssertDisposed(last);
    }

    internal static void ChildControlsStayLive()
    {
        using var window = new TestWindow();
        using var panel = new RoundedPanel
        {
            Size = new Size(300, 140), UseAntialiasedEdges = true, CacheBackgroundDrawing = true
        };
        using var label = new Label
        {
            Bounds = new Rectangle(20, 20, 200, 30), Text = "AAAA", BackColor = Color.Transparent
        };
        using var button = new AccentButton
        {
            Bounds = new Rectangle(20, 65, 150, 38), Text = "Test", AnimateInteractions = true
        };
        window.Controls.Add(panel);
        panel.Controls.Add(label);
        panel.Controls.Add(button);
        window.Show();
        Application.DoEvents();
        using var before = new Bitmap(panel.Width, panel.Height);
        panel.DrawToBitmap(before, panel.ClientRectangle);
        var surface = Cached(panel);
        label.Text = "BBBB";
        using var after = new Bitmap(panel.Width, panel.Height);
        panel.DrawToBitmap(after, panel.ClientRectangle);
        Assert(ReferenceEquals(surface, Cached(panel)), "Changing child text rebuilt the panel surface.");
        var textChanged = false;
        for (var y = label.Top; y < label.Bottom; y++)
            for (var x = label.Left; x < label.Right; x++)
                textChanged |= before.GetPixel(x, y) != after.GetPixel(x, y);
        Assert(textChanged, "Child text was frozen in the cached surface.");
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        button.PerformClick();
        Assert(clicks == 1, "Caching interfered with button input.");
        window.Hide();
        window.Show();
        Application.DoEvents();
        Assert(panel.Visible && button.Visible, "Cached controls did not return after hiding.");
        surface = Cached(panel);
        typeof(Control).GetMethod("RecreateHandle", Members)!.Invoke(panel, null);
        AssertDisposed(surface);
        panel.DrawToBitmap(after, panel.ClientRectangle);
        Assert(!ReferenceEquals(surface, Cached(panel)), "Handle recreation retained a stale surface.");
    }

    internal static void TabSwitchComparison()
    {
        using var window = new TestWindow { ClientSize = new Size(1080, 510) };
        using var shell = new RoundedPanel { Dock = DockStyle.Fill, UseAntialiasedEdges = true };
        using var host = new AtomicPageHost { Bounds = new Rectangle(10, 60, 1060, 390) };
        window.Controls.Add(shell);
        shell.Controls.Add(host);
        var surfaces = new List<RoundedPanel> { shell };
        var labels = new List<Label>();
        for (var index = 0; index < 7; index++)
        {
            var page = new Panel { BackColor = UiTheme.CardOuter };
            var card = new RoundedPanel
            {
                Bounds = new Rectangle(52, 32, 956, 356), UseAntialiasedEdges = true
            };
            surfaces.Add(card);
            for (var item = 0; item < 24; item++)
            {
                var label = SurfaceLabelChecks.Create();
                label.Bounds = new Rectangle(20 + item % 3 * 300, 20 + item / 3 * 37, 280, 30);
                label.BackColor = Color.Transparent;
                label.Text = $"Page {index}, control {item}";
                card.Controls.Add(label);
                labels.Add(label);
            }
            page.Controls.Add(card);
            host.AddPage(page);
        }
        window.Show();
        Application.DoEvents();
        double Run(bool cached, bool solidLabels = false)
        {
            foreach (var surface in surfaces) surface.CacheBackgroundDrawing = cached;
            foreach (var label in labels) SurfaceLabelChecks.SetFast(label, solidLabels);
            for (var index = 0; index < 7; index++)
            {
                host.SelectedIndex = (host.SelectedIndex + 1) % host.PageCount;
                Application.DoEvents();
            }
            var watch = Stopwatch.StartNew();
            for (var index = 0; index < 35; index++)
            {
                host.SelectedIndex = (host.SelectedIndex + 1) % host.PageCount;
                Application.DoEvents();
            }
            return watch.Elapsed.TotalMilliseconds / 35;
        }
        var direct = Run(false);
        var cached = Run(true);
        var solidLabels = Run(true, true);
        var cachedAgain = Run(true);
        var solidLabelsAgain = Run(true, true);
        Console.WriteLine($"INFO tab-switch mean: direct={direct:F1} ms; cached={cached:F1}/{cachedAgain:F1} ms; solid-labels={solidLabels:F1}/{solidLabelsAgain:F1} ms (not an FPS guarantee)");
    }

    private static Bitmap Render(RoundedPanel panel, Rectangle? clip = null, float dpi = 96f)
    {
        var image = new Bitmap(panel.Width, panel.Height, PixelFormat.Format32bppPArgb);
        image.SetResolution(dpi, dpi);
        using var canvas = Graphics.FromImage(image);
        canvas.Clear(Color.Magenta);
        if (clip is { } area)
        {
            canvas.TranslateTransform(-area.X, -area.Y);
            canvas.SetClip(area);
        }
        using var e = new PaintEventArgs(canvas, clip ?? panel.ClientRectangle);
        typeof(RoundedPanel).GetMethod("OnPaintBackground", Members)!.Invoke(panel, [e]);
        typeof(RoundedPanel).GetMethod("OnPaint", Members)!.Invoke(panel, [e]);
        return image;
    }

    private static Bitmap Cached(RoundedPanel panel) => (Bitmap)Cache.GetValue(panel)!;
    private static void EqualPixels(Bitmap expected, Bitmap actual)
    {
        Assert(expected.Size == actual.Size, "Surface size changed.");
        for (var y = 0; y < expected.Height; y++)
            for (var x = 0; x < expected.Width; x++)
                Assert(expected.GetPixel(x, y) == actual.GetPixel(x, y), $"Cached pixel differs at {x},{y}.");
    }
    private static void AssertDisposed(Bitmap image)
    {
        try { image.GetPixel(0, 0); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("An old cached bitmap was not disposed.");
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
            ClientSize = new Size(320, 160);
        }
        protected override bool ShowWithoutActivation => true;
    }
}
