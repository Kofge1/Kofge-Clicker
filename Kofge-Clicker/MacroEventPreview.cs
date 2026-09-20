namespace KofgeClicker;

internal sealed class MacroEventPreview : Control
{
    private readonly Font _titleFont = UiTheme.CreateFont("Segoe UI Semibold", 12.5f, FontStyle.Bold);
    private string _title = string.Empty;
    private IReadOnlyList<string> _lines = [];

    internal MacroEventPreview()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.TextPrimary;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    internal void SetContent(string title, IReadOnlyList<string> lines)
    {
        _title = title;
        _lines = lines;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        UiTheme.ConfigureRoundedControlGraphics(e.Graphics, this);
        var bounds = new RectangleF(0.5f, 0.5f, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = UiTheme.CreateRoundedRectPath(bounds, 16f);
        using var fill = new SolidBrush(UiTheme.Surface);
        using var border = new Pen(UiTheme.BorderSoft);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        TextRenderer.DrawText(
            e.Graphics,
            _title,
            _titleFont,
            new Rectangle(20, 15, Math.Max(1, Width - 224), 25),
            UiTheme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var y = 49;
        foreach (var line in _lines.Take(4))
        {
            TextRenderer.DrawText(
                e.Graphics,
                line,
                UiTheme.BodyFont,
                new Rectangle(20, y, Width - 40, 23),
                UiTheme.TextSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            y += 25;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
        }

        base.Dispose(disposing);
    }
}
