using FluentReport.Core;

namespace FluentReport.Elements;

/// <summary>
/// Renders a sequence of child elements in a vertical stack with optional spacing,
/// much like <see cref="ColumnElement"/> but constructed from a data collection via a factory.
/// </summary>
/// <remarks>
/// The elements returned by the factory are cached after the first call so that
/// <see cref="Measure"/> and <see cref="Render"/> use identical instances.
/// </remarks>
public class ListElement : ElementBase
{
    private readonly IReadOnlyList<IElement> _items;

    /// <summary>Vertical gap between consecutive items, in points.</summary>
    public float Spacing { get; set; }

    public ListElement(IEnumerable<IElement> items, float spacing = 0)
    {
        _items = items.ToList().AsReadOnly();
        Spacing = spacing;
    }

    public override Size Measure(MeasureContext context)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        bool first = true;

        foreach (var item in _items)
        {
            if (!first) totalHeight += Spacing;
            first = false;

            var s = item.Measure(new MeasureContext
            {
                AvailableWidth = context.AvailableWidth,
                AvailableHeight = Math.Max(0, context.AvailableHeight - totalHeight)
            });

            totalHeight += s.Height;
            if (s.Width > maxWidth) maxWidth = s.Width;
        }

        return new(Math.Min(maxWidth, context.AvailableWidth), totalHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        float y = position.Y;
        bool first = true;

        foreach (var item in _items)
        {
            if (!first) y += Spacing;
            first = false;

            var s = item.Measure(new MeasureContext
            {
                AvailableWidth = size.Width,
                AvailableHeight = Math.Max(0, size.Height - (y - position.Y))
            });

            item.Render(context, new Position(position.X, y), new Size(size.Width, s.Height));
            y += s.Height;
        }
    }
}
