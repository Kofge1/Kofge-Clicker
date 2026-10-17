using System.Drawing.Imaging;

namespace KofgeClicker;

public sealed partial class RoundedPanel
{
    private bool _cacheBackgroundDrawing;
    private Bitmap? _backgroundDrawing;
    private BackgroundDrawingKey _backgroundDrawingKey;

    // Cache only the panel surface. Child controls, text and input are always live.
    public bool CacheBackgroundDrawing
    {
        get => _cacheBackgroundDrawing;
        set
        {
            if (_cacheBackgroundDrawing == value)
                return;
            _cacheBackgroundDrawing = value;
            ClearBackgroundDrawing();
            Invalidate();
        }
    }

    private bool TryPaintCachedBackground(Graphics graphics)
    {
        var background = Parent?.BackColor ?? UiTheme.AppBackground;
        if (!CacheBackgroundDrawing || !UseAntialiasedEdges || Width <= 0 || Height <= 1
            || background.A != 255)
        {
            ClearBackgroundDrawing();
            return false;
        }

        var key = new BackgroundDrawingKey(ClientSize, graphics.DpiX, graphics.DpiY,
            Radius, FillColor, BorderColor, background, DrawShadow);
        if (_backgroundDrawing is null || key != _backgroundDrawingKey)
        {
            var image = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            try
            {
                image.SetResolution(graphics.DpiX, graphics.DpiY);
                using var canvas = Graphics.FromImage(image);
                canvas.Clear(background);
                PaintAntialiased(canvas);
            }
            catch
            {
                image.Dispose();
                throw;
            }

            ClearBackgroundDrawing();
            _backgroundDrawing = image;
            _backgroundDrawingKey = key;
        }

        graphics.DrawImageUnscaled(_backgroundDrawing, 0, 0);
        return true;
    }

    private void ClearBackgroundDrawing()
    {
        _backgroundDrawing?.Dispose();
        _backgroundDrawing = null;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        ClearBackgroundDrawing();
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ClearBackgroundDrawing();
        base.Dispose(disposing);
    }

    private readonly record struct BackgroundDrawingKey(Size Size, float DpiX, float DpiY,
        int Radius, Color Fill, Color Border, Color Background, bool Shadow);
}
