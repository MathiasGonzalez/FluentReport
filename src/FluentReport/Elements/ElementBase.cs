using FluentReport.Core;

namespace FluentReport.Elements;

public abstract class ElementBase : IElement
{
    public abstract Size Measure(MeasureContext context);
    public abstract void Render(RenderContext context, Position position, Size size);
}
