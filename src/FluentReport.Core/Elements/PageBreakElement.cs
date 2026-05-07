using FluentReport.Core;

namespace FluentReport.Elements;

public class PageBreakElement : ElementBase
{
    public override Size Measure(MeasureContext context) => Size.Zero;
    public override void Render(RenderContext context, Position position, Size size) { }
}
