using ClosedXML.Excel;
using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Excel;

/// <summary>
/// Renders a FluentReport document as an Excel (.xlsx) workbook.
/// Each document page maps to an Excel worksheet.
/// PageBreak elements within content produce additional worksheets.
/// Elements without a meaningful Excel equivalent (images) are skipped.
/// </summary>
public class ExcelDocumentRenderer
{
    private readonly DocumentSettings _settings;

    /// <summary>Number of virtual Excel columns used to distribute layout. Default is 10.</summary>
    public int TotalColumns { get; set; } = 10;

    /// <summary>
    /// Assumed width (in points) of one relative unit when mixing fixed and relative columns.
    /// E.g. <c>RelativeColumn(1)</c> is treated as 150 content-points wide for proportional allocation.
    /// </summary>
    public double RelativeUnitWidthPt { get; set; } = 150.0;

    public ExcelDocumentRenderer(DocumentSettings settings) => _settings = settings;

    public void RenderToStream(Stream stream)
    {
        using var workbook = new XLWorkbook();

        int pageIndex = 0;
        foreach (var pageSettings in _settings.Pages)
        {
            pageIndex++;
            WritePageToWorkbook(workbook, pageSettings, pageIndex);
        }

        workbook.SaveAs(stream);
    }

    // ── Page → worksheet(s) ──────────────────────────────────────────────────

    private void WritePageToWorkbook(XLWorkbook workbook, PageSettings page, int pageIndex)
    {
        // Flatten top-level content items so PageBreak elements can create new sheets.
        // SpacerElements are inserted between items when the column has non-zero spacing,
        // mirroring the behavior of DocumentRenderer.GetContentElements in the PDF renderer.
        var topElements = GetTopLevelElements(page.ContentElement);

        var sheetGroups = new List<List<IElement>>();
        var currentGroup = new List<IElement>();
        sheetGroups.Add(currentGroup);

        foreach (var element in topElements)
        {
            if (Resolve(element) is PageBreakElement)
            {
                currentGroup = new List<IElement>();
                sheetGroups.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(element);
            }
        }

        for (int si = 0; si < sheetGroups.Count; si++)
        {
            string sheetName = sheetGroups.Count == 1
                ? (_settings.Pages.Count == 1 ? "Sheet1" : $"Sheet{pageIndex}")
                : $"Sheet{pageIndex}_{si + 1}";

            var sheet = workbook.AddWorksheet(sheetName);
            int row = 1;

            if (page.HeaderElement != null)
                WriteElement(sheet, page.HeaderElement, ref row, 1, TotalColumns + 1, null);

            foreach (var element in sheetGroups[si])
                WriteElement(sheet, element, ref row, 1, TotalColumns + 1, null);

            if (page.FooterElement != null)
                WriteElement(sheet, page.FooterElement, ref row, 1, TotalColumns + 1, null);

            sheet.Columns().AdjustToContents();
        }
    }

    // ── Element tree traversal ───────────────────────────────────────────────

    private void WriteElement(
        IXLWorksheet sheet,
        IElement element,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        var resolved = Resolve(element);

        switch (resolved)
        {
            case ColumnElement col:
                WriteColumn(sheet, col, ref row, startCol, endCol, style);
                break;

            case RowElement rowEl:
                WriteRow(sheet, rowEl, ref row, startCol, endCol, style);
                break;

            case TableElement table:
                WriteTable(sheet, table, ref row, startCol, endCol);
                break;

            case TextElement text:
                WriteText(sheet, text, ref row, startCol, endCol, style);
                break;

            case BorderElement border:
                WriteBorderElement(sheet, border, ref row, startCol, endCol, style);
                break;

            case PaddingElement padding:
                if (padding.Child != null)
                    WriteElement(sheet, padding.Child, ref row, startCol, endCol, style);
                break;

            case AlignElement align:
                var alignStyle = (style ?? new StyleInfo()) with { Alignment = align.Alignment };
                if (align.Child != null)
                    WriteElement(sheet, align.Child, ref row, startCol, endCol, alignStyle);
                break;

            case LineElement line:
                WriteLineRow(sheet, line, ref row, startCol, endCol);
                break;

            case SpacerElement:
                row++; // blank row
                break;

            // PageBreakElement handled upstream; ImageElement not supported in Excel
            default:
                break;
        }
    }

