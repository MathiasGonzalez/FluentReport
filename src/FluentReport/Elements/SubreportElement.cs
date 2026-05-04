using FluentReport.Core;
using FluentReport.Rendering;
using SkiaSharp;

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
        var renderer = new DocumentRenderer(_nested.Settings);
        int pageCount = renderer.GetPageCount();
        float y = position.Y;

        for (int i = 0; i < pageCount; i++)
        {
            using var image = renderer.RenderPageToImage(i);
            float scaledH = image.Height * size.Width / Math.Max(1f, image.Width);
            var destRect = new SKRect(position.X, y, position.X + size.Width, y + scaledH);
            using var paint = new SKPaint { IsAntialias = true };
            context.Canvas.DrawImage(image, destRect, paint);
            y += scaledH;
        }
    }
}
