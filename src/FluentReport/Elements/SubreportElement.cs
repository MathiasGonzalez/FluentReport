using FluentReport.Core;

namespace FluentReport.Elements;

/// <summary>
/// Renders a nested <see cref="Document"/> inline inside a parent document.
/// Each page of the nested document is drawn sequentially, scaled to fit the
/// available width while preserving the original aspect ratio.
/// </summary>
public class SubreportElement : ElementBase
{
    private readonly Document _nested;

    /// <summary>
    /// Optional fixed height for the subreport area, in points.
    /// When <c>null</c>, the height is estimated from the nested document's page sizes.
    /// </summary>
    public float? FixedHeight { get; set; }

    public SubreportElement(Document nested)
    {
        _nested = nested ?? throw new ArgumentNullException(nameof(nested));
    }

    public override Size Measure(MeasureContext context)
    {
        if (FixedHeight.HasValue)
            return new(context.AvailableWidth, FixedHeight.Value);

        // Estimate total height from each page's natural aspect ratio — avoids a full render pass.
        float totalHeight = _nested.Settings.Pages
            .Sum(p => p.Size.Height * context.AvailableWidth / Math.Max(1f, p.Size.Width));

        return new(context.AvailableWidth, totalHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        // Use GenerateImages for a single pagination pass (O(n) instead of O(n²)).
        var pageImages = _nested.GenerateImages(1f);
        float y = position.Y;

        // Clip the canvas to the allocated height to prevent overflow into adjacent content.
        context.Canvas.Save();
        context.Canvas.ClipRect(
            new SkiaSharp.SKRect(position.X, position.Y, position.X + size.Width, position.Y + size.Height));

        foreach (var pngBytes in pageImages)
        {
            using var image = SkiaSharp.SKImage.FromEncodedData(pngBytes);
            if (image == null) continue;

            float scaledH = image.Height * size.Width / Math.Max(1f, image.Width);
            // Stop drawing once we would go beyond the allocated area.
            if (y - position.Y + scaledH > size.Height + 0.5f && y > position.Y)
                break;

            var destRect = new SkiaSharp.SKRect(position.X, y, position.X + size.Width, y + scaledH);
            using var paint = new SkiaSharp.SKPaint { IsAntialias = true };
            context.Canvas.DrawImage(image, destRect, paint);
            y += scaledH;
        }

        context.Canvas.Restore();
    }
}
