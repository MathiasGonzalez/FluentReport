using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

/// <summary>
/// A box that holds one piece of content (text, column, row, table, image…) and decorates it.
/// The decorations can be called in any order and always nest the same way: padding around the
/// content, then background and border around the padding, then the alignment. A container takes
/// one content: use <see cref="Column"/> or <see cref="Row"/> to place several elements.
/// </summary>
public class ContainerBuilder
{
    private IElement? _child;
    private float _paddingTop, _paddingBottom, _paddingLeft, _paddingRight;
    private ReportColor? _background;
    private BorderStyle? _border;
    private HorizontalAlignment? _alignment;

    /// <summary>Space between the container's edge and its content, on the four sides.</summary>
    public ContainerBuilder Padding(float all)
    {
        _paddingTop = _paddingBottom = _paddingLeft = _paddingRight = all;
        return this;
    }

    /// <summary>Left and right padding.</summary>
    public ContainerBuilder PaddingHorizontal(float h) { _paddingLeft = _paddingRight = h; return this; }
    /// <summary>Top and bottom padding.</summary>
    public ContainerBuilder PaddingVertical(float v) { _paddingTop = _paddingBottom = v; return this; }
    /// <summary>Top padding.</summary>
    public ContainerBuilder PaddingTop(float v) { _paddingTop = v; return this; }
    /// <summary>Bottom padding.</summary>
    public ContainerBuilder PaddingBottom(float v) { _paddingBottom = v; return this; }
    /// <summary>Left padding.</summary>
    public ContainerBuilder PaddingLeft(float v) { _paddingLeft = v; return this; }
    /// <summary>Right padding.</summary>
    public ContainerBuilder PaddingRight(float v) { _paddingRight = v; return this; }

    /// <summary>Background color, padding included, given as <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.</summary>
    public ContainerBuilder Background(string hex)
    {
        _background = ReportColor.FromHex(hex);
        return this;
    }

    /// <summary>Background color, padding included.</summary>
    public ContainerBuilder Background(ReportColor color)
    {
        _background = color;
        return this;
    }

    /// <summary>Draws a border around the content, padding included. Default color black.</summary>
    public ContainerBuilder Border(float width = 1, string? colorHex = null)
    {
        _border = new BorderStyle
        {
            Width = width,
            Color = colorHex != null ? ReportColor.FromHex(colorHex) : ReportColor.Black
        };
        return this;
    }

    /// <summary>Centers the content horizontally. Text is centered line by line and its background and border fill the container; other content is shrunk to its own width, background and border included, and centered.</summary>
    public ContainerBuilder AlignCenter()
    {
        _alignment = HorizontalAlignment.Center;
        return this;
    }

    /// <summary>Places the content at the right. Text is aligned line by line and its background and border fill the container; other content is shrunk to its own width, background and border included, and moved to the right.</summary>
    public ContainerBuilder AlignRight()
    {
        _alignment = HorizontalAlignment.Right;
        return this;
    }

    /// <summary>Places the content at the left (the default).</summary>
    public ContainerBuilder AlignLeft()
    {
        _alignment = HorizontalAlignment.Left;
        return this;
    }

    /// <summary>Sets a text as content and returns its style builder.</summary>
    public TextBuilder Text(string text)
    {
        var textEl = new TextElement(text);
        SetContent(textEl);
        return new TextBuilder(textEl);
    }

    /// <summary>Sets a text made of several spans (styles, page numbers) as content.</summary>
    public TextBuilder Text(Action<DynamicTextBuilder> configure)
    {
        var textEl = new TextElement();
        var builder = new DynamicTextBuilder(textEl);
        configure(builder);
        SetContent(textEl);
        return new TextBuilder(textEl);
    }

    /// <summary>Sets a column as content: items stacked vertically.</summary>
    public ContainerBuilder Column(Action<ColumnBuilder> configure)
    {
        var col = new ColumnElement();
        var builder = new ColumnBuilder(col);
        configure(builder);
        return SetContent(col);
    }

