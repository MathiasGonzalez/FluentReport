using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>Draws a background and a border around its child, over the width it is given.</summary>
public class BorderElement : IElement
{
    /// <summary>The element inside the border.</summary>
    public IElement? Child { get; set; }
    /// <summary>Border width, color and sides. Width 0 draws no border.</summary>
    public BorderStyle Border { get; set; } = new();
    /// <summary>Fill color; <c>null</c> draws no background.</summary>
    public ReportColor? BackgroundColor { get; set; }
}
