using FluentReport.Styling;

namespace FluentReport.Elements;

public class BorderElement : IElement
{
    public IElement? Child { get; set; }
    public BorderStyle Border { get; set; } = new();
    public ReportColor? BackgroundColor { get; set; }
}
