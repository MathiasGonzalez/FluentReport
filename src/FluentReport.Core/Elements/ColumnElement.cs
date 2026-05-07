using FluentReport.Core;

namespace FluentReport.Elements;

public class ColumnElement : ElementBase
{
    public List<IElement> Items { get; } = new();
    public float Spacing { get; set; } = 0;

    public override Size Measure(MeasureContext context)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        bool first = true;
        foreach (var item in Items)
        {
            if (!first) totalHeight += Spacing;
            first = false;
            var s = item.Measure(context.WithDimensions(
                context.AvailableWidth,
                Math.Max(0, context.AvailableHeight - totalHeight)));
            totalHeight += s.Height;
            if (s.Width > maxWidth) maxWidth = s.Width;
        }
        return new(Math.Min(maxWidth, context.AvailableWidth), totalHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        float y = position.Y;
        bool first = true;
        foreach (var item in Items)
        {
            if (!first) y += Spacing;
            first = false;
            var s = item.Measure(context.MeasureContextFor(
                size.Width,
                Math.Max(0, size.Height - (y - position.Y))));
            item.Render(context, new Position(position.X, y), new Size(size.Width, s.Height));
            y += s.Height;
        }
    }
}
