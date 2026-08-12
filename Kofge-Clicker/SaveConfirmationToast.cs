using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace KofgeClicker;

internal sealed class SaveConfirmationToast : IDisposable
{
    private const int FadeInDurationMs = 180;
    private const int VisibleDurationMs = 1000;
    private const int FadeOutDurationMs = 220;

    private readonly Form _owner;
    private readonly SaveConfirmationToastWindow _window = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 15 };
    private readonly Stopwatch _phaseWatch = new();
    private ToastPhase _phase;
    private bool _disposed;

    internal SaveConfirmationToast(Form owner)
    {
        _owner = owner;
        _animationTimer.Tick += OnAnimationTick;
        _owner.Move += OnOwnerChanged;
        _owner.Resize += OnOwnerChanged;
        _owner.VisibleChanged += OnOwnerChanged;
    }

    internal void Show(Control anchor, string message)
    {
        if (_disposed || anchor.IsDisposed || !_owner.Visible || _owner.WindowState == FormWindowState.Minimized)
        {
            return;
        }

        _window.SetMessage(message);
        _ = _window.Handle;
        PositionAbove(anchor);
        _window.Opacity = 0.01;
        if (!_window.Visible)
        {
            _window.Show();
        }

        _phase = ToastPhase.FadeIn;
        _phaseWatch.Restart();
        _animationTimer.Start();
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        switch (_phase)
        {
            case ToastPhase.FadeIn:
                var fadeInProgress = Math.Clamp(_phaseWatch.Elapsed.TotalMilliseconds / FadeInDurationMs, 0, 1);
                _window.Opacity = Math.Clamp(1 - Math.Pow(1 - fadeInProgress, 3), 0.01, 1);
                if (fadeInProgress >= 1)
                {
                    _window.Opacity = 1;
                    _phase = ToastPhase.Visible;
                    _phaseWatch.Restart();
                }
                break;

            case ToastPhase.Visible:
                if (_phaseWatch.ElapsedMilliseconds >= VisibleDurationMs)
                {
                    _phase = ToastPhase.FadeOut;
                    _phaseWatch.Restart();
                }
                break;

            case ToastPhase.FadeOut:
                var fadeOutProgress = Math.Clamp(_phaseWatch.Elapsed.TotalMilliseconds / FadeOutDurationMs, 0, 1);
                _window.Opacity = Math.Clamp(1 - (fadeOutProgress * fadeOutProgress), 0, 1);
                if (fadeOutProgress >= 1)
                {
                    HideImmediate();
                }
                break;
        }
    }

    private void PositionAbove(Control anchor)
    {
        var anchorBounds = new Rectangle(anchor.PointToScreen(Point.Empty), anchor.Size);
        var workingArea = Screen.FromControl(anchor).WorkingArea;
        var x = anchorBounds.Left + ((anchorBounds.Width - _window.Width) / 2);
        var y = anchorBounds.Top - _window.Height - 10;
        x = Math.Clamp(x, workingArea.Left + 8, Math.Max(workingArea.Left + 8, workingArea.Right - _window.Width - 8));
        y = Math.Clamp(y, workingArea.Top + 8, Math.Max(workingArea.Top + 8, workingArea.Bottom - _window.Height - 8));
        _window.Location = new Point(x, y);
    }

    private void OnOwnerChanged(object? sender, EventArgs e)
    {
        if (!_owner.Visible || _owner.WindowState == FormWindowState.Minimized || _window.Visible)
        {
            HideImmediate();
        }
    }

    private void HideImmediate()
    {
        _animationTimer.Stop();
        _phaseWatch.Reset();
        _phase = ToastPhase.Hidden;
        if (_window.Visible)
        {
            _window.Hide();
        }

        _window.Opacity = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        HideImmediate();
        _owner.Move -= OnOwnerChanged;
        _owner.Resize -= OnOwnerChanged;
        _owner.VisibleChanged -= OnOwnerChanged;
        _animationTimer.Dispose();
        _window.Dispose();
    }

    private enum ToastPhase
    {
        Hidden,
        FadeIn,
        Visible,
        FadeOut
    }
}

internal sealed class SaveConfirmationToastWindow : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private readonly Font _messageFont = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold);
    private string _message = string.Empty;

    internal SaveConfirmationToastWindow()
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = UiTheme.Surface;
        DoubleBuffered = true;
        Width = 254;
        Height = 48;
        Opacity = 0;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    internal void SetMessage(string message)
    {
        _message = message;
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.TryEnableSmallRoundedCorners(Handle);
        ApplyRoundedRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiTheme.ConfigureGraphics(e.Graphics);
        e.Graphics.Clear(UiTheme.Surface);
        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = UiTheme.CreateRoundedRectPath(bounds, 15.5f);
        using var fill = new SolidBrush(UiTheme.Surface);
        e.Graphics.FillPath(fill, path);
        UiTheme.DrawContinuousRoundedOutline(
            e.Graphics,
            new Rectangle(0, 0, Width, Height),
            16,
            Color.FromArgb(83, 103, 143));

        using var checkPen = new Pen(Color.FromArgb(91, 202, 120), 2.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawLines(checkPen, [new PointF(19, 24), new PointF(25, 30), new PointF(36, 18)]);

        TextRenderer.DrawText(
            e.Graphics,
            _message,
            _messageFont,
            new Rectangle(49, 0, Width - 61, Height),
            UiTheme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _messageFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 1 || Height <= 1)
        {
            return;
        }

        using var path = UiTheme.CreateRoundedRectPath(
            new RectangleF(0, 0, Width - 0.01f, Height - 0.01f),
            15.5f);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}
