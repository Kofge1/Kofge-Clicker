using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KofgeClicker;

internal sealed class HoverTooltipService : IDisposable
{
    private const int HoverDelayMs = 1000;
    private const int FadeInDurationMs = 250;
    private const int FadeOutDurationMs = 140;

    private readonly Form _owner;
    private readonly HoverTooltipWindow _popup = new();
    private readonly Dictionary<Control, string> _messages = [];
    private readonly System.Windows.Forms.Timer _delayTimer = new() { Interval = HoverDelayMs };
    private readonly System.Windows.Forms.Timer _fadeTimer = new() { Interval = 15 };
    private readonly Stopwatch _fadeWatch = new();
    private Control? _pendingControl;
    private Control? _visibleForControl;
    private double _fadeStartOpacity;
    private double _fadeTargetOpacity;
    private int _fadeDurationMs;
    private bool _disposed;

    internal HoverTooltipService(Form owner)
    {
        _owner = owner;
        _delayTimer.Tick += OnDelayElapsed;
        _fadeTimer.Tick += OnFadeTick;
        _owner.VisibleChanged += OnOwnerVisibleChanged;
        _owner.Resize += OnOwnerResize;
        _owner.Move += OnOwnerMoved;
    }

    internal void SetTooltip(Control control, string message)
    {
        if (_disposed || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_messages.ContainsKey(control))
        {
            _messages[control] = message;
            return;
        }

        _messages.Add(control, message);
        control.MouseEnter += OnTargetMouseEnter;
        control.MouseLeave += OnTargetMouseLeave;
        control.MouseDown += OnTargetMouseDown;
        control.Disposed += OnTargetDisposed;
    }

    private void OnTargetMouseEnter(object? sender, EventArgs e)
    {
        if (sender is not Control control || !_messages.ContainsKey(control))
        {
            return;
        }

        HideImmediate();
        _pendingControl = control;
        _delayTimer.Start();
    }

    private void OnTargetMouseLeave(object? sender, EventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (_pendingControl == control)
        {
            _delayTimer.Stop();
            _pendingControl = null;
        }

        if (_visibleForControl == control)
        {
            BeginFade(0, FadeOutDurationMs);
        }
    }

    private void OnTargetMouseDown(object? sender, MouseEventArgs e)
    {
        HideImmediate();
    }

    private void OnTargetDisposed(object? sender, EventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (_pendingControl == control || _visibleForControl == control)
        {
            HideImmediate();
        }

        _messages.Remove(control);
    }

    private void OnDelayElapsed(object? sender, EventArgs e)
    {
        _delayTimer.Stop();
        var control = _pendingControl;
        _pendingControl = null;
        if (control is null
            || control.IsDisposed
            || !_messages.TryGetValue(control, out var message)
            || !IsPointerOver(control)
            || !_owner.Visible
            || _owner.WindowState == FormWindowState.Minimized)
        {
            return;
        }

        _popup.SetMessage(message);
        _ = _popup.Handle;
        PositionPopup(control);
        _popup.Opacity = 0.01;
        _popup.Show();
        _visibleForControl = control;
        BeginFade(1, FadeInDurationMs);
    }

    private void PositionPopup(Control control)
    {
        var anchorLocation = control.PointToScreen(Point.Empty);
        var anchor = new Rectangle(anchorLocation, control.Size);
        var workingArea = Screen.FromControl(control).WorkingArea;
        var x = anchor.Left + ((anchor.Width - _popup.Width) / 2);
        var y = anchor.Bottom + 10;

        if (y + _popup.Height > workingArea.Bottom)
        {
            y = anchor.Top - _popup.Height - 10;
        }

        x = Math.Clamp(x, workingArea.Left + 8, Math.Max(workingArea.Left + 8, workingArea.Right - _popup.Width - 8));
        y = Math.Clamp(y, workingArea.Top + 8, Math.Max(workingArea.Top + 8, workingArea.Bottom - _popup.Height - 8));
        _popup.Location = new Point(x, y);
    }

    private void BeginFade(double targetOpacity, int durationMs)
    {
        _fadeTimer.Stop();
        _fadeStartOpacity = _popup.Visible ? _popup.Opacity : 0;
        _fadeTargetOpacity = targetOpacity;
        _fadeDurationMs = durationMs;
        _fadeWatch.Restart();
        _fadeTimer.Start();
    }

