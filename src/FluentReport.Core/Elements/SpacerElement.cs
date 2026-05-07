using FluentReport.Core;

namespace FluentReport.Elements;

public class SpacerElement : ElementBase
{
    private readonly float _size;

    public SpacerElement(float size = 0) { _size = size; }

    public override Size Measure(MeasureContext context)
        => new(context.AvailableWidth, _size);

    public override void Render(RenderContext context, Position position, Size size) { }
}
