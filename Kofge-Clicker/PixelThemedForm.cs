namespace KofgeClicker;

public class PixelThemedForm : Form
{
    protected override bool ShowWithoutActivation => PixelUiPreview.IsSnapshotRendering || base.ShowWithoutActivation;
    public PixelThemedForm()
    {
        if (PixelUiPreview.Enabled) Font = UiTheme.BodyFont;
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (PixelUiPreview.Enabled && e.Control is not null) PixelUiPreview.ApplyTree(e.Control);
    }

    protected override void OnLoad(EventArgs e)
    {
        if (PixelUiPreview.Enabled) PixelUiPreview.ApplyTree(this);
        base.OnLoad(e);
    }

    protected override void OnShown(EventArgs e)
    {
        if (PixelUiPreview.IsSnapshotRendering) return;
        base.OnShown(e);
    }
}
