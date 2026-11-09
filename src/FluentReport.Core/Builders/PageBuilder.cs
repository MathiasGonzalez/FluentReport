using FluentReport.Core;
using FluentReport.Elements;

namespace FluentReport.Builders;

/// <summary>Configures one page definition. All measures are in points (1/72 inch).</summary>
public class PageBuilder
{
    private readonly PageSettings _settings;

    internal PageBuilder(PageSettings settings) => _settings = settings;

    /// <summary>Sets the page size, e.g. <see cref="PageSizes.A4"/>.</summary>
    public PageBuilder Size(PageSize size) { _settings.Size = size; _settings.ContinuousHeight = false; return this; }
    /// <summary>Sets a custom page size in points.</summary>
    public PageBuilder Size(float width, float height) { _settings.Size = new PageSize(width, height); _settings.ContinuousHeight = false; return this; }

    /// <summary>
    /// A page <paramref name="width"/> points wide that grows to the height of its content: the whole
    /// content goes on one page with no blank space below it. For receipts on roll paper (80 mm ≈ 227 pt).
    /// Page breaks inside the content are ignored.
    /// </summary>
    public PageBuilder ContinuousSize(float width)
    {
        if (!(width > 0) || float.IsInfinity(width)) throw new ArgumentOutOfRangeException(nameof(width), width, "The page width must be positive.");
        _settings.Size = new PageSize(width, 0);
        _settings.ContinuousHeight = true;
        return this;
    }
    /// <summary>Swaps width and height. A <see cref="ContinuousSize"/> page has no fixed height and is left as it is.</summary>
    public PageBuilder Landscape()
    {
        if (!_settings.ContinuousHeight) _settings.Size = _settings.Size.Landscape();
        return this;
    }

    /// <summary>Sets the four margins to the same value.</summary>
    public PageBuilder MarginAll(float margin)
    {
        _settings.MarginTop = _settings.MarginBottom = _settings.MarginLeft = _settings.MarginRight = margin;
        return this;
    }

    /// <summary>Sets each margin, in CSS order: top, right, bottom, left.</summary>
    public PageBuilder Margin(float top, float right, float bottom, float left)
    {
        _settings.MarginTop = top; _settings.MarginRight = right;
        _settings.MarginBottom = bottom; _settings.MarginLeft = left;
        return this;
    }

    /// <summary>Sets the left and right margins.</summary>
    public PageBuilder MarginHorizontal(float h) { _settings.MarginLeft = _settings.MarginRight = h; return this; }
    /// <summary>Sets the top and bottom margins.</summary>
    public PageBuilder MarginVertical(float v) { _settings.MarginTop = _settings.MarginBottom = v; return this; }

    /// <summary>The container repeated at the top of every page.</summary>
    public ContainerBuilder Header()
    {
        var cb = new ContainerBuilder();
        _settings.HeaderElement = new LazyElement(cb);
        return cb;
    }

    /// <summary>The container repeated at the bottom of every page.</summary>
    public ContainerBuilder Footer()
    {
        var cb = new ContainerBuilder();
        _settings.FooterElement = new LazyElement(cb);
        return cb;
    }

    /// <summary>The container for the body, which flows across as many pages as it needs.</summary>
    public ContainerBuilder Content()
    {
        var cb = new ContainerBuilder();
        _settings.ContentElement = new LazyElement(cb);
        return cb;
    }
}
