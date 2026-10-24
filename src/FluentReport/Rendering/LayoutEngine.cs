using System.Globalization;
using System.Runtime.CompilerServices;
using FluentReport.Elements;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>Result of fitting an element into the space left on a page.</summary>
/// <param name="Head">The part drawn on this page, or <c>null</c> when nothing fits.</param>
/// <param name="Tail">The part that continues on the next page, or <c>null</c> when nothing is left.</param>
internal readonly record struct SplitResult(IElement? Head, IElement? Tail);

/// <summary>
/// Lays out the element tree for the PDF/PNG output: measures elements at a given width, splits
/// them across pages and draws them on a Skia canvas.
/// </summary>
/// <remarks>
/// Columns (nested or decorated), text (by line) and tables (by row, repeating header rows and
/// keeping footer rows at the end) split across pages. Other elements are atomic: they move to the
/// next page, or are clipped at the bottom when they are taller than an empty page.
/// </remarks>
internal sealed class LayoutEngine : IDisposable
{
    private static readonly ReportColor[] ChartPalette =
    [
        new(70, 130, 180), new(220, 80, 80), new(80, 160, 80),
        new(210, 140, 40), new(140, 80, 200), new(60, 180, 180),
    ];

    private readonly FontCache _fonts;
    private readonly Dictionary<byte[], SKImage?> _images = new(ReferenceEqualityComparer.Instance);
    private readonly ConditionalWeakTable<IElement, Dictionary<float, float>> _heights = new();
    private readonly ConditionalWeakTable<TextElement, Dictionary<float, TextBlock>> _textBlocks = new();

    public LayoutEngine(PdfOptions? options) => _fonts = new FontCache(options?.FontResolver);

    /// <summary>1-based page being drawn; read by page-number spans.</summary>
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    // ── Measuring ────────────────────────────────────────────────────────────

    public float Measure(IElement element, float width)
    {
        var cache = _heights.GetOrCreateValue(element);
        if (cache.TryGetValue(width, out var cached)) return cached;
        return cache[width] = MeasureCore(element, width);
    }

    private float MeasureCore(IElement element, float width) => element switch
    {
        TextElement text => Text(text, width).Height,
        TextBlock block => block.Height,
        ColumnElement column => MeasureColumn(column, width),
        RowElement row => RowItemWidths(row, width).Select((w, i) => row.Items[i].Element is { } e ? Measure(e, w) : 0).DefaultIfEmpty(0).Max(),
        TableElement table => TableRows(table, width).Sum(r => r.Height),
        PaddingElement padding => padding.Top + padding.Bottom + (padding.Child is { } c ? Measure(c, Inner(width, padding)) : 0),
        BorderElement border => border.Child is { } c ? Measure(c, width) : 0,
        AlignElement align => align.Child is { } c ? Measure(c, width) : 0,
        LineElement line => line.Thickness,
        SpacerElement spacer => spacer.Height,
        PageBreakElement => 0,
        ImageElement image => ImageSize(image, width).Height,
        ChartElement chart => chart.FixedHeight,
        CanvasElement canvas => canvas.Height,
        IRendererSpecificElement specific => specific.Fallback is { } f ? Measure(f, width) : 0,
        _ => throw new NotSupportedException($"The PDF renderer does not support {element.GetType().Name}.")
    };

    private float MeasureColumn(ColumnElement column, float width)
    {
        float total = 0;
        bool first = true;
        foreach (var item in column.Items)
        {
            if (item is PageBreakElement) continue;
            if (!first) total += column.Spacing;
            first = false;
            total += Measure(item, width);
        }
        return total;
    }

    /// <summary>Natural width of an element, used to align it inside a wider box.</summary>
    private float MeasureWidth(IElement element, float maxWidth) => element switch
    {
        TextElement text => Math.Min(maxWidth, Text(text, maxWidth).Width),
        TextBlock block => Math.Min(maxWidth, block.Width),
        ImageElement image => ImageSize(image, maxWidth).Width,
        ColumnElement column => column.Items.Select(i => MeasureWidth(i, maxWidth)).DefaultIfEmpty(0).Max(),
        PaddingElement p => Math.Min(maxWidth, p.Left + p.Right + (p.Child is { } c ? MeasureWidth(c, Inner(maxWidth, p)) : 0)),
        BorderElement b => b.Child is { } c ? MeasureWidth(c, maxWidth) : 0,
        CanvasElement canvas => Math.Min(maxWidth, canvas.Width),
        _ => maxWidth
    };

