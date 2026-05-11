using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

public class TextBuilder
{
    private readonly TextElement _text;
    private readonly ContainerBuilder? _parent;

    internal TextBuilder(TextElement text, ContainerBuilder? parent = null)
    {
        _text = text;
        _parent = parent;
    }

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
    /// Applies a visual rotation (in degrees, counter-clockwise) to the rendered text.
    /// <para>
    /// <b>Important:</b> Rotation is a render-only transform. Text measurement and layout
    /// calculations do not account for the rotated bounding box, so rotated text may
    /// overlap neighbouring elements or be clipped at page/container edges.
    /// </para>
    /// </summary>
    public TextBuilder Rotate(float degrees) { _text.Style.Rotation = degrees; return this; }

    public ContainerBuilder Padding(float all) => _parent?.Padding(all) ?? throw new InvalidOperationException("No parent container");
    public ContainerBuilder PaddingVertical(float v) => _parent?.PaddingVertical(v) ?? throw new InvalidOperationException("No parent container");
    public ContainerBuilder PaddingHorizontal(float h) => _parent?.PaddingHorizontal(h) ?? throw new InvalidOperationException("No parent container");
}

public class DynamicTextBuilder(TextElement text)
{
    public DynamicTextBuilder Span(string value, Action<TextStyle>? configure = null)
    {
        var style = text.Style.Clone();
        configure?.Invoke(style);
        text.AddSpan(value, style);
        return this;
    }

    public DynamicTextBuilder CurrentPageNumber(Action<TextStyle>? configure = null)
    {
        var style = text.Style.Clone();
        configure?.Invoke(style);
        text.AddCurrentPageSpan(style);
        return this;
    }

    public DynamicTextBuilder TotalPages(Action<TextStyle>? configure = null)
    {
        var style = text.Style.Clone();
        configure?.Invoke(style);
        text.AddTotalPagesSpan(style);
        return this;
    }
}
