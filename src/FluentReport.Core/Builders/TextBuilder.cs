using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

public class TextBuilder
{
    private readonly TextElement _text;

    internal TextBuilder(TextElement text) => _text = text;

    public TextBuilder FontSize(float size) { _text.Style.FontSize = size; return this; }
    public TextBuilder FontFamily(string family) { _text.Style.FontFamily = family; return this; }
    public TextBuilder Bold() { _text.Style.Bold = true; return this; }
    public TextBuilder Italic() { _text.Style.Italic = true; return this; }
    public TextBuilder Underline() { _text.Style.Underline = true; return this; }
    public TextBuilder Color(string hex) { _text.Style.Color = ReportColor.FromHex(hex); return this; }
    public TextBuilder Color(ReportColor color) { _text.Style.Color = color; return this; }
    public TextBuilder AlignCenter() { _text.Style.Alignment = TextAlignment.Center; return this; }
    public TextBuilder AlignRight() { _text.Style.Alignment = TextAlignment.Right; return this; }
    public TextBuilder AlignLeft() { _text.Style.Alignment = TextAlignment.Left; return this; }
    public TextBuilder AlignJustify() { _text.Style.Alignment = TextAlignment.Justify; return this; }
    public TextBuilder LineSpacing(float spacing) { _text.Style.LineSpacing = spacing; return this; }

    /// <summary>
    /// Rotates the text (degrees, counter-clockwise). PDF only; layout uses the unrotated box,
    /// so rotated text can overlap its neighbours.
    /// </summary>
    public TextBuilder Rotate(float degrees) { _text.Style.Rotation = degrees; return this; }
}

public class DynamicTextBuilder(TextElement text)
{
    private static readonly Action<TextStyle> NoOverrides = _ => { };

    public DynamicTextBuilder Span(string value, Action<TextStyle>? configure = null)
    {
        text.AddSpan(value, configure ?? NoOverrides);
        return this;
    }

    public DynamicTextBuilder CurrentPageNumber(Action<TextStyle>? configure = null)
    {
        text.AddCurrentPageSpan(configure ?? NoOverrides);
        return this;
    }

    public DynamicTextBuilder TotalPages(Action<TextStyle>? configure = null)
    {
        text.AddTotalPagesSpan(configure ?? NoOverrides);
        return this;
    }
}
