namespace FluentReport.Styling;

public class BorderStyle
{
    public float Width { get; set; } = 1;
    public ReportColor Color { get; set; } = ReportColor.Black;
    public BorderSide Sides { get; set; } = BorderSide.All;
}

[Flags]
public enum BorderSide
{
    None = 0,
    Top = 1,
    Right = 2,
    Bottom = 4,
    Left = 8,
    All = Top | Right | Bottom | Left
}
