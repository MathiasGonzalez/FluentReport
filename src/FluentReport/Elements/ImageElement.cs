using FluentReport.Core;
using SkiaSharp;

namespace FluentReport.Elements;

public enum ImageFit { Contain, Cover, Fill, FitWidth, FitHeight }

public class ImageElement : ElementBase, IDisposable
{
    private readonly SKBitmap? _bitmap;
    private bool _disposed;
    public float? FixedWidth { get; set; }
    public float? FixedHeight { get; set; }
    public ImageFit Fit { get; set; } = ImageFit.Contain;

    /// <summary>Raw image bytes as provided to the constructor. Useful for non-Skia renderers (e.g. HTML).</summary>
    public byte[]? SourceBytes { get; private set; }

    /// <summary>Source file path when constructed from a path. Useful for non-Skia renderers.</summary>
    public string? SourcePath { get; private set; }

    public ImageElement(string path)
    {
        SourcePath = path;
        if (File.Exists(path))
        {
            SourceBytes = File.ReadAllBytes(path);
            _bitmap = SKBitmap.Decode(SourceBytes);
        }
    }

    public ImageElement(byte[] imageBytes)
    {
        if (imageBytes.Length > 0)
        {
            SourceBytes = imageBytes;
            _bitmap = SKBitmap.Decode(imageBytes);
        }
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _bitmap?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
