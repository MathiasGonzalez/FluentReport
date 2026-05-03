using FluentReport.Elements;

namespace FluentReport.Core;

public class PageSettings
{
    public PageSize Size { get; set; } = PageSize.A4;
    public float MarginTop { get; set; } = 40;
    public float MarginBottom { get; set; } = 40;
    public float MarginLeft { get; set; } = 40;
    public float MarginRight { get; set; } = 40;

    public IElement? HeaderElement { get; set; }
    public IElement? ContentElement { get; set; }
    public IElement? FooterElement { get; set; }

    public float ContentWidth => Size.Width - MarginLeft - MarginRight;
    public float ContentHeight => Size.Height - MarginTop - MarginBottom;
}