    private static float Inner(float width, PaddingElement p) => Math.Max(0, width - p.Left - p.Right);

    private TextBlock Text(TextElement text, float width)
    {
        var cache = _textBlocks.GetOrCreateValue(text);
        if (!cache.TryGetValue(width, out var block))
            cache[width] = block = TextLayout.Layout(text, width, _fonts);
        return block;
    }

    // ── Splitting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fits <paramref name="element"/> into <paramref name="available"/> points. On an empty page
    /// (<paramref name="pageIsEmpty"/>) something is always placed, so pagination always progresses.
    /// </summary>
    public SplitResult Split(IElement element, float width, float available, bool pageIsEmpty)
    {
        switch (element)
        {
            case ColumnElement column:
                return SplitColumn(column, width, available, pageIsEmpty);
            case TextElement text:
                return SplitText(Text(text, width), available, pageIsEmpty);
            case TextBlock block:
                return SplitText(block, available, pageIsEmpty);
            case TableElement table:
                return SplitTable(table, width, available, pageIsEmpty);
            case PaddingElement { Child: { } child } padding:
            {
                var inner = Split(child, Inner(width, padding), available - padding.Top - padding.Bottom, pageIsEmpty);
                return Wrap(inner, padding, c => new PaddingElement
                {
                    Child = c, Top = padding.Top, Bottom = padding.Bottom, Left = padding.Left, Right = padding.Right
                });
            }
            case BorderElement { Child: { } child } border:
                return Wrap(Split(child, width, available, pageIsEmpty), border,
                    c => new BorderElement { Child = c, Border = border.Border, BackgroundColor = border.BackgroundColor });
            case AlignElement { Child: { } child } align:
                return Wrap(Split(child, width, available, pageIsEmpty), align,
                    c => new AlignElement { Child = c, Alignment = align.Alignment });
            default:
                return Measure(element, width) <= available + 0.01f || pageIsEmpty
                    ? new SplitResult(element, null)
                    : new SplitResult(null, element);
        }
    }

    private static SplitResult Wrap<T>(SplitResult inner, T original, Func<IElement, T> clone) where T : IElement
    {
        if (inner.Head == null) return new SplitResult(null, original);
        if (inner.Tail == null) return new SplitResult(original, null);
        return new SplitResult(clone(inner.Head), clone(inner.Tail));
    }

    private SplitResult SplitColumn(ColumnElement column, float width, float available, bool pageIsEmpty)
    {
        var head = new List<IElement>();
        float used = 0;

        for (int i = 0; i < column.Items.Count; i++)
        {
            var item = column.Items[i];
            if (item is PageBreakElement)
            {
                if (head.Count == 0) continue; // no blank pages for leading or repeated breaks
                // An empty tail still tells the enclosing column to start a new page.
                return new SplitResult(Column(column, head), Rest(column, i + 1) ?? new ColumnElement());
            }

            float gap = head.Count > 0 ? column.Spacing : 0;
            float height = Measure(item, width);
            if (used + gap + height <= available + 0.01f && !ContainsPageBreak(item))
            {
                head.Add(item);
                used += gap + height;
                continue;
            }

            var part = Split(item, width, available - used - gap, pageIsEmpty && head.Count == 0);
            if (part.Head != null)
            {
                head.Add(part.Head);
                used += gap + Measure(part.Head, width);
                if (part.Tail == null) continue;
            }

            if (head.Count == 0) return new SplitResult(null, column);

            var tail = new List<IElement>();
            if (part.Tail != null) tail.Add(part.Tail);
            tail.AddRange(column.Items.Skip(i + 1));
            return new SplitResult(Column(column, head), tail.Count > 0 ? Column(column, tail) : null);
        }

        return new SplitResult(head.Count == column.Items.Count ? column : Column(column, head), null);
    }

    private static bool ContainsPageBreak(IElement element) => element switch
    {
        PageBreakElement => true,
        ColumnElement c => c.Items.Any(ContainsPageBreak),
        PaddingElement { Child: { } c } => ContainsPageBreak(c),
        BorderElement { Child: { } c } => ContainsPageBreak(c),
        AlignElement { Child: { } c } => ContainsPageBreak(c),
        _ => false
    };

