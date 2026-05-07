using FluentReport.Core;

namespace FluentReport.Elements;

public interface IElement
{
    Size Measure(MeasureContext context);
    void Render(RenderContext context, Position position, Size size);
}
