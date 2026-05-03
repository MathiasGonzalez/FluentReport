using FluentReport.Core;

namespace FluentReport.Elements;

public enum HorizontalAlignment { Left, Center, Right }

public class AlignElement : ElementBase
{
    public IElement? Child { get; set; }
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    public override Size Measure(MeasureContext context)
        => Child?.Measure(context) ?? Size.Zero;

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (Child == null) return;
        var childSize = Child.Measure(new MeasureContext { AvailableWidth = size.Width, AvailableHeight = size.Height });
        float x = Alignment switch
        {
            HorizontalAlignment.Center => position.X + (size.Width - childSize.Width) / 2f,
            HorizontalAlignment.Right => position.X + size.Width - childSize.Width,
            _ => position.X
        };
        Child.Render(context, new Position(x, position.Y), childSize);
    }
}