    /// <summary>Sets a row as content: items side by side.</summary>
    public ContainerBuilder Row(Action<RowBuilder> configure)
    {
        var row = new RowElement();
        var builder = new RowBuilder(row);
        configure(builder);
        return SetContent(row);
    }

    /// <summary>Sets a table as content.</summary>
    public ContainerBuilder Table(Action<TableBuilder> configure)
    {
        var table = new TableElement();
        var builder = new TableBuilder(table);
        configure(builder);
        return SetContent(table);
    }

    /// <summary>Sets an image file as content. It scales down to the container's width, keeping its proportions.</summary>
    public ContainerBuilder Image(string path)
    {
        return SetContent(new ImageElement(path));
    }

    /// <summary>Sets an encoded image (PNG, JPEG, GIF, WebP, BMP) as content. It scales down to the container's width, keeping its proportions.</summary>
    public ContainerBuilder Image(byte[] imageBytes)
    {
        return SetContent(new ImageElement(imageBytes));
    }

    /// <summary>Sets empty vertical space of <paramref name="size"/> points as content.</summary>
    public ContainerBuilder Spacer(float size = 0)
    {
        return SetContent(new SpacerElement(size));
    }

    /// <summary>Sets a horizontal line across the container as content. Default color black.</summary>
    public ContainerBuilder Line(float thickness = 1, string? colorHex = null)
    {
        var line = new LineElement { Thickness = thickness };
        if (colorHex != null) line.Color = ReportColor.FromHex(colorHex);
        return SetContent(line);
    }

    /// <summary>Starts a new page here. Ignored on a continuous page.</summary>
    public ContainerBuilder PageBreak()
    {
        return SetContent(new PageBreakElement());
    }

    /// <summary>
    /// Renders each item in <paramref name="items"/> using the provided template, stacked vertically.
    /// </summary>
    /// <typeparam name="T">Type of each data item.</typeparam>
    /// <param name="items">Collection of data items to render.</param>
    /// <param name="itemTemplate">
    /// Delegate that configures a <see cref="ContainerBuilder"/> for each item.
    /// </param>
    /// <param name="spacing">Vertical gap between items, in points.</param>
    public ContainerBuilder List<T>(IEnumerable<T> items, Action<ContainerBuilder, T> itemTemplate, float spacing = 0)
    {
        var column = new ColumnElement { Spacing = spacing };
        foreach (var item in items)
        {
            var cb = new ContainerBuilder();
            itemTemplate(cb, item);
            column.Items.Add(new LazyElement(cb));
        }
        return SetContent(column);
    }




    /// <summary>Adds a chart. Drawn in PDF; HTML and Excel render its fallback (nothing by default).</summary>
    public ChartBuilder Chart()
    {
        var chart = new ChartElement();
        SetContent(chart);
        return new ChartBuilder(chart, this);
    }

    /// <summary>
    /// Inserts the content of every page of <paramref name="nested"/> here, one after another.
    /// The nested document's headers, footers, sizes and margins are not used.
    /// </summary>
    public ContainerBuilder Subreport(Document nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        var column = new ColumnElement();
        foreach (var page in nested.Settings.Pages)
            if (page.ContentElement != null)
                column.Items.Add(page.ContentElement);
        return SetContent(column);
    }

    /// <summary>Uses a custom element as content. Renderers must know the element type (or it must be an <see cref="IRendererSpecificElement"/>).</summary>
    public ContainerBuilder Element(IElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return SetContent(element);
    }

    private ContainerBuilder SetContent(IElement content)
    {
        if (_child != null)
            throw new InvalidOperationException(
                $"This container already has content ({_child.GetType().Name}). Use Column() or Row() to place several elements.");
        _child = content;
        return this;
    }

    internal IElement Build()
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
                Border = _border ?? new() { Width = 0 },
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
