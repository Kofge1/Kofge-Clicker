using System.Reflection;
using KofgeClicker;

internal static class SurfaceLabelChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(AccentButton).Assembly;

    internal static Label Create(string type = "SurfaceLabel") =>
        (Label)Activator.CreateInstance(App.GetType("KofgeClicker." + type)!, true)!;

    internal static void SetFast(Label label, bool value) => App.GetType("KofgeClicker.SurfaceLabel")!
        .GetProperty("UseSolidPanelBackground", Members)!.SetValue(label, value);

    internal static void RenderingAndFallback()
    {
        using var window = new Form { BackColor = UiTheme.CardOuter, ShowInTaskbar = false };
        using var panel = new RoundedPanel
        {
            Size = new Size(300, 180), UseAntialiasedEdges = true, CacheBackgroundDrawing = true
        };
        window.Controls.Add(panel);
        using var label = Create();
        label.Text = "Surface label";
        label.Font = UiTheme.BodyFont;
        label.BackColor = Color.Transparent;
        label.ForeColor = UiTheme.TextPrimary;
        panel.Controls.Add(label);
        bool IsSolid() => (bool)typeof(RoundedPanel).GetMethod("ContainsSolidSurface", Members)!
            .Invoke(panel, [label.Bounds])!;
        void Compare()
        {
            SetFast(label, false);
            using var expected = Capture(panel);
            SetFast(label, true);
            using var actual = Capture(panel);
            EqualPixels(expected, actual);
        }

        label.Bounds = new Rectangle(20, 10, 240, 32);
        Assert(IsSolid(), "A title inside the flat surface did not use the fast path.");
        Compare();
        label.Enabled = false;
        Compare();
        label.Enabled = true;
        label.RightToLeft = RightToLeft.Yes;
        Compare();
        label.RightToLeft = RightToLeft.No;
        panel.FillColor = Color.FromArgb(45, 75, 100);
        Compare();

        foreach (var bounds in new[]
        {
            new Rectangle(0, 0, 80, 24), new Rectangle(270, 0, 30, 35),
            new Rectangle(15, 150, 140, 25), new Rectangle(-5, 30, 60, 30)
        })
        {
            label.Bounds = bounds;
            Assert(!IsSolid(), "A label touching a corner, border or shadow used a solid background.");
            Compare();
        }

        label.Bounds = new Rectangle(25, 40, 200, 36);
        panel.DrawShadow = false;
        Compare();
        label.BackColor = Color.FromArgb(100, 80, 90, 110);
        Compare();
        label.BackColor = Color.Transparent;
        using var tile = new Bitmap(8, 8);
        using (var canvas = Graphics.FromImage(tile)) canvas.Clear(Color.Orange);
        label.BackgroundImage = tile;
        Compare();
        label.BackgroundImage = null;
        panel.CacheBackgroundDrawing = false;
        Assert(!IsSolid(), "An unrelated uncached panel used the fast path.");
        Compare();
        panel.CacheBackgroundDrawing = true;
        panel.FillColor = Color.FromArgb(100, 45, 75, 100);
        Assert(!IsSolid(), "A translucent panel used a solid background.");
        Compare();

        panel.FillColor = UiTheme.CardInner;
        panel.Controls.Remove(label);
        using var wrapped = Create("CompactWrappedLabel");
        wrapped.Bounds = new Rectangle(25, 40, 200, 70);
        wrapped.BackColor = Color.Transparent;
        wrapped.Text = "First line\nSecond line";
        wrapped.Font = UiTheme.BodyFont;
        panel.Controls.Add(wrapped);
        SetFast(wrapped, false);
        using var directWrapped = Capture(panel);
        SetFast(wrapped, true);
        using var fastWrapped = Capture(panel);
        EqualPixels(directWrapped, fastWrapped);
    }

    private static Bitmap Capture(Control control)
    {
        var image = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(image, control.ClientRectangle);
        return image;
    }

    private static void EqualPixels(Bitmap expected, Bitmap actual)
    {
        for (var y = 0; y < expected.Height; y++)
            for (var x = 0; x < expected.Width; x++)
                Assert(expected.GetPixel(x, y) == actual.GetPixel(x, y), $"Label rendering differs at {x},{y}.");
    }

    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