    private static ColumnElement Column(ColumnElement template, IEnumerable<IElement> items)
    {
        var column = new ColumnElement { Spacing = template.Spacing };
        column.Items.AddRange(items);
        return column;
    }

    private static ColumnElement? Rest(ColumnElement column, int from)
        => from < column.Items.Count ? Column(column, column.Items.Skip(from)) : null;

    private static SplitResult SplitText(TextBlock block, float available, bool pageIsEmpty)
    {
        float used = 0;
        int count = 0;
        while (count < block.Lines.Count && used + block.Lines[count].Height <= available + 0.01f)
            used += block.Lines[count++].Height;

        if (count == block.Lines.Count) return new SplitResult(block, null);
        if (count == 0)
        {
            if (!pageIsEmpty) return new SplitResult(null, block);
            count = 1;
        }
        return new SplitResult(block.Slice(0, count), block.Slice(count, block.Lines.Count - count));
    }

    // ── Tables ───────────────────────────────────────────────────────────────

    private enum TableSection { Header, Data, Footer }

    private sealed record TableRow(TableSection Section, IReadOnlyList<(TableCell Cell, int StartColumn, int Span)> Cells, float Height);

    private List<TableRow> TableRows(TableElement table, float width)
    {
        var widths = ColumnWidths(table, width);
        var rows = new List<TableRow>();
        void Add(TableSection section, List<TableCell> cells)
        {
            foreach (var row in table.PartitionIntoRows(cells))
            {
                float height = row.Select(c => c.Cell.Content is { } content ? Measure(content, SpanWidth(widths, c.StartColumn, c.Span)) : 0)
                                  .DefaultIfEmpty(0).Max();
                rows.Add(new TableRow(section, row, height));
            }
        }
        Add(TableSection.Header, table.HeaderCells);
        Add(TableSection.Data, table.DataCells);
        Add(TableSection.Footer, table.FooterCells);
        return rows;
    }

    private SplitResult SplitTable(TableElement table, float width, float available, bool pageIsEmpty)
    {
        var rows = TableRows(table, width);
        if (rows.Sum(r => r.Height) <= available + 0.01f) return new SplitResult(table, null);

        float headerHeight = rows.Where(r => r.Section == TableSection.Header).Sum(r => r.Height);
        var data = rows.Where(r => r.Section == TableSection.Data).ToList();
        var footerHeight = rows.Where(r => r.Section == TableSection.Footer).Sum(r => r.Height);

        // Data rows that fit after the header; all remaining rows plus the footer must fit to finish here.
        float used = headerHeight;
        int fit = 0;
        while (fit < data.Count && used + data[fit].Height <= available + 0.01f)
            used += data[fit++].Height;

        if (fit == data.Count && used + footerHeight > available + 0.01f && fit > 0)
            fit--; // keep at least one data row with the footer on the next page

        if (fit == 0)
        {
            if (!pageIsEmpty) return new SplitResult(null, table);
            fit = Math.Min(1, data.Count);
            if (fit == 0) return new SplitResult(table, null); // header/footer alone taller than the page
        }

        int headCells = data.Take(fit).Sum(r => r.Cells.Count);
        var head = CloneTable(table, table.DataCells.Take(headCells), includeFooter: false);
        var tail = CloneTable(table, table.DataCells.Skip(headCells), includeFooter: true);
        return new SplitResult(head, tail);
    }

    private static TableElement CloneTable(TableElement table, IEnumerable<TableCell> data, bool includeFooter)
    {
        var clone = new TableElement { BorderWidth = table.BorderWidth, BorderColor = table.BorderColor };
        clone.Columns.AddRange(table.Columns);
        clone.HeaderCells.AddRange(table.HeaderCells); // repeated on every page
        clone.DataCells.AddRange(data);
        if (includeFooter) clone.FooterCells.AddRange(table.FooterCells);
        return clone;
    }

