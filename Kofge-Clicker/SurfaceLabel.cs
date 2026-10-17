namespace KofgeClicker;

internal class SurfaceLabel : Label
{
    // Main-window labels opt in by using this type; dialogs keep ordinary Label.
    internal bool UseSolidPanelBackground { get; set; } = true;

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
