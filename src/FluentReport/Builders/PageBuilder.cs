using FluentReport.Core;
using FluentReport.Elements;

namespace FluentReport.Builders;

public class PageBuilder
{
    private readonly PageSettings _settings;

    internal PageBuilder(PageSettings settings) => _settings = settings;

    public PageBuilder Size(PageSize size) { _settings.Size = size; return this; }
    public PageBuilder Size(float width, float height) { _settings.Size = new PageSize(width, height); return this; }
    public PageBuilder Landscape() { _settings.Size = _settings.Size.Landscape(); return this; }

    public PageBuilder MarginAll(float margin)
    {
        _settings.MarginTop = _settings.MarginBottom = _settings.MarginLeft = _settings.MarginRight = margin;
        return this;
    }

    public PageBuilder Margin(float top, float right, float bottom, float left)
    {
        _settings.MarginTop = top; _settings.MarginRight = right;
        _settings.MarginBottom = bottom; _settings.MarginLeft = left;
        return this;
    }

    public PageBuilder MarginHorizontal(float h) { _settings.MarginLeft = _settings.MarginRight = h; return this; }
    public PageBuilder MarginVertical(float v) { _settings.MarginTop = _settings.MarginBottom = v; return this; }

    public ContainerBuilder Header()
    {
        var cb = new ContainerBuilder();
        _settings.HeaderElement = new LazyElement(cb);
        return cb;
    }

    public ContainerBuilder Footer()
    {
        var cb = new ContainerBuilder();
        _settings.FooterElement = new LazyElement(cb);
        return cb;
    }

    public ContainerBuilder Content()
    {
        var cb = new ContainerBuilder();
        _settings.ContentElement = new LazyElement(cb);
        return cb;
    }
}
