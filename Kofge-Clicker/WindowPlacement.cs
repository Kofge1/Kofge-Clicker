namespace KofgeClicker;

internal static class WindowPlacement
{
    internal static void ClampToWorkingArea(Form form)
    {
        var bounds = form.Bounds;
        var area = Screen.FromRectangle(bounds).WorkingArea;
        if (area.Width <= 0 || area.Height <= 0)
        {
            area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1200, 800);
        }

        var left = bounds.Width <= area.Width
            ? Math.Clamp(bounds.Left, area.Left, area.Right - bounds.Width)
            : area.Left;
        var top = bounds.Height <= area.Height
            ? Math.Clamp(bounds.Top, area.Top, area.Bottom - bounds.Height)
            : area.Top;

        if (left == bounds.Left && top == bounds.Top)
        {
            return;
        }

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(left, top);
    }
}
