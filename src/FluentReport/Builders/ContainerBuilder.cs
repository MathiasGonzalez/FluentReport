using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

public class ContainerBuilder
{
    protected IElement? _child;
    private float _paddingTop, _paddingBottom, _paddingLeft, _paddingRight;
    private ReportColor? _background;
    private BorderStyle? _border;
    private HorizontalAlignment? _alignment;

    public ContainerBuilder Padding(float all)
    {
        _paddingTop = _paddingBottom = _paddingLeft = _paddingRight = all;
        return this;
    }

    public ContainerBuilder PaddingHorizontal(float h) { _paddingLeft = _paddingRight = h; return this; }
    public ContainerBuilder PaddingVertical(float v) { _paddingTop = _paddingBottom = v; return this; }
    public ContainerBuilder PaddingTop(float v) { _paddingTop = v; return this; }
    public ContainerBuilder PaddingBottom(float v) { _paddingBottom = v; return this; }
    public ContainerBuilder PaddingLeft(float v) { _paddingLeft = v; return this; }
    public ContainerBuilder PaddingRight(float v) { _paddingRight = v; return this; }

    public ContainerBuilder Background(string hex)
    {
        _background = ReportColor.FromHex(hex);
        return this;
    }

    public ContainerBuilder Background(ReportColor color)
    {
        _background = color;
        return this;
    }

    public ContainerBuilder Border(float width = 1, string? colorHex = null)
    {
        _border = new BorderStyle
        {
            Width = width,
            Color = colorHex != null ? ReportColor.FromHex(colorHex) : ReportColor.Black
        };
        return this;
    }

    public ContainerBuilder AlignCenter()
    {
        _alignment = HorizontalAlignment.Center;
        return this;
    }

    public ContainerBuilder AlignRight()
    {
        _alignment = HorizontalAlignment.Right;
        return this;
    }

    public ContainerBuilder AlignLeft()
    {
        _alignment = HorizontalAlignment.Left;
        return this;
    }

    public TextBuilder Text(string text)
    {
        var textEl = new TextElement(text);
        _child = textEl;
        return new TextBuilder(textEl, this);
    }

    public TextBuilder Text(Action<DynamicTextBuilder> configure)
    {
        var textEl = new TextElement();
        var builder = new DynamicTextBuilder(textEl);
        configure(builder);
        _child = textEl;
        return new TextBuilder(textEl, this);
    }

    public ContainerBuilder Column(Action<ColumnBuilder> configure)
    {
        var col = new ColumnElement();
        var builder = new ColumnBuilder(col);
        configure(builder);
        _child = col;
        return this;
    }

    public ContainerBuilder Row(Action<RowBuilder> configure)
    {
        var row = new RowElement();
        var builder = new RowBuilder(row);
        configure(builder);
        _child = row;
        return this;
    }

    public ContainerBuilder Table(Action<TableBuilder> configure)
    {
        var table = new TableElement();
        var builder = new TableBuilder(table);
        configure(builder);
        _child = table;
        return this;
    }

    public ContainerBuilder Image(string path)
    {
        _child = new ImageElement(path);
        return this;
    }

    public ContainerBuilder Image(byte[] imageBytes)
    {
        _child = new ImageElement(imageBytes);
        return this;
    }

    public ContainerBuilder Spacer(float size = 0)
    {
        _child = new SpacerElement(size);
        return this;
    }

    public ContainerBuilder Line(float thickness = 1, string? colorHex = null)
    {
        var line = new LineElement { Thickness = thickness };
        if (colorHex != null) line.Color = ReportColor.FromHex(colorHex);
        _child = line;
        return this;
    }

    public ContainerBuilder PageBreak()
    {
        _child = new PageBreakElement();
        return this;
    }

    internal virtual IElement Build()
    {
        IElement element = _child ?? new SpacerElement();

        bool hasPadding = _paddingTop != 0 || _paddingBottom != 0 || _paddingLeft != 0 || _paddingRight != 0;
        bool hasBorder = _border != null;
        bool hasBg = _background.HasValue;

        if (hasPadding)
        {
            element = new PaddingElement
            {
                Child = element,
                Top = _paddingTop,
                Bottom = _paddingBottom,
                Left = _paddingLeft,
                Right = _paddingRight
            };
        }

        if (hasBorder || hasBg)
        {
            element = new BorderElement
            {
                Child = element,
                Border = _border ?? new BorderStyle { Width = 0 },
                BackgroundColor = _background
            };
        }

        if (_alignment.HasValue)
        {
            element = new AlignElement
            {
                Child = element,
                Alignment = _alignment.Value
            };
        }

        return element;
    }
}
