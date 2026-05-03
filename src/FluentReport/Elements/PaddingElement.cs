using FluentReport.Core;

namespace FluentReport.Elements;

public class PaddingElement : ElementBase
{
    public IElement? Child { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
    public float Right { get; set; }

    public override Size Measure(MeasureContext context)
    {
        if (Child == null) return new(Left + Right, Top + Bottom);
        var childContext = new MeasureContext
        {
            AvailableWidth = Math.Max(0, context.AvailableWidth - Left - Right),
            AvailableHeight = Math.Max(0, context.AvailableHeight - Top - Bottom)
        };
        var childSize = Child.Measure(childContext);
        return new(childSize.Width + Left + Right, childSize.Height + Top + Bottom);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (Child == null) return;
        var childPosition = new Position(position.X + Left, position.Y + Top);
        var childSize = new Size(
            Math.Max(0, size.Width - Left - Right),
            Math.Max(0, size.Height - Top - Bottom)
        );
        Child.Render(context, childPosition, childSize);
    }
}
