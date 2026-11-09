using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>A horizontal line across the available width.</summary>
public class LineElement : IElement
{
    /// <summary>Line thickness in points. Default 1.</summary>
    public float Thickness { get; set; } = 1;
    /// <summary>Line color. Default black.</summary>
    public ReportColor Color { get; set; } = ReportColor.Black;
}