    private void WriteColumn(
        IXLWorksheet sheet,
        ColumnElement col,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        bool first = true;
        foreach (var item in col.Items)
        {
            // Insert a blank row between items when the column has non-zero spacing
            if (!first && col.Spacing > 0)
                row++;
            first = false;
            WriteElement(sheet, item, ref row, startCol, endCol, style);
        }
    }

    private void WriteRow(
        IXLWorksheet sheet,
        RowElement rowEl,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        if (rowEl.Items.Count == 0) return;

        // Reserve 1 virtual column as a gap between items when Spacing > 0
        bool hasSpacing = rowEl.Spacing > 0 && rowEl.Items.Count > 1;
        int spacingCols = hasSpacing ? rowEl.Items.Count - 1 : 0;
        int contentCols = Math.Max(rowEl.Items.Count, endCol - startCol - spacingCols);

        int[] colWidths = DistributeColumns(
            rowEl.Items.Select(i => (i.IsRelative, i.IsRelative ? i.RelativeWidth : i.FixedWidth ?? 1f)).ToList(),
            contentCols);

        int startRow = row;
        int maxRow = row;
        int colOffset = startCol;

        for (int i = 0; i < rowEl.Items.Count; i++)
        {
            var item = rowEl.Items[i];
            if (item.Element != null)
            {
                int itemRow = startRow;
                WriteElement(sheet, item.Element, ref itemRow, colOffset, colOffset + colWidths[i], style);
                if (itemRow > maxRow) maxRow = itemRow;
            }
            colOffset += colWidths[i];
            if (hasSpacing && i < rowEl.Items.Count - 1)
                colOffset++; // skip gap column
        }

        row = maxRow;
    }

    private void WriteTable(
        IXLWorksheet sheet,
        TableElement table,
        ref int row,
        int startCol,
        int endCol)
    {
        if (table.Columns.Count == 0) return;

        int availableCols = endCol - startCol;
        int[] colWidths = DistributeColumns(
            table.Columns.Select(c => (c.IsRelative, c.IsRelative ? c.RelativeWidth : c.FixedWidth ?? 1f)).ToList(),
            availableCols);

        WriteCellRow(sheet, table, table.HeaderCells, colWidths, ref row, startCol, isHeader: true);
        WriteCellRow(sheet, table, table.DataCells, colWidths, ref row, startCol, isHeader: false);
    }

