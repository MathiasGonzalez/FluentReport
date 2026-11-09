using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

/// <summary>Styles a text. Alignment set here applies to the lines of the text within its container.</summary>
public class TextBuilder
{
    private readonly TextElement _text;

    internal TextBuilder(TextElement text) => _text = text;

    /// <summary>Font size in points. Default 12.</summary>
    public TextBuilder FontSize(float size) { _text.Style.FontSize = size; return this; }
    /// <summary>Font family name. Default <c>sans-serif</c>.</summary>
    public TextBuilder FontFamily(string family) { _text.Style.FontFamily = family; return this; }
    /// <summary>Bold text.</summary>
    public TextBuilder Bold() { _text.Style.Bold = true; return this; }
    /// <summary>Italic text.</summary>
    public TextBuilder Italic() { _text.Style.Italic = true; return this; }
    /// <summary>Underlined text.</summary>
    public TextBuilder Underline() { _text.Style.Underline = true; return this; }
    /// <summary>Text color as <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.</summary>
    public TextBuilder Color(string hex) { _text.Style.Color = ReportColor.FromHex(hex); return this; }
    /// <summary>Text color.</summary>
    public TextBuilder Color(ReportColor color) { _text.Style.Color = color; return this; }
    /// <summary>Centers each line.</summary>
    public TextBuilder AlignCenter() { _text.Style.Alignment = TextAlignment.Center; return this; }
    /// <summary>Aligns each line to the right.</summary>
    public TextBuilder AlignRight() { _text.Style.Alignment = TextAlignment.Right; return this; }
    /// <summary>Aligns each line to the left (the default).</summary>
    public TextBuilder AlignLeft() { _text.Style.Alignment = TextAlignment.Left; return this; }
    /// <summary>Justifies the lines, except the last of each paragraph.</summary>
    public TextBuilder AlignJustify() { _text.Style.Alignment = TextAlignment.Justify; return this; }
    /// <summary>Line height as a multiple of the font size. Default 1.2.</summary>
    public TextBuilder LineSpacing(float spacing) { _text.Style.LineSpacing = spacing; return this; }

    /// <summary>
    /// Rotates the text (degrees, counter-clockwise). PDF only; layout uses the unrotated box,
    /// so rotated text can overlap its neighbours.
    /// </summary>
    public TextBuilder Rotate(float degrees) { _text.Style.Rotation = degrees; return this; }
}

/// <summary>Builds a text from several spans, each with its own style, including page numbers.</summary>
public class DynamicTextBuilder(TextElement text)
{
    private static readonly Action<TextStyle> NoOverrides = _ => { };

    /// <summary>Adds text. <paramref name="configure"/> overrides the style of the whole text for this span.</summary>
    public DynamicTextBuilder Span(string value, Action<TextStyle>? configure = null)
    {
        text.AddSpan(value, configure ?? NoOverrides);
        return this;
    }

    /// <summary>Adds the number of the page being drawn.</summary>
    public DynamicTextBuilder CurrentPageNumber(Action<TextStyle>? configure = null)
    {
        text.AddCurrentPageSpan(configure ?? NoOverrides);
        return this;
    }

    /// <summary>Adds the total number of pages of the document.</summary>
    public DynamicTextBuilder TotalPages(Action<TextStyle>? configure = null)
    {
        text.AddTotalPagesSpan(configure ?? NoOverrides);
        return this;
    }
}
