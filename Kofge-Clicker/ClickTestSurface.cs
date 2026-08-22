using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KofgeClicker;

public sealed class ClickTestSurface : Control
{
    private readonly object _clickSync = new();
    private readonly Queue<uint> _recentClickTimes = [];
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly Font _titleFont;
    private readonly Font _instructionFont;
    private readonly Font _metricFont;
    private readonly Font _metricLabelFont;
    private long _flashUntilTicks;
    private int _currentCps;
    private int _totalClicks;
    private int _refreshActive;
    private int _refreshStartQueued;
    private IntPtr _nativeHandle;
    private volatile bool _hovered;

    public string TitleText { get; set; } = string.Empty;
    public string InstructionText { get; set; } = string.Empty;
    public string ClicksText { get; set; } = string.Empty;
    public string CpsText { get; set; } = string.Empty;
    public int TotalClicks => Volatile.Read(ref _totalClicks);

    public ClickTestSurface()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        _titleFont = UiTheme.CreateFont("Segoe UI Semibold", 15f, FontStyle.Bold);
        _instructionFont = UiTheme.CreateFont("Segoe UI", 11.5f, FontStyle.Regular);
        _metricFont = UiTheme.CreateFont("Segoe UI Semibold", 22f, FontStyle.Bold);
        _metricLabelFont = UiTheme.CreateFont("Segoe UI", 10.5f, FontStyle.Regular);
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _refreshTimer.Tick += OnRefreshTimerTick;
    }

    public void ResetTest()
    {
        Interlocked.Exchange(ref _totalClicks, 0);
        lock (_clickSync)
        {
            _currentCps = 0;
            _recentClickTimes.Clear();
        }

        _flashUntilTicks = 0;
        _refreshTimer.Stop();
        Interlocked.Exchange(ref _refreshActive, 0);
        Interlocked.Exchange(ref _refreshStartQueued, 0);
        Invalidate();
    }

    public void RecordObservedMouseDown(string token, uint eventTime, int screenX, int screenY)
    {
        if (token is not ("LButton" or "RButton") || !ContainsScreenPoint(screenX, screenY))
        {
            return;
        }

        RecordClick(eventTime);
    }

    public void RecordGeneratedMouseDown(string buttonName)
    {
        if (buttonName is not ("Left" or "LButton" or "Right" or "RButton") || !_hovered)
        {
            return;
        }

        // The UI thread already tracks whether the pointer is over this control.
        // Avoid synchronous cursor/window queries on every generated click because
        // they would slow down the input worker and distort high-CPS measurements.
        RecordClick(unchecked((uint)Environment.TickCount));
    }

    private void RecordClick(uint eventTime)
    {
        Interlocked.Increment(ref _totalClicks);
        lock (_clickSync)
        {
            _recentClickTimes.Enqueue(eventTime);
            PruneOldClicksCore(eventTime);
            _currentCps = _recentClickTimes.Count;
        }

        Volatile.Write(ref _flashUntilTicks, Stopwatch.GetTimestamp() + (Stopwatch.Frequency / 10));
        QueueRefreshTimerStart();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Focused)
        {
            Focus();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Volatile.Write(ref _nativeHandle, Handle);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _hovered = false;
        Volatile.Write(ref _nativeHandle, IntPtr.Zero);
        base.OnHandleDestroyed(e);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            _hovered = false;
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        UiTheme.ConfigureGraphics(e.Graphics);

        var now = Stopwatch.GetTimestamp();
        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f));
        using var path = UiTheme.CreateRoundedRectPath(bounds, 17.5f);
        var fillColor = now < _flashUntilTicks
            ? Color.FromArgb(54, 74, 122)
            : UiTheme.Surface;
        var borderColor = _hovered
            ? Color.FromArgb(104, 147, 239)
            : Color.FromArgb(76, 86, 118);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(borderColor, 1f) { Alignment = PenAlignment.Center };
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        TextRenderer.DrawText(
            e.Graphics,
            TitleText,
            _titleFont,
            new Rectangle(18, 18, Width - 36, 26),
            UiTheme.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            e.Graphics,
            InstructionText,
            _instructionFont,
            new Rectangle(24, 52, Width - 48, 54),
            UiTheme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

        using var divider = new Pen(Color.FromArgb(65, 76, 108), 1f);
        e.Graphics.DrawLine(divider, 24, 112, Width - 24, 112);

        var columnWidth = (Width - 48) / 2;
        DrawMetric(e.Graphics, TotalClicks.ToString(), ClicksText, new Rectangle(24, 126, columnWidth, 72));
        DrawMetric(e.Graphics, Volatile.Read(ref _currentCps).ToString(), CpsText, new Rectangle(24 + columnWidth, 126, columnWidth, 72));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            _titleFont.Dispose();
            _instructionFont.Dispose();
            _metricFont.Dispose();
            _metricLabelFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        var now = unchecked((uint)Environment.TickCount);
        var hasRecentClicks = false;
        lock (_clickSync)
        {
            PruneOldClicksCore(now);
            _currentCps = _recentClickTimes.Count;
            hasRecentClicks = _recentClickTimes.Count > 0;
        }

        if (!hasRecentClicks && Stopwatch.GetTimestamp() >= Volatile.Read(ref _flashUntilTicks))
        {
            _refreshTimer.Stop();
            Interlocked.Exchange(ref _refreshActive, 0);
            Interlocked.Exchange(ref _refreshStartQueued, 0);
        }

        Invalidate();
    }

    private void PruneOldClicksCore(uint now)
    {
        while (_recentClickTimes.Count > 0 && unchecked(now - _recentClickTimes.Peek()) >= 1000u)
        {
            _recentClickTimes.Dequeue();
        }
    }

    private bool ContainsScreenPoint(int x, int y)
    {
        var handle = Volatile.Read(ref _nativeHandle);
        return handle != IntPtr.Zero
            && NativeMethods.IsWindowVisible(handle)
            && NativeMethods.GetWindowRect(handle, out var bounds)
            && x >= bounds.Left
            && x < bounds.Right
            && y >= bounds.Top
            && y < bounds.Bottom;
    }

    private void QueueRefreshTimerStart()
    {
        if (Volatile.Read(ref _refreshActive) != 0 || Interlocked.Exchange(ref _refreshStartQueued, 1) != 0)
        {
            return;
        }

        try
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                if (!IsDisposed)
                {
                    Interlocked.Exchange(ref _refreshActive, 1);
                    _refreshTimer.Start();
                    Invalidate();
                }

                Interlocked.Exchange(ref _refreshStartQueued, 0);
            }));
        }
        catch (InvalidOperationException)
        {
            Interlocked.Exchange(ref _refreshStartQueued, 0);
        }
    }

    private void DrawMetric(Graphics graphics, string value, string label, Rectangle bounds)
    {
        TextRenderer.DrawText(
            graphics,
            value,
            _metricFont,
            new Rectangle(bounds.Left, bounds.Top, bounds.Width, 38),
            UiTheme.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            graphics,
            label,
            _metricLabelFont,
            new Rectangle(bounds.Left, bounds.Top + 42, bounds.Width, 24),
            UiTheme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
    }
}
