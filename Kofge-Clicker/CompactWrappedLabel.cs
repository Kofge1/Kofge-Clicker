namespace KofgeClicker;

internal sealed class CompactWrappedLabel : Label
{
    private const int TextInset = 6;

    public int LineSpacingReduction { get; set; } = 3;

    protected override void OnPaint(PaintEventArgs e)
    {
        if (string.IsNullOrEmpty(Text) || !Text.Contains('\n'))
        {
            base.OnPaint(e);
            return;
        }

        var lines = Text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        var lineHeight = Font.Height;
        var lineAdvance = Math.Max(1, lineHeight - LineSpacingReduction);
        var totalHeight = lineHeight + ((lines.Length - 1) * lineAdvance);
        var top = TextAlign switch
        {
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight
                => (ClientSize.Height - totalHeight) / 2,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
                => ClientSize.Height - totalHeight,
            _ => 0
        };

        var flags = TextFormatFlags.NoPadding
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.SingleLine
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.PreserveGraphicsClipping;
        flags |= TextAlign switch
        {
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter
                => TextFormatFlags.HorizontalCenter,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight
                => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };

        var textColor = Enabled ? ForeColor : SystemColors.GrayText;
        var textWidth = Math.Max(0, ClientSize.Width - Padding.Horizontal - TextInset);
        for (var index = 0; index < lines.Length; index++)
        {
            var lineBounds = new Rectangle(
                Padding.Left + TextInset,
                top + (index * lineAdvance),
                textWidth,
                lineHeight);
            TextRenderer.DrawText(e.Graphics, lines[index], Font, lineBounds, textColor, flags);
        }
    }
}
