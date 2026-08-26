using System.Diagnostics;

namespace KofgeClicker;

internal sealed class ThemedNotificationToast : IDisposable
{
    private const int FadeInDurationMs = 160;
    private const int VisibleDurationMs = 1100;
    private const int FadeOutDurationMs = 220;

    private readonly Form _owner;
    private readonly ThemedNotificationToastWindow _window = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly Stopwatch _watch = new();
    private NotificationPhase _phase;
    private bool _disposed;

    internal ThemedNotificationToast(Form owner)
    {
        _owner = owner;
        _timer.Tick += OnAnimationTick;
    }

    internal void Show(string message, string? highlightedText = null)
    {
        if (_disposed || _owner.IsDisposed)
        {
            return;
        }

        if (_owner.InvokeRequired)
        {
            _owner.BeginInvoke(new Action(() => Show(message, highlightedText)));
            return;
        }

        _window.SetMessage(message, highlightedText);
        _ = _window.Handle;
        PositionAtScreenCorner();
        _window.Opacity = 0.01;
        if (!_window.Visible)
        {
            _window.Show();
        }

        _phase = NotificationPhase.FadeIn;
        _watch.Restart();
        _timer.Start();
    }

    internal void WarmUp(string message, string? highlightedText = null)
    {
        if (_disposed || _owner.IsDisposed)
        {
            return;
        }

        _window.SetMessage(message, highlightedText);
        _ = _window.Handle;
        _window.Opacity = 0;
    }

    private void PositionAtScreenCorner()
    {
        var screen = _owner.Visible
            ? Screen.FromControl(_owner)
            : Screen.FromPoint(Cursor.Position);
        var area = screen.WorkingArea;
        _window.Location = new Point(
            area.Right - _window.Width - 18,
            area.Bottom - _window.Height - 18);
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        switch (_phase)
        {
            case NotificationPhase.FadeIn:
                var fadeIn = Math.Clamp(_watch.Elapsed.TotalMilliseconds / FadeInDurationMs, 0, 1);
                _window.Opacity = Math.Clamp(1 - Math.Pow(1 - fadeIn, 3), 0.01, 1);
                if (fadeIn >= 1)
                {
                    _window.Opacity = 1;
                    _phase = NotificationPhase.Visible;
                    _watch.Restart();
                }
                break;

            case NotificationPhase.Visible:
                if (_watch.ElapsedMilliseconds >= VisibleDurationMs)
                {
                    _phase = NotificationPhase.FadeOut;
                    _watch.Restart();
                }
                break;

            case NotificationPhase.FadeOut:
                var fadeOut = Math.Clamp(_watch.Elapsed.TotalMilliseconds / FadeOutDurationMs, 0, 1);
                _window.Opacity = Math.Clamp(1 - (fadeOut * fadeOut), 0, 1);
                if (fadeOut >= 1)
                {
                    HideImmediate();
                }
                break;
        }
    }

    private void HideImmediate()
    {
        _timer.Stop();
        _watch.Reset();
        _phase = NotificationPhase.Hidden;
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
        _timer.Dispose();
        _window.Dispose();
    }

    private enum NotificationPhase
    {
        Hidden,
        FadeIn,
        Visible,
        FadeOut
    }
}

internal sealed class ThemedNotificationToastWindow : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private readonly Font _titleFont = UiTheme.CreateFont("Segoe UI Semibold", 11.5f, FontStyle.Bold);
    private readonly Font _messageFont = UiTheme.CreateFont("Segoe UI", 12.5f);
    private readonly Font _highlightFont = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold);
    private string _message = string.Empty;
    private string _highlightedText = string.Empty;

    internal ThemedNotificationToastWindow()
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = UiTheme.Surface;
        DoubleBuffered = true;
        Width = 330;
        Height = 68;
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

    internal void SetMessage(string message, string? highlightedText = null)
    {
        _message = message;
        _highlightedText = highlightedText ?? string.Empty;
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
        using var path = UiTheme.CreateRoundedRectPath(bounds, 16f);
        using var fill = new SolidBrush(UiTheme.Surface);
        e.Graphics.FillPath(fill, path);
        UiTheme.DrawContinuousRoundedOutline(
            e.Graphics,
            new Rectangle(0, 0, Width, Height),
            16,
            Color.FromArgb(83, 103, 143));

        using var accentBrush = new SolidBrush(UiTheme.Accent);
        e.Graphics.FillRectangle(accentBrush, 0, 15, 3, Height - 30);

        TextRenderer.DrawText(
            e.Graphics,
            "Kofge-Clicker",
            _titleFont,
            new Rectangle(22, 8, Width - 36, 22),
            UiTheme.AccentBorder,
            TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.NoPadding
                | TextFormatFlags.NoClipping);
        var messageBounds = new Rectangle(22, 29, Width - 36, 29);
        var messageFlags = TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPadding;
        if (string.IsNullOrEmpty(_highlightedText))
        {
            TextRenderer.DrawText(
                e.Graphics,
                _message,
                _messageFont,
                messageBounds,
                UiTheme.TextPrimary,
                messageFlags | TextFormatFlags.EndEllipsis);
        }
        else
        {
            var prefixWidth = TextRenderer.MeasureText(
                e.Graphics,
                _message,
                _messageFont,
                Size.Empty,
                messageFlags).Width;
            TextRenderer.DrawText(
                e.Graphics,
                _message,
                _messageFont,
                new Rectangle(messageBounds.Left, messageBounds.Top, prefixWidth, messageBounds.Height),
                UiTheme.TextPrimary,
                messageFlags);

            const int highlightedTextGap = 6;
            var highlightedBounds = new Rectangle(
                messageBounds.Left + prefixWidth + highlightedTextGap,
                messageBounds.Top,
                Math.Max(0, messageBounds.Width - prefixWidth - highlightedTextGap),
                messageBounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                _highlightedText,
                _highlightFont,
                highlightedBounds,
                UiTheme.AccentBorder,
                messageFlags | TextFormatFlags.EndEllipsis);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _messageFont.Dispose();
            _highlightFont.Dispose();
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
            16f);
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}
