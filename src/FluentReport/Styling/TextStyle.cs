namespace FluentReport.Styling;

public class TextStyle
{
    public float FontSize { get; set; } = 12;
    public string FontFamily { get; set; } = "sans-serif";
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    public bool Underline { get; set; } = false;
    public ReportColor Color { get; set; } = ReportColor.Black;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public float LineSpacing { get; set; } = 1.2f;

    public TextStyle Clone() => new()
    {
        FontSize = FontSize,
        FontFamily = FontFamily,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Color = Color,
        Alignment = Alignment,
        LineSpacing = LineSpacing
    };
}

public enum TextAlignment { Left, Center, Right, Justify }
