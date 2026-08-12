using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KofgeClicker;

public sealed class ClickTestSurface : Control
{
    private readonly Queue<long> _recentClickTicks = [];
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly Font _titleFont;
    private readonly Font _instructionFont;
    private readonly Font _metricFont;
    private readonly Font _metricLabelFont;
    private long _flashUntilTicks;
    private int _currentCps;
    private bool _hovered;

    public string TitleText { get; set; } = string.Empty;
    public string InstructionText { get; set; } = string.Empty;
    public string ClicksText { get; set; } = string.Empty;
    public string CpsText { get; set; } = string.Empty;
    public int TotalClicks { get; private set; }

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
        TotalClicks = 0;
        _currentCps = 0;
        _recentClickTicks.Clear();
        _flashUntilTicks = 0;
        _refreshTimer.Stop();
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        var wasStopped = !_refreshTimer.Enabled;
        var now = Stopwatch.GetTimestamp();
        TotalClicks++;
        _recentClickTicks.Enqueue(now);
        PruneOldClicks(now);
        _currentCps = _recentClickTicks.Count;
        _flashUntilTicks = now + (Stopwatch.Frequency / 10);
        _refreshTimer.Start();
        if (wasStopped)
        {
            Invalidate();
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
        DrawMetric(e.Graphics, _currentCps.ToString(), CpsText, new Rectangle(24 + columnWidth, 126, columnWidth, 72));
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
        var now = Stopwatch.GetTimestamp();
        PruneOldClicks(now);
        _currentCps = _recentClickTicks.Count;

        if (_recentClickTicks.Count == 0 && now >= _flashUntilTicks)
        {
            _refreshTimer.Stop();
        }

        Invalidate();
    }

    private void PruneOldClicks(long now)
    {
        var cutoff = now - Stopwatch.Frequency;
        while (_recentClickTicks.Count > 0 && _recentClickTicks.Peek() <= cutoff)
        {
            _recentClickTicks.Dequeue();
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
