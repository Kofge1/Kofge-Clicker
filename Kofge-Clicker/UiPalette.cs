namespace KofgeClicker;

internal static class UiPalette
{
    internal static readonly Color Background = Color.FromArgb(9, 18, 29);
    internal static readonly Color Frame = Color.FromArgb(17, 38, 57);
    internal static readonly Color Card = Color.FromArgb(13, 47, 68);
    internal static readonly Color Surface = Color.FromArgb(8, 30, 44);
    internal static readonly Color Border = Color.FromArgb(36, 78, 105);
    internal static readonly Color Accent = Color.FromArgb(0, 167, 249);
    internal static readonly Color AccentEdge = Color.FromArgb(82, 203, 255);
    internal static readonly Color Text = Color.FromArgb(239, 248, 255);
    internal static readonly Color ToggleOff = Color.FromArgb(20, 53, 73);

    internal static Color MapColor(Color value)
    {
        var rgb = (value.R, value.G, value.B);
        // Repeated styling must not alter colors that already use this palette.
        if (rgb is (9, 18, 29) or (17, 38, 57) or (13, 47, 68) or (8, 30, 44)
            or (36, 78, 105) or (0, 167, 249) or (82, 203, 255) or (239, 248, 255)
            or (158, 207, 231) or (177, 217, 235) or (20, 53, 73)) return value;
        var mapped = rgb switch
        {
            (24, 32, 51) => Background,
            (46, 54, 88) or (36, 43, 59) or (58, 64, 92) => Frame,
            (59, 67, 106) => Card,
            (43, 49, 80) or (38, 43, 67) => Surface,
            (57, 68, 94) or (61, 72, 102) => Border,
            _ => value
        };
        if (mapped.ToArgb() != value.ToArgb()) return Color.FromArgb(value.A, mapped);
        if (value.R >= 220 && value.G >= 220 && value.B >= 230) mapped = Text;
        else if (value.B > value.G + 10 && value.B > value.R)
        {
            mapped = value.B switch
            {
                >= 220 when value.R >= 120 && value.G >= 175 => AccentEdge,
                >= 165 when value.R < 140 => Accent,
                >= 210 => Color.FromArgb(177, 217, 235),
                >= 165 => Color.FromArgb(158, 207, 231),
                >= 90 => Border,
                >= 60 => Frame,
                _ => Surface
            };
        }
        return Color.FromArgb(value.A, mapped);
    }
}