    private void OnFadeTick(object? sender, EventArgs e)
    {
        if (!_popup.Visible)
        {
            _fadeTimer.Stop();
            return;
        }

        var progress = Math.Clamp(_fadeWatch.Elapsed.TotalMilliseconds / _fadeDurationMs, 0, 1);
        var eased = _fadeTargetOpacity > _fadeStartOpacity
            ? 1 - Math.Pow(1 - progress, 3)
            : progress;
        _popup.Opacity = Math.Clamp(
            _fadeStartOpacity + ((_fadeTargetOpacity - _fadeStartOpacity) * eased),
            0,
            1);

        if (progress < 1)
        {
            return;
        }

        _fadeTimer.Stop();
        if (_fadeTargetOpacity <= 0)
        {
            _popup.Hide();
            _visibleForControl = null;
        }
    }

    private void OnOwnerVisibleChanged(object? sender, EventArgs e)
    {
        if (!_owner.Visible)
        {
            HideImmediate();
        }
    }

    private void OnOwnerResize(object? sender, EventArgs e)
    {
        if (_owner.WindowState == FormWindowState.Minimized)
        {
            HideImmediate();
        }
    }

    private void OnOwnerMoved(object? sender, EventArgs e) => HideImmediate();

    private static bool IsPointerOver(Control control)
    {
        try
        {
            return control.ClientRectangle.Contains(control.PointToClient(Cursor.Position));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void HideImmediate()
    {
        _delayTimer.Stop();
        _fadeTimer.Stop();
        _fadeWatch.Reset();
        _pendingControl = null;
        _visibleForControl = null;
        if (_popup.Visible)
        {
            _popup.Hide();
        }

        _popup.Opacity = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        HideImmediate();
        foreach (var control in _messages.Keys.ToArray())
        {
            control.MouseEnter -= OnTargetMouseEnter;
            control.MouseLeave -= OnTargetMouseLeave;
            control.MouseDown -= OnTargetMouseDown;
            control.Disposed -= OnTargetDisposed;
        }

        _messages.Clear();
        _owner.VisibleChanged -= OnOwnerVisibleChanged;
        _owner.Resize -= OnOwnerResize;
        _owner.Move -= OnOwnerMoved;
        _delayTimer.Dispose();
        _fadeTimer.Dispose();
        _popup.Dispose();
    }
}

internal sealed class HoverTooltipWindow : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int CsDropShadow = 0x00020000;
    private const int HorizontalPadding = 16;
    private const int VerticalPadding = 13;
    private const int MaxTextWidth = 360;
    private readonly Font _tooltipFont = UiTheme.CreateFont("Segoe UI", 11.5f);
    private string _message = string.Empty;

    internal HoverTooltipWindow()
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(36, 43, 59);
        ForeColor = UiTheme.TextPrimary;
        DoubleBuffered = true;
        Opacity = 0;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            parameters.ClassStyle |= CsDropShadow;
            return parameters;
        }
    }

    internal void SetMessage(string message)
    {
        _message = message.Trim();
        var flags = TextFormatFlags.Left
            | TextFormatFlags.Top
            | TextFormatFlags.WordBreak
            | TextFormatFlags.NoPadding
            | TextFormatFlags.NoPrefix;
        var measured = TextRenderer.MeasureText(
            _message,
            _tooltipFont,
            new Size(MaxTextWidth, 1000),
            flags);
        Width = Math.Clamp(measured.Width + (HorizontalPadding * 2), 230, MaxTextWidth + (HorizontalPadding * 2));
        measured = TextRenderer.MeasureText(
            _message,
            _tooltipFont,
            new Size(Width - (HorizontalPadding * 2), 1000),
            flags);
        Height = Math.Max(48, measured.Height + (VerticalPadding * 2));
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.TryEnableSmallRoundedCorners(Handle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var borderPen = new Pen(Color.FromArgb(83, 103, 143), 1f);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        var textBounds = new Rectangle(
            HorizontalPadding,
            VerticalPadding,
            Width - (HorizontalPadding * 2),
            Height - (VerticalPadding * 2));
        TextRenderer.DrawText(
            e.Graphics,
            _message,
            _tooltipFont,
            textBounds,
            ForeColor,
            TextFormatFlags.Left
                | TextFormatFlags.Top
                | TextFormatFlags.WordBreak
                | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tooltipFont.Dispose();
        }

        base.Dispose(disposing);
    }
}
