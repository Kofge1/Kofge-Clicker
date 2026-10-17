using System.Diagnostics;

namespace KofgeClicker;

internal readonly record struct UiFrameSnapshot(
    bool IsActive, int PaintCount, double FramesPerSecond, double AverageMilliseconds);

internal sealed class UiFrameMetrics
{
    private readonly Queue<long> _paintTimes = new();
    private long _minimumFrameId = long.MaxValue;
    private long _lastFrameId;
    private long _lastPaintTimestamp;
    private bool _active;
    internal bool IsActive => _active;
    internal event EventHandler? Changed;

    internal void BeginSession(long firstFrameId)
    {
        _paintTimes.Clear();
        _lastPaintTimestamp = 0;
        _minimumFrameId = firstFrameId;
        _lastFrameId = firstFrameId - 1;
        _active = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void EndSession()
    {
        if (!_active)
            return;
        _active = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void RecordPaint(long frameId, long timestamp)
    {
        // Several buttons can paint the same scheduled frame. Count it only once.
        if (frameId < _minimumFrameId || frameId <= _lastFrameId)
            return;
        _lastFrameId = frameId;
        _lastPaintTimestamp = timestamp;
        _paintTimes.Enqueue(timestamp);
        while (_paintTimes.Count > 61)
            _paintTimes.Dequeue();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal UiFrameSnapshot GetSnapshot()
    {
        var fps = 0d;
        var milliseconds = 0d;
        if (_paintTimes.Count >= 4)
        {
            var elapsed = Stopwatch.GetElapsedTime(_paintTimes.Peek(), _lastPaintTimestamp).TotalSeconds;
            if (elapsed > 0)
            {
                fps = (_paintTimes.Count - 1) / elapsed;
                milliseconds = 1000d / fps;
            }
        }
        return new(_active, _paintTimes.Count, fps, milliseconds);
    }
}

internal sealed class UiFrameRateBadge : Control
{
    private readonly UiFrameMetrics _metrics = ButtonAnimationClock.Metrics;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 200 };
    private readonly Font _diagnosticFont = UiTheme.CreateFont("Segoe UI", 10.5f);
    private UiFrameSnapshot _snapshot;
    private bool _disposed;

    internal UiFrameRateBadge()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        BackColor = UiTheme.AppBackground;
        ForeColor = UiTheme.TextSoft;
        Font = _diagnosticFont;
        _metrics.Changed += OnMetricsChanged;
        _refreshTimer.Tick += OnRefreshTick;
    }

    private bool CanDisplay => !_disposed && IsHandleCreated && Visible
        && FindForm() is not { WindowState: FormWindowState.Minimized };

    private void OnMetricsChanged(object? sender, EventArgs e)
    {
        if (_disposed)
            return;
        if (!CanDisplay)
        {
            _refreshTimer.Stop();
            return;
        }
        if (!_metrics.IsActive)
        {
            _refreshTimer.Stop();
            RefreshSnapshot();
        }
        else if (!_refreshTimer.Enabled)
        {
            RefreshSnapshot();
            _refreshTimer.Start();
        }
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        if (!CanDisplay)
        {
            _refreshTimer.Stop();
            return;
        }
        RefreshSnapshot();
        if (!_snapshot.IsActive)
            _refreshTimer.Stop();
    }

    private void RefreshSnapshot()
    {
        var next = _metrics.GetSnapshot();
        if (next == _snapshot)
            return;
        _snapshot = next;
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        OnMetricsChanged(this, EventArgs.Empty);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!_disposed)
            OnMetricsChanged(this, EventArgs.Empty);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (!_disposed)
            _refreshTimer.Stop();
        base.OnHandleDestroyed(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        var russian = LocalizationService.CurrentLanguageCode == LocalizationService.RussianLanguageCode;
        var hasSample = _snapshot.FramesPerSecond > 0;
        var firstLine = hasSample ? $"UI: {_snapshot.FramesPerSecond:F0} FPS" : "UI: -- FPS";
        var idle = russian ? "покой" : "idle";
        var measuring = russian ? "замер..." : "sampling...";
        var secondLine = hasSample
            ? $"{_snapshot.AverageMilliseconds:F1} ms{(_snapshot.IsActive ? "" : " / " + idle)}"
            : _snapshot.IsActive ? measuring : idle;
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        var half = Height / 2;
        TextRenderer.DrawText(e.Graphics, firstLine, Font, new Rectangle(0, 0, Width, half), ForeColor, flags);
        TextRenderer.DrawText(e.Graphics, secondLine, Font, new Rectangle(0, half, Width, Height - half), ForeColor, flags);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _metrics.Changed -= OnMetricsChanged;
            _refreshTimer.Dispose();
            _diagnosticFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