    private static float[] ColumnWidths(TableElement table, float width)
    {
        float fixedTotal = table.Columns.Where(c => !c.IsRelative).Sum(c => c.FixedWidth ?? 0);
        float relativeTotal = table.Columns.Where(c => c.IsRelative).Sum(c => c.RelativeWidth);
        float remaining = Math.Max(0, width - fixedTotal);
        return table.Columns
            .Select(c => c.IsRelative ? (relativeTotal > 0 ? remaining * c.RelativeWidth / relativeTotal : 0) : c.FixedWidth ?? 0)
            .ToArray();
    }

    private static float SpanWidth(float[] widths, int start, int span)
        => widths.Skip(start).Take(span).Sum();

    private static float[] RowItemWidths(RowElement row, float width)
    {
        float spacing = row.Items.Count > 1 ? row.Spacing * (row.Items.Count - 1) : 0;
        float fixedTotal = row.Items.Where(i => !i.IsRelative).Sum(i => i.FixedWidth ?? 0);
        float relativeTotal = row.Items.Where(i => i.IsRelative).Sum(i => i.RelativeWidth);
        float remaining = Math.Max(0, width - fixedTotal - spacing);
        return row.Items
            .Select(i => i.IsRelative ? (relativeTotal > 0 ? remaining * i.RelativeWidth / relativeTotal : 0) : i.FixedWidth ?? 0)
            .ToArray();
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    /// <summary>Draws <paramref name="element"/> in the box (x, y, width, height); height may exceed the measured height.</summary>
    public void Draw(IElement element, SKCanvas canvas, float x, float y, float width, float height, TextAlignment? alignOverride = null)
    {
        switch (element)
        {
            case TextElement text:
                DrawText(Text(text, width), canvas, x, y, width, alignOverride);
                break;
            case TextBlock block:
                DrawText(block, canvas, x, y, width, alignOverride);
                break;
            case ColumnElement column:
            {
                float cy = y;
                bool first = true;
                foreach (var item in column.Items)
                {
                    if (item is PageBreakElement) continue;
                    if (!first) cy += column.Spacing;
                    first = false;
                    float h = Measure(item, width);
                    Draw(item, canvas, x, cy, width, h);
                    cy += h;
                }
                break;
            }
            case RowElement row:
            {
                var widths = RowItemWidths(row, width);
                float cx = x;
                for (int i = 0; i < row.Items.Count; i++)
                {
                    if (row.Items[i].Element is { } item)
                        Draw(item, canvas, cx, y, widths[i], height); // full row height: backgrounds line up
                    cx += widths[i] + row.Spacing;
                }
                break;
            }
            case TableElement table:
                DrawTable(table, canvas, x, y, width);
                break;
            case PaddingElement padding:
                if (padding.Child is { } padded)
                    Draw(padded, canvas, x + padding.Left, y + padding.Top, Inner(width, padding),
                        Math.Max(0, height - padding.Top - padding.Bottom), alignOverride);
                break;
            case BorderElement border:
                DrawBorder(border, canvas, x, y, width, height, alignOverride);
                break;
            case AlignElement align:
                DrawAligned(align, canvas, x, y, width, height);
                break;
            case LineElement line:
                using (var paint = Stroke(line.Color, line.Thickness))
                    canvas.DrawLine(x, y + line.Thickness / 2, x + width, y + line.Thickness / 2, paint);
                break;
            case ImageElement image:
                DrawImage(image, canvas, x, y, width);
                break;
            case ChartElement chart:
                DrawChart(chart, canvas, x, y, width, chart.FixedHeight);
                break;
            case CanvasElement custom:
            {
                var rect = SKRect.Create(x, y, Math.Min(custom.Width, width), custom.Height);
                canvas.Save();
                canvas.ClipRect(rect);
                custom.Draw(canvas, rect);
                canvas.Restore();
                break;
            }
            case IRendererSpecificElement specific:
                if (specific.Fallback is { } fallback)
                    Draw(fallback, canvas, x, y, width, height, alignOverride);
                break;
            case SpacerElement:
            case PageBreakElement:
                break;
            default:
                throw new NotSupportedException($"The PDF renderer does not support {element.GetType().Name}.");
        }
    }

    private void DrawBorder(BorderElement border, SKCanvas canvas, float x, float y, float width, float height, TextAlignment? alignOverride)
    {
        if (border.BackgroundColor is { } background)
            using (var fill = Fill(background))
                canvas.DrawRect(x, y, width, height, fill);

        if (border.Child is { } child)
            Draw(child, canvas, x, y, width, height, alignOverride);

        var b = border.Border;
        if (b.Width <= 0 || b.Sides == BorderSide.None) return;
        using var stroke = Stroke(b.Color, b.Width);
        if (b.Sides.HasFlag(BorderSide.Top)) canvas.DrawLine(x, y, x + width, y, stroke);
        if (b.Sides.HasFlag(BorderSide.Bottom)) canvas.DrawLine(x, y + height, x + width, y + height, stroke);
        if (b.Sides.HasFlag(BorderSide.Left)) canvas.DrawLine(x, y, x, y + height, stroke);
        if (b.Sides.HasFlag(BorderSide.Right)) canvas.DrawLine(x + width, y, x + width, y + height, stroke);
    }

    private void DrawAligned(AlignElement align, SKCanvas canvas, float x, float y, float width, float height)
    {
        if (align.Child is not { } child) return;

        // Text (optionally inside padding) is aligned line by line with its real width, which keeps
        // page numbers correct; other content is shrink-wrapped and offset.
        if (IsPaddedText(child))
        {
            var textAlign = align.Alignment switch
            {
                HorizontalAlignment.Center => TextAlignment.Center,
                HorizontalAlignment.Right => TextAlignment.Right,
                _ => TextAlignment.Left
            };
            Draw(child, canvas, x, y, width, height, textAlign);
            return;
        }

        float childWidth = MeasureWidth(child, width);
        float offset = align.Alignment switch
        {
            HorizontalAlignment.Center => (width - childWidth) / 2,
            HorizontalAlignment.Right => width - childWidth,
            _ => 0
        };
        Draw(child, canvas, x + offset, y, childWidth, height);
    }

    private static bool IsPaddedText(IElement element) => element switch
    {
        TextElement or TextBlock => true,
        PaddingElement { Child: { } c } => IsPaddedText(c),
        _ => false
    };

    private void DrawText(TextBlock block, SKCanvas canvas, float x, float y, float width, TextAlignment? alignOverride)
    {
        var alignment = alignOverride ?? block.Alignment;
        float top = y;
        foreach (var line in block.Lines)
        {
            float baseline = top + (line.Height - (line.Ascent + line.Descent)) / 2 + line.Ascent;
            var texts = line.Runs.Select(r => r.Text ?? PageNumber(r.Span)).ToArray();
            var widths = line.Runs.Select((r, i) => _fonts.Measure(texts[i], r.Style)).ToArray();
            var spaces = line.Runs.Select(r => r.SpaceBefore ? _fonts.Measure(" ", r.Style) : 0).ToArray();
            float total = widths.Sum() + spaces.Sum();

            float extraPerSpace = 0;
            float cx = x;
            switch (alignment)
            {
                case TextAlignment.Center: cx += (width - total) / 2; break;
                case TextAlignment.Right: cx += width - total; break;
                case TextAlignment.Justify when !line.EndsParagraph:
                    int gaps = line.Runs.Count(r => r.SpaceBefore);
                    if (gaps > 0) extraPerSpace = Math.Max(0, width - total) / gaps;
                    break;
            }

            for (int i = 0; i < line.Runs.Count; i++)
            {
                var run = line.Runs[i];
                if (run.SpaceBefore) cx += spaces[i] + extraPerSpace;
                DrawRun(canvas, texts[i], cx, baseline, widths[i], run.Style);
                cx += widths[i];
            }
            top += line.Height;
        }
    }

    private string PageNumber(TextSpan span)
        => (span.IsTotalPages ? TotalPages : CurrentPage).ToString(CultureInfo.InvariantCulture);

    private void DrawRun(SKCanvas canvas, string text, float x, float baseline, float width, TextStyle style)
    {
        if (text.Length == 0) return;
        var font = _fonts.Font(style);
        using var paint = new SKPaint { Color = ToSk(style.EffectiveColor), IsAntialias = true };

        bool rotated = style.Rotation != 0;
        if (rotated)
        {
            canvas.Save();
            canvas.Translate(x, baseline);
            canvas.RotateDegrees(-style.Rotation); // counter-clockwise in a y-down space
            x = 0;
            baseline = 0;
        }

        canvas.DrawText(text, x, baseline, SKTextAlign.Left, font, paint);

        if (style.Underline)
        {
            var metrics = font.Metrics;
            float offset = metrics.UnderlinePosition ?? style.FontSize * 0.1f;
            float thickness = metrics.UnderlineThickness ?? style.FontSize / 15f;
            using var line = Stroke(style.EffectiveColor, thickness);
            canvas.DrawLine(x, baseline + offset, x + width, baseline + offset, line);
        }

        if (rotated) canvas.Restore();
    }

    private void DrawTable(TableElement table, SKCanvas canvas, float x, float y, float width)
    {
        var widths = ColumnWidths(table, width);
        using var stroke = table.BorderWidth > 0 ? Stroke(table.BorderColor, table.BorderWidth) : null;
        float cy = y;
        foreach (var row in TableRows(table, width))
        {
            foreach (var (cell, start, span) in row.Cells)
            {
                float cx = x + SpanWidth(widths, 0, start);
                float cw = SpanWidth(widths, start, span);
                if (cell.Content is { } content)
                    Draw(content, canvas, cx, cy, cw, row.Height);
                if (stroke != null)
                    canvas.DrawRect(cx, cy, cw, row.Height, stroke);
            }
            cy += row.Height;
        }
    }

    // ── Images ───────────────────────────────────────────────────────────────

    /// <summary>
    /// One decoded image per byte array: Skia's PDF backend embeds JPEG data as-is and writes an
    /// image used on several pages only once.
    /// </summary>
    private SKImage? Image(ImageElement element)
    {
        if (element.MimeType == null) return null;
        if (!_images.TryGetValue(element.SourceBytes, out var image))
            _images[element.SourceBytes] = image = SKImage.FromEncodedData(element.SourceBytes);
        return image;
    }

    /// <summary>Box of an image: fixed sizes win, the aspect ratio is kept, and the result never exceeds the width.</summary>
    private (float Width, float Height) ImageSize(ImageElement element, float maxWidth)
    {
        var image = Image(element);
        if (image == null || image.Width == 0 || image.Height == 0) return (0, 0);

        float ratio = (float)image.Height / image.Width;
        float w, h;
        if (element.FixedWidth is { } fw && element.FixedHeight is { } fh) (w, h) = (fw, fh);
        else if (element.FixedWidth is { } fixedW) (w, h) = (fixedW, fixedW * ratio);
        else if (element.FixedHeight is { } fixedH) (w, h) = (fixedH / ratio, fixedH);
        else (w, h) = (image.Width, image.Height);

        if (w > maxWidth && maxWidth > 0)
        {
            h *= maxWidth / w;
            w = maxWidth;
        }
        return (w, h);
    }

    private void DrawImage(ImageElement element, SKCanvas canvas, float x, float y, float width)
    {
        var image = Image(element);
        if (image == null) return;
        var (w, h) = ImageSize(element, width);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(image, SKRect.Create(x, y, w, h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
    }

    // ── Charts ───────────────────────────────────────────────────────────────

    private void DrawChart(ChartElement chart, SKCanvas canvas, float x, float y, float width, float height)
    {
        if (chart.Series.Count == 0) return;

        const float titleHeight = 20f, legendHeight = 18f, axisLeft = 45f, axisBottom = 30f, padding = 8f;
        var titleStyle = new TextStyle { FontSize = 13, Bold = true };
        var axisStyle = new TextStyle { FontSize = 9, Color = new ReportColor(105, 105, 105) };

        float titleOffset = chart.Title != null ? titleHeight + padding : 0f;
        float plotX = x + axisLeft;
        float plotY = y + titleOffset + padding;
        float plotW = width - axisLeft - padding;
        float plotH = height - titleOffset - legendHeight - padding - axisBottom - padding * 2;
        if (plotW <= 0 || plotH <= 0) return;

        if (chart.Title != null)
        {
            float tw = _fonts.Measure(chart.Title, titleStyle);
            DrawRun(canvas, chart.Title, x + (width - tw) / 2, y + titleHeight, tw, titleStyle);
        }

        var values = chart.Series.SelectMany(s => s.Values).ToList();
        double max = Math.Max(0, values.DefaultIfEmpty(0).Max());
        double min = Math.Min(0, values.DefaultIfEmpty(0).Min());
        double range = max - min;
        if (range == 0) range = 1;
        float ToY(double v) => plotY + plotH - (float)(plotH * (v - min) / range);

        using (var grid = Stroke(new ReportColor(220, 220, 220), 0.5f))
        {
            const int gridLines = 5;
            for (int g = 0; g <= gridLines; g++)
            {
                double value = min + range * g / gridLines;
                float gy = ToY(value);
                canvas.DrawLine(plotX, gy, plotX + plotW, gy, grid);
                var label = Math.Abs(value) >= 1000
                    ? (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k"
                    : value.ToString("0.##", CultureInfo.InvariantCulture);
                float lw = _fonts.Measure(label, axisStyle);
                DrawRun(canvas, label, plotX - 3 - lw, gy + 4, lw, axisStyle);
            }
        }

        using (var axis = Stroke(new ReportColor(105, 105, 105), 1))
        {
            canvas.DrawLine(plotX, plotY, plotX, plotY + plotH, axis);
            canvas.DrawLine(plotX, ToY(0), plotX + plotW, ToY(0), axis);
        }

        int categories = Math.Max(1, chart.CategoryLabels.Count > 0
            ? chart.CategoryLabels.Count
            : chart.Series.Max(s => s.Values.Count));
        float categoryWidth = plotW / categories;

        for (int si = 0; si < chart.Series.Count; si++)
        {
            var series = chart.Series[si];
            var color = series.Color ?? ChartPalette[si % ChartPalette.Length];
            if (chart.Type == ChartType.Bar)
            {
                float barWidth = categoryWidth * 0.8f / chart.Series.Count;
                using var fill = Fill(color);
                for (int c = 0; c < categories && c < series.Values.Count; c++)
                {
                    float top = ToY(Math.Max(0, series.Values[c]));
                    float bottom = ToY(Math.Min(0, series.Values[c])); // bars grow from zero
                    canvas.DrawRect(plotX + categoryWidth * c + categoryWidth * 0.1f + barWidth * si, top, barWidth - 1, bottom - top, fill);
                }
            }
            else
            {
                using var stroke = Stroke(color, 2);
                using var dot = Fill(color);
                using var path = new SKPath();
                for (int c = 0; c < categories && c < series.Values.Count; c++)
                {
                    var point = new SKPoint(plotX + categoryWidth * c + categoryWidth / 2, ToY(series.Values[c]));
                    if (c == 0) path.MoveTo(point); else path.LineTo(point);
                    canvas.DrawCircle(point, 3, dot);
                }
                canvas.DrawPath(path, stroke);
            }
        }

        for (int c = 0; c < categories; c++)
        {
            var label = c < chart.CategoryLabels.Count ? chart.CategoryLabels[c] : (c + 1).ToString(CultureInfo.InvariantCulture);
            float lw = _fonts.Measure(label, axisStyle);
            DrawRun(canvas, label, plotX + categoryWidth * c + (categoryWidth - lw) / 2, plotY + plotH + axisBottom - 4, lw, axisStyle);
        }

        // Legend, wrapped onto the available width.
        float legendX = plotX, legendY = plotY + plotH + axisBottom + padding;
        for (int si = 0; si < chart.Series.Count; si++)
        {
            var series = chart.Series[si];
            float lw = _fonts.Measure(series.Label, axisStyle);
            if (legendX > plotX && legendX + 14 + lw > x + width)
            {
                legendX = plotX;
                legendY += legendHeight;
            }
            using (var fill = Fill(series.Color ?? ChartPalette[si % ChartPalette.Length]))
                canvas.DrawRect(legendX, legendY - 8, 10, 10, fill);
            DrawRun(canvas, series.Label, legendX + 14, legendY, lw, axisStyle);
            legendX += 14 + lw + 14;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SKColor ToSk(ReportColor c) => new(c.R, c.G, c.B, c.A);

    private static SKPaint Fill(ReportColor color) => new() { Color = ToSk(color), Style = SKPaintStyle.Fill, IsAntialias = true };

    private static SKPaint Stroke(ReportColor color, float width) =>
        new() { Color = ToSk(color), Style = SKPaintStyle.Stroke, StrokeWidth = width, IsAntialias = true };

    public void Dispose()
    {
        foreach (var image in _images.Values) image?.Dispose();
        _images.Clear();
        _fonts.Dispose();
    }
}