    private static void WriteCellRow(
        IXLWorksheet sheet,
        TableElement table,
        IList<TableCell> cells,
        int[] colWidths,
        ref int row,
        int startCol,
        bool isHeader)
    {
        int cols = table.Columns.Count;
        if (cols == 0 || cells.Count == 0) return;

        int rowCount = (int)Math.Ceiling((double)cells.Count / cols);

        for (int r = 0; r < rowCount; r++)
        {
            int colOffset = startCol;
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx >= cells.Count || c >= colWidths.Length) break;

                var cell = cells[idx];
                int cellEndCol = colOffset + colWidths[c] - 1; // inclusive

                if (colWidths[c] > 1)
                    sheet.Range(row, colOffset, row, cellEndCol).Merge();

                var xlCell = sheet.Cell(row, colOffset);
                xlCell.Value = ExtractText(cell.Content);

                var textStyle = ExtractTextStyle(cell.Content);
                if (textStyle != null)
                    ApplyTextStyle(xlCell, textStyle);

                if (isHeader)
                    xlCell.Style.Font.Bold = true;

                // Apply alignment from an AlignElement wrapper on the cell content
                var cellAlignment = ExtractHorizontalAlignment(cell.Content);
                if (cellAlignment.HasValue)
                    xlCell.Style.Alignment.Horizontal = MapHorizontalAlignment(cellAlignment.Value);

                var bg = ExtractBackgroundColor(cell.Content);
                if (bg.HasValue)
                    xlCell.Style.Fill.BackgroundColor = ToXLColor(bg.Value);

                if (table.BorderWidth > 0)
                {
                    xlCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    xlCell.Style.Border.OutsideBorderColor = ToXLColor(table.BorderColor);
                }

                colOffset += colWidths[c];
            }
            row++;
        }
    }

    private static void WriteText(
        IXLWorksheet sheet,
        TextElement text,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        if (endCol - startCol > 1)
            sheet.Range(row, startCol, row, endCol - 1).Merge();

        var cell = sheet.Cell(row, startCol);

        if (text.Spans.Count > 1)
        {
            // Multi-span: use ClosedXML rich text to preserve per-span formatting
            var richText = cell.CreateRichText();
            bool hasContent = false;
            foreach (var span in text.Spans)
            {
                string spanText = span.IsCurrentPage ? "1"
                    : span.IsTotalPages ? "?"
                    : span.StaticText ?? "";
                if (spanText.Length == 0) continue;
                hasContent = true;

                var rich = richText.AddText(spanText)
                    .SetBold(span.Style.Bold)
                    .SetItalic(span.Style.Italic)
                    .SetUnderline(span.Style.Underline
                        ? XLFontUnderlineValues.Single
                        : XLFontUnderlineValues.None)
                    .SetFontSize(span.Style.FontSize)
                    .SetFontColor(ToXLColor(span.Style.Color));
                if (!string.IsNullOrEmpty(span.Style.FontFamily))
                    rich.SetFontName(span.Style.FontFamily);
            }
            _ = hasContent; // used to track whether any span added text
            cell.Style.Alignment.Horizontal = MapTextAlignment(text.Style.Alignment);
            cell.Style.Alignment.WrapText = true;
        }
        else
        {
            // Single span: apply cell-level styling
            var sb = new System.Text.StringBuilder();
            foreach (var span in text.Spans)
            {
                sb.Append(span.IsCurrentPage ? "1"
                    : span.IsTotalPages ? "?"
                    : span.StaticText ?? "");
            }
            cell.Value = sb.ToString();

            var textStyle = text.Spans.Count > 0 ? text.Spans[0].Style : text.Style;
            ApplyTextStyle(cell, textStyle);
        }

        // Apply wrapper style overrides (work for both rich text and plain text)
        if (style?.Bold == true) cell.Style.Font.Bold = true;
        if (style?.BackgroundColor.HasValue == true)
            cell.Style.Fill.BackgroundColor = ToXLColor(style.BackgroundColor.Value);
        if (style?.Border != null && style.Border.Width > 0)
            ApplyBorder(cell, style.Border);
        if (style?.Alignment.HasValue == true)
            cell.Style.Alignment.Horizontal = MapHorizontalAlignment(style.Alignment.Value);

        row++;
    }

    private void WriteBorderElement(
        IXLWorksheet sheet,
        BorderElement border,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? parentStyle)
    {
        var newStyle = new StyleInfo
        {
            BackgroundColor = border.BackgroundColor,
            Border = border.Border.Width > 0 ? border.Border : null,
            Bold = parentStyle?.Bold ?? false,
            Alignment = parentStyle?.Alignment,
        };

        if (border.Child != null)
            WriteElement(sheet, border.Child, ref row, startCol, endCol, newStyle);
    }

    private static void WriteLineRow(IXLWorksheet sheet, LineElement line, ref int row, int startCol, int endCol)
    {
        var color = ToXLColor(line.Color);
        var borderStyle = line.Thickness >= 2 ? XLBorderStyleValues.Medium : XLBorderStyleValues.Thin;

        for (int c = startCol; c < endCol; c++)
        {
            var cell = sheet.Cell(row, c);
            cell.Style.Border.BottomBorder = borderStyle;
            cell.Style.Border.BottomBorderColor = color;
        }
        row++;
    }

    // ── Content extraction helpers ───────────────────────────────────────────

    private static string ExtractText(IElement? element)
    {
        if (element == null) return "";
        return Resolve(element) switch
        {
            TextElement text => string.Concat(text.Spans.Select(s =>
                s.IsCurrentPage ? "1" : s.IsTotalPages ? "?" : s.StaticText ?? "")),
            ColumnElement col => string.Join("\n", col.Items.Select(ExtractText)),
            RowElement rowEl => string.Join(" ", rowEl.Items.Select(i => ExtractText(i.Element))),
            PaddingElement p => ExtractText(p.Child),
            BorderElement b => ExtractText(b.Child),
            AlignElement a => ExtractText(a.Child),
            _ => ""
        };
    }

    private static TextStyle? ExtractTextStyle(IElement? element)
    {
        if (element == null) return null;
        return Resolve(element) switch
        {
            TextElement text => text.Spans.Count > 0 ? text.Spans[0].Style : text.Style,
            PaddingElement p => ExtractTextStyle(p.Child),
            BorderElement b => ExtractTextStyle(b.Child),
            AlignElement a => ExtractTextStyle(a.Child),
            _ => null
        };
    }

    private static ReportColor? ExtractBackgroundColor(IElement? element)
    {
        if (element == null) return null;
        return Resolve(element) switch
        {
            BorderElement b => b.BackgroundColor,
            PaddingElement p => ExtractBackgroundColor(p.Child),
            AlignElement a => ExtractBackgroundColor(a.Child),
            _ => null
        };
    }

    /// <summary>
    /// Extracts the <see cref="HorizontalAlignment"/> from an <see cref="AlignElement"/> wrapper,
    /// if present. Used to honour <c>.AlignCenter()</c>/<c>.AlignRight()</c> on table cells.
    /// </summary>
    private static HorizontalAlignment? ExtractHorizontalAlignment(IElement? element)
    {
        if (element == null) return null;
        return Resolve(element) switch
        {
            AlignElement a => a.Alignment,
            PaddingElement p => ExtractHorizontalAlignment(p.Child),
            BorderElement b => ExtractHorizontalAlignment(b.Child),
            _ => null
        };
    }

    // ── Styling helpers ──────────────────────────────────────────────────────

    private static void ApplyTextStyle(IXLCell cell, TextStyle style)
    {
        cell.Style.Font.Bold = style.Bold;
        cell.Style.Font.Italic = style.Italic;
        cell.Style.Font.Underline = style.Underline
            ? XLFontUnderlineValues.Single
            : XLFontUnderlineValues.None;
        cell.Style.Font.FontSize = style.FontSize;
        cell.Style.Font.FontColor = ToXLColor(style.Color);
        cell.Style.Alignment.Horizontal = MapTextAlignment(style.Alignment);
        cell.Style.Alignment.WrapText = true;
    }

    private static void ApplyBorder(IXLCell cell, BorderStyle border)
    {
        var color = ToXLColor(border.Color);
        const XLBorderStyleValues thin = XLBorderStyleValues.Thin;
        if (border.Sides.HasFlag(BorderSide.Top))
        { cell.Style.Border.TopBorder = thin; cell.Style.Border.TopBorderColor = color; }
        if (border.Sides.HasFlag(BorderSide.Bottom))
        { cell.Style.Border.BottomBorder = thin; cell.Style.Border.BottomBorderColor = color; }
        if (border.Sides.HasFlag(BorderSide.Left))
        { cell.Style.Border.LeftBorder = thin; cell.Style.Border.LeftBorderColor = color; }
        if (border.Sides.HasFlag(BorderSide.Right))
        { cell.Style.Border.RightBorder = thin; cell.Style.Border.RightBorderColor = color; }
    }

    private static XLColor ToXLColor(ReportColor c) =>
        XLColor.FromArgb(c.A, c.R, c.G, c.B);

    private static XLAlignmentHorizontalValues MapTextAlignment(TextAlignment a) =>
        a switch
        {
            TextAlignment.Center => XLAlignmentHorizontalValues.Center,
            TextAlignment.Right => XLAlignmentHorizontalValues.Right,
            TextAlignment.Justify => XLAlignmentHorizontalValues.Justify,
            _ => XLAlignmentHorizontalValues.Left
        };

    private static XLAlignmentHorizontalValues MapHorizontalAlignment(HorizontalAlignment a) =>
        a switch
        {
            HorizontalAlignment.Center => XLAlignmentHorizontalValues.Center,
            HorizontalAlignment.Right => XLAlignmentHorizontalValues.Right,
            _ => XLAlignmentHorizontalValues.Left
        };

    // ── Column distribution ──────────────────────────────────────────────────

    /// <summary>
    /// Distributes <paramref name="totalCols"/> virtual Excel columns across items.
    /// Fixed-width items (points) and relative-weight items are normalized to a common
    /// unit before proportional distribution: 1 relative unit = <see cref="RelativeUnitWidthPt"/> pts.
    /// Uses the Hare-Niemeyer (largest-remainder) method so the sum always equals
    /// <paramref name="totalCols"/> and each item receives at least 1 column.
    /// </summary>
    private int[] DistributeColumns(List<(bool isRelative, float weight)> items, int totalCols)
    {
        if (items.Count == 0) return [];

        // Normalize: fixed items keep their point value;
        // relative items are scaled by RelativeUnitWidthPt so both types are in the same unit.
        var normalizedWeights = items
            .Select(i => i.isRelative ? (double)i.weight * RelativeUnitWidthPt : (double)i.weight)
            .ToList();

        return DistributeProportional(normalizedWeights, totalCols);
    }

    /// <summary>
    /// Proportional allocation using the Hare-Niemeyer (largest-remainder) method.
    /// Guarantees the result sums to exactly <paramref name="totalCols"/> and each
    /// slot receives at least 1 column (stealing from the largest allocation when needed).
    /// </summary>
    private static int[] DistributeProportional(List<double> weights, int totalCols)
    {
        int n = weights.Count;
        if (n == 0) return [];

        // Ensure we can give at least 1 column per item
        if (totalCols < n) totalCols = n;

        double total = weights.Sum();
        var alloc = new int[n];

        if (total <= 0)
        {
            // Equal distribution
            int each = totalCols / n;
            for (int i = 0; i < n; i++) alloc[i] = each;
            alloc[0] += totalCols - each * n;
            return alloc;
        }

        // Floor allocations
        var exact = weights.Select(w => w / total * totalCols).ToArray();
        var floors = exact.Select(v => (int)v).ToArray();
        int remaining = totalCols - floors.Sum();

        // Distribute leftover columns to items with the largest remainders
        var byRemainder = Enumerable.Range(0, n)
            .OrderByDescending(i => exact[i] - floors[i]);
        foreach (int i in byRemainder.Take(remaining))
            floors[i]++;

        // Enforce minimum 1 per item (steal from the item with most columns)
        for (int i = 0; i < n; i++)
        {
            while (floors[i] < 1)
            {
                int maxIdx = Array.IndexOf(floors, floors.Max());
                if (floors[maxIdx] <= 1) { floors[i] = 1; break; } // nothing left to steal
                floors[maxIdx]--;
                floors[i]++;
            }
        }

        return floors;
    }

    // ── Utilities ────────────────────────────────────────────────────────────

    private static IElement Resolve(IElement element) =>
        element is LazyElement lazy ? lazy.Built : element;

    /// <summary>
    /// Returns the top-level items of the content element, inserting
    /// <see cref="SpacerElement"/> instances between items when the column has
    /// non-zero spacing — mirroring the PDF renderer's <c>GetContentElements</c>.
    /// </summary>
    private static List<IElement> GetTopLevelElements(IElement? content)
    {
        if (content == null) return [];
        var resolved = Resolve(content);
        if (resolved is ColumnElement column)
        {
            if (column.Spacing <= 0) return column.Items.ToList();
            var items = new List<IElement>();
            bool first = true;
            foreach (var item in column.Items)
            {
                if (!first) items.Add(new SpacerElement(column.Spacing));
                first = false;
                items.Add(item);
            }
            return items;
        }
        return [content];
    }
}

/// <summary>Carries inherited style information while traversing the element tree.</summary>
internal record StyleInfo
{
    public ReportColor? BackgroundColor { get; init; }
    public BorderStyle? Border { get; init; }
    public bool Bold { get; init; }
    public HorizontalAlignment? Alignment { get; init; }
}

