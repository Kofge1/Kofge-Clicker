namespace KofgeClicker;

internal class SurfaceLabel : Label
{
    // Classic text rendering remains available when the pixel skin is not active.
    internal bool UseSolidPanelBackground { get; set; } = true;
    internal Point PixelTextOffset { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (PixelUiPreview.IsStyled(this))
        {
            var bounds = ClientRectangle;
            bounds.Offset(PixelTextOffset);
            var color = Enabled ? ForeColor : UiTheme.TextMuted;
            if (PixelUiPreview.UsesUniformText(this))
                PixelUiPreview.DrawUniformText(e.Graphics, Text, Font, bounds, color, TextAlign);
            else if (PixelUiPreview.Enabled && Height > Font.Height * 1.6f)
                PixelUiPreview.DrawWrappedText(e.Graphics, Text, Font, bounds, color, TextAlign);
            else PixelUiPreview.DrawText(e.Graphics, Text, Font, bounds, color, TextAlign);
            return;
        }
        base.OnPaint(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (UseSolidPanelBackground && BackColor.A == 0 && BackgroundImage is null
            && Parent is RoundedPanel panel && panel.ContainsSolidSurface(Bounds))
        {
            using var brush = new SolidBrush(panel.FillColor);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
            return;
        }

        base.OnPaintBackground(e);
    }
}
