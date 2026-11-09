namespace FluentReport.Styling;

/// <summary>Width, color and sides of a border.</summary>
public class BorderStyle
{
    /// <summary>Line width in points. Default 1; 0 draws no border.</summary>
    public float Width { get; set; } = 1;
    /// <summary>Line color. Default black.</summary>
    public ReportColor Color { get; set; } = ReportColor.Black;
    /// <summary>Which sides are drawn. Default all.</summary>
    public BorderSide Sides { get; set; } = BorderSide.All;
}

/// <summary>Sides of a border; combine them as flags.</summary>
[Flags]
public enum BorderSide
{
    /// <summary>No side.</summary>
    None = 0,
    /// <summary>The top side.</summary>
    Top = 1,
    /// <summary>The right side.</summary>
    Right = 2,
    /// <summary>The bottom side.</summary>
    Bottom = 4,
    /// <summary>The left side.</summary>
    Left = 8,
    /// <summary>The four sides.</summary>
    All = Top | Right | Bottom | Left
}
