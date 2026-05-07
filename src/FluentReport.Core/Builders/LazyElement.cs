using FluentReport.Core;
using FluentReport.Elements;

namespace FluentReport.Builders;

public class LazyElement : IElement
{
    private readonly ContainerBuilder _builder;
    private IElement? _built;

    public LazyElement(ContainerBuilder builder) => _builder = builder;

    public IElement Built => _built ??= _builder.Build();

    public Size Measure(MeasureContext context) => Built.Measure(context);
    public void Render(RenderContext context, Position position, Size size) => Built.Render(context, position, size);
}
