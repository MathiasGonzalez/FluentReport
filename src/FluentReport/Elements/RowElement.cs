using FluentReport.Core;

namespace FluentReport.Elements;

public class RowItem
{
    public IElement? Element { get; set; }
    public float? FixedWidth { get; set; }
    public float RelativeWidth { get; set; } = 1;
    public bool IsRelative => !FixedWidth.HasValue;
}

public class RowElement : ElementBase
{
    public List<RowItem> Items { get; } = new();
    public float Spacing { get; set; } = 0;

    public override Size Measure(MeasureContext context)
    {
        var (fixedTotal, relativeTotal, spacing) = CalculateWidths(context.AvailableWidth);
        float maxHeight = 0;
        foreach (var item in Items)
        {
            var width = GetItemWidth(item, context.AvailableWidth, fixedTotal, relativeTotal, spacing);
            var s = item.Element?.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = context.AvailableHeight }) ?? Size.Zero;
            if (s.Height > maxHeight) maxHeight = s.Height;
        }
        return new(context.AvailableWidth, maxHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        var (fixedTotal, relativeTotal, spacing) = CalculateWidths(size.Width);
        float x = position.X;
        foreach (var item in Items)
        {
            var width = GetItemWidth(item, size.Width, fixedTotal, relativeTotal, spacing);
            var s = item.Element?.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = size.Height }) ?? Size.Zero;
            item.Element?.Render(context, new Position(x, position.Y), new Size(width, s.Height));
            x += width + Spacing;
        }
    }

    private (float fixedTotal, float relativeTotal, float spacing) CalculateWidths(float availableWidth)
    {
        var fixedTotal = Items.Where(i => !i.IsRelative).Sum(i => i.FixedWidth ?? 0);
        var relativeTotal = Items.Where(i => i.IsRelative).Sum(i => i.RelativeWidth);
        var spacing = Items.Count > 1 ? Spacing * (Items.Count - 1) : 0;
        return (fixedTotal, relativeTotal, spacing);
    }

    private float GetItemWidth(RowItem item, float availableWidth, float fixedTotal, float relativeTotal, float spacing)
    {
        if (!item.IsRelative) return item.FixedWidth ?? 0;
        var remaining = availableWidth - fixedTotal - spacing;
        return relativeTotal > 0 ? remaining * (item.RelativeWidth / relativeTotal) : 0;
    }
}
