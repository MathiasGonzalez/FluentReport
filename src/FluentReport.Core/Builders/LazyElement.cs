using FluentReport.Core;
using FluentReport.Elements;

namespace FluentReport.Builders;

public class LazyElement(ContainerBuilder builder) : IElement
{
    private IElement? _built;

    public IElement Built => _built ??= builder.Build();

    public Size Measure(MeasureContext context) => Built.Measure(context);
    public void Render(RenderContext context, Position position, Size size) => Built.Render(context, position, size);
}
