namespace KofgeClicker;

public sealed partial class RoundedPanel
{
    internal bool ContainsSolidSurface(Rectangle childBounds)
    {
        if (!CacheBackgroundDrawing || !UseAntialiasedEdges || FillColor.A != 255
            || BackgroundImage is not null || BorderStyle != BorderStyle.None || AutoScroll
            || childBounds.Width <= 0 || childBounds.Height <= 0)
            return false;

        var bottomInset = DrawShadow ? 9 : 1;
        var left = 0.5f;
        var top = 0.5f;
        var right = Width - 0.5f;
        var bottom = Height - bottomInset - 0.5f;
        const float margin = 2f;
        if (childBounds.Left < left + margin || childBounds.Top < top + margin
            || childBounds.Right > right - margin || childBounds.Bottom > bottom - margin)
            return false;

        var radius = Math.Clamp(Radius - 0.5f, 0f, Math.Max(0f, Math.Min(right - left, bottom - top) / 2f));
        var safeRadius = Math.Max(0f, radius - margin);
        bool Inside(float x, float y)
        {
            var dx = x - Math.Clamp(x, left + radius, right - radius);
            var dy = y - Math.Clamp(y, top + radius, bottom - radius);
            return dx * dx + dy * dy <= safeRadius * safeRadius;
        }

        // Keep the whole label away from the border, antialiasing and shadow.
        return Inside(childBounds.Left, childBounds.Top) && Inside(childBounds.Right, childBounds.Top)
            && Inside(childBounds.Left, childBounds.Bottom) && Inside(childBounds.Right, childBounds.Bottom);
    }
}
