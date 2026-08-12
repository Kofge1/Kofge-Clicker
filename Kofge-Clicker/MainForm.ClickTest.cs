namespace KofgeClicker;

public sealed partial class MainForm
{
    private void UpdateClickTestButton()
    {
        if (_clickTestSurface is null)
        {
            return;
        }

        _clickTestSurface.AcceptedButton = NormalizeClickButton(_settings.ClickButton) == "Right"
            ? MouseButtons.Right
            : MouseButtons.Left;
    }
}
