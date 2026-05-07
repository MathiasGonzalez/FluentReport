using FluentReport.Core;

namespace FluentReport.Elements;

/// <summary>
/// Renders a nested <see cref="Document"/> inline inside a parent document.
/// Each page of the nested document is drawn sequentially, scaled to fit the
/// available width while preserving the original aspect ratio.
/// Rendering requires a <see cref="IPageImagesProvider"/> on <see cref="RenderContext.PageImagesProvider"/>;
/// without it the element renders as empty.
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
        if (context.PageImagesProvider == null) return;

        var pageImages = context.PageImagesProvider.GetPageImages(_nested.Settings, 1f);
        float y = position.Y;

        context.Canvas.Save();
        context.Canvas.ClipRect(position.X, position.Y, size.Width, size.Height);

        foreach (var pngBytes in pageImages)
        {
            var (imgW, imgH) = ImageElement.ReadDimensionsPublic(pngBytes);
            if (imgW <= 0) continue;

            float scaledH = imgH * size.Width / imgW;
            if (y - position.Y + scaledH > size.Height + 0.5f && y > position.Y)
                break;

            context.Canvas.DrawImageBytes(pngBytes, position.X, y, size.Width, scaledH);
            y += scaledH;
        }

        context.Canvas.Restore();
    }
}

