using FluentReport.Core;
using SkiaSharp;

namespace FluentReport.Elements;

public enum ImageFit { Contain, Cover, Fill, FitWidth, FitHeight }

public class ImageElement : ElementBase
{
    private readonly SKBitmap? _bitmap;
    public float? FixedWidth { get; set; }
    public float? FixedHeight { get; set; }
    public ImageFit Fit { get; set; } = ImageFit.Contain;

    public ImageElement(string path)
    {
        if (File.Exists(path))
            _bitmap = SKBitmap.Decode(path);
    }

    public ImageElement(byte[] imageBytes)
    {
        _bitmap = SKBitmap.Decode(imageBytes);
    }

    public override Size Measure(MeasureContext context)
    {
        if (_bitmap == null) return Size.Zero;
        var w = FixedWidth ?? Math.Min(_bitmap.Width, context.AvailableWidth);
        var h = FixedHeight ?? (_bitmap.Height * w / _bitmap.Width);
        return new(w, h);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (_bitmap == null) return;
        var destRect = new SKRect(position.X, position.Y, position.X + size.Width, position.Y + size.Height);
        using var paint = new SKPaint { IsAntialias = true };
        context.Canvas.DrawBitmap(_bitmap, destRect, paint);
    }
}
