using System.Diagnostics;
using System.Reflection;
using KofgeClicker;

internal static class FrameDiagnosticsChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(AccentButton).Assembly;

    internal static void CountsPaintsNotTicks()
    {
        var metrics = New("UiFrameMetrics");
        Call(metrics, "RecordPaint", 1L, 0L);
        Assert((int)Get(Snapshot(metrics), "PaintCount") == 0, "Counted a paint before a session.");
        Call(metrics, "BeginSession", 1L);
        Assert((double)Get(Snapshot(metrics), "FramesPerSecond") == 0d,
            "Reported FPS without any paints.");
        for (var index = 0; index < 6; index++)
        {
            var timestamp = (long)Math.Round(index * Stopwatch.Frequency / 60d);
            Call(metrics, "RecordPaint", index + 1L, timestamp);
            Call(metrics, "RecordPaint", index + 1L, timestamp + 1);
        }
        var snapshot = Snapshot(metrics);
        Assert((int)Get(snapshot, "PaintCount") == 6, "Counted multiple buttons as multiple frames.");
        Assert(Math.Abs((double)Get(snapshot, "FramesPerSecond") - 60d) < 0.1,
            "Incorrect paint-rate calculation.");
        Assert(Math.Abs((double)Get(snapshot, "AverageMilliseconds") - 1000d / 60d) < 0.1,
            "Incorrect frame-time calculation.");
        Call(metrics, "RecordPaint", 7L, Stopwatch.Frequency / 2);
        Assert((double)Get(Snapshot(metrics), "FramesPerSecond") < 20d,
            "A long frame did not reduce the measured rate.");
        Call(metrics, "EndSession");
        Assert(!(bool)Get(Snapshot(metrics), "IsActive"), "Counter remained active while idle.");
        Call(metrics, "BeginSession", 10L);
        Call(metrics, "RecordPaint", 9L, Stopwatch.Frequency);
        Assert((int)Get(Snapshot(metrics), "PaintCount") == 0, "Counted a stale paint from the previous session.");
        Call(metrics, "BeginSession", 100L);
        for (var index = 0; index < 90; index++)
            Call(metrics, "RecordPaint", index + 100L, (long)Math.Round(index * Stopwatch.Frequency / 60d));
        Assert((int)Get(Snapshot(metrics), "PaintCount") == 61, "The sliding sample window grew without a bound.");
        Assert(Math.Abs((double)Get(Snapshot(metrics), "FramesPerSecond") - 60d) < 0.1,
            "Sliding-window FPS changed after dropping old samples.");
        Assert((bool)Get(metrics, "IsActive"), "The lightweight active-state query disagrees with the session.");
        Call(metrics, "EndSession");
        Assert(!(bool)Get(metrics, "IsActive"), "The lightweight active-state query remained active after stopping.");
    }

    internal static void BadgeLifecycleAndRealPaints()
    {
        using var host = new TestWindow();
        using var button = new AccentButton { Text = "Test", Width = 130, AnimateInteractions = true };
        using var badge = (Control)New("UiFrameRateBadge");
        badge.SetBounds(140, 0, 118, 40);
        host.Controls.Add(button);
        host.Controls.Add(badge);
        host.Show();
        Application.DoEvents();
        var metrics = App.GetType("KofgeClicker.ButtonAnimationClock")!
            .GetProperty("Metrics", Members)!.GetValue(null)!;
        var changed = metrics.GetType().GetField("Changed", Members)!;
        var subscriptions = ((Delegate?)changed.GetValue(metrics))?.GetInvocationList().Length ?? 0;
        Call(button, "OnMouseEnter", EventArgs.Empty);
        Pump(210);
        Assert((int)Get(Snapshot(metrics), "PaintCount") > 0, "Actual button paints were not measured.");
        foreach (var line in new[] { "UI: 144 FPS", "16,7 ms / покой", "16.7 ms / idle", "sampling...", "замер..." })
        {
            var size = TextRenderer.MeasureText(line, badge.Font, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            Assert(size.Width <= badge.Width && size.Height <= badge.Height / 2,
                $"Diagnostics text is clipped: {line}, measured={size}, available={badge.Width}x{badge.Height / 2}, font={badge.Font.Size}/{badge.Font.Unit}.");
        }
        using (var preview = new Bitmap(badge.Width, badge.Height))
        {
            badge.DrawToBitmap(preview, badge.ClientRectangle);
            preview.Save(Path.Combine(Path.GetTempPath(), "Kofge-UI-FPS-preview.png"));
        }
        var timer = (System.Windows.Forms.Timer)badge.GetType().GetField("_refreshTimer", Members)!.GetValue(badge)!;
        Assert(!timer.Enabled, "Diagnostics timer kept running after animation finished.");
        Call(button, "OnMouseLeave", EventArgs.Empty);
        host.Hide();
        Pump(250);
        Assert(!timer.Enabled, "Diagnostics timer ran while the window was hidden.");
        host.Show();
        badge.Dispose();
        var afterDisposal = ((Delegate?)changed.GetValue(metrics))?.GetInvocationList().Length ?? 0;
        Assert(afterDisposal == subscriptions - 1, "Disposed diagnostics remained subscribed.");
        Call(button, "OnMouseEnter", EventArgs.Empty);
        Pump(180);
    }

    private static object New(string name) => Activator.CreateInstance(
        App.GetType("KofgeClicker." + name)!, Members, null, null, null)!;
    private static object? Call(object target, string name, params object?[] args) =>
        target.GetType().GetMethod(name, Members)!.Invoke(target, args);
    private static object Get(object target, string property) =>
        target.GetType().GetProperty(property, Members)!.GetValue(target)!;
    private static object Snapshot(object metrics) => Call(metrics, "GetSnapshot")!;
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        Application.DoEvents();
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
