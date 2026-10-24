using FluentReport.Styling;

namespace FluentReport.Elements;

public class LineElement : IElement
{
    public float Thickness { get; set; } = 1;
    public ReportColor Color { get; set; } = ReportColor.Black;
}
