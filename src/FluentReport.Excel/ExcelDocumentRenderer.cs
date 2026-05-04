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
/// Elements without a meaningful Excel equivalent (images, spacers) are skipped.
/// </summary>
public class ExcelDocumentRenderer
{
    private readonly DocumentSettings _settings;

    /// <summary>Number of virtual Excel columns used to distribute layout. Default is 10.</summary>
    public int TotalColumns { get; set; } = 10;

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
        // Flatten top-level content items so PageBreak elements can create new sheets
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

    private static void WriteElement(
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

            case LineElement:
                WriteLineRow(sheet, ref row, startCol, endCol);
                break;

            case SpacerElement:
                row++; // blank row
                break;

            // PageBreakElement handled upstream; ImageElement not supported in Excel
            default:
                break;
        }
    }

    private static void WriteColumn(
        IXLWorksheet sheet,
        ColumnElement col,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        foreach (var item in col.Items)
            WriteElement(sheet, item, ref row, startCol, endCol, style);
    }

    private static void WriteRow(
        IXLWorksheet sheet,
        RowElement rowEl,
        ref int row,
        int startCol,
        int endCol,
        StyleInfo? style)
    {
        if (rowEl.Items.Count == 0) return;

        int availableCols = endCol - startCol;
        int[] colWidths = DistributeColumns(
            rowEl.Items.Select(i => (i.IsRelative, i.IsRelative ? i.RelativeWidth : i.FixedWidth ?? 1f)).ToList(),
            availableCols);

        int startRow = row;
        int maxRow = row;
        int colOffset = startCol;

        for (int i = 0; i < rowEl.Items.Count; i++)
        {
            var item = rowEl.Items[i];
            if (item.Element == null) { colOffset += colWidths[i]; continue; }

            int itemRow = startRow;
            WriteElement(sheet, item.Element, ref itemRow, colOffset, colOffset + colWidths[i], style);
            if (itemRow > maxRow) maxRow = itemRow;
            colOffset += colWidths[i];
        }

        row = maxRow;
    }

    private static void WriteTable(
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

                IXLCell xlCell;
                if (colWidths[c] > 1)
                {
                    sheet.Range(row, colOffset, row, cellEndCol).Merge();
                }
                xlCell = sheet.Cell(row, colOffset);

                xlCell.Value = ExtractText(cell.Content);

                var textStyle = ExtractTextStyle(cell.Content);
                if (textStyle != null)
                {
                    ApplyTextStyle(xlCell, textStyle);
                }
                if (isHeader)
                    xlCell.Style.Font.Bold = true;

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
        var sb = new System.Text.StringBuilder();
        foreach (var span in text.Spans)
        {
            sb.Append(span.IsCurrentPage ? "1"
                    : span.IsTotalPages ? "?"
                    : span.StaticText ?? "");
        }

        string cellText = sb.ToString();

        IXLCell cell;
        if (endCol - startCol > 1)
        {
            sheet.Range(row, startCol, row, endCol - 1).Merge();
        }
        cell = sheet.Cell(row, startCol);
        cell.Value = cellText;

        // Text style from first span (or element-level style)
        var textStyle = text.Spans.Count > 0 ? text.Spans[0].Style : text.Style;
        ApplyTextStyle(cell, textStyle);

        // Override with wrapper style
        if (style?.Bold == true) cell.Style.Font.Bold = true;
        if (style?.BackgroundColor.HasValue == true)
            cell.Style.Fill.BackgroundColor = ToXLColor(style.BackgroundColor.Value);
        if (style?.Border != null && style.Border.Width > 0)
            ApplyBorder(cell, style.Border);
        if (style?.Alignment.HasValue == true)
            cell.Style.Alignment.Horizontal = MapHorizontalAlignment(style.Alignment.Value);

        row++;
    }

    private static void WriteBorderElement(
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

    private static void WriteLineRow(IXLWorksheet sheet, ref int row, int startCol, int endCol)
    {
        for (int c = startCol; c < endCol; c++)
        {
            var cell = sheet.Cell(row, c);
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.Black;
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

    private static int[] DistributeColumns(List<(bool isRelative, float weight)> items, int totalCols)
    {
        if (items.Count == 0) return [];
        double totalWeight = items.Sum(i => (double)i.weight);
        var result = new int[items.Count];
        int allocated = 0;
        for (int i = 0; i < items.Count - 1; i++)
        {
            result[i] = Math.Max(1, (int)Math.Round(items[i].weight / totalWeight * totalCols));
            allocated += result[i];
        }
        result[^1] = Math.Max(1, totalCols - allocated);
        return result;
    }

    // ── Utilities ────────────────────────────────────────────────────────────

    private static IElement Resolve(IElement element) =>
        element is LazyElement lazy ? lazy.Built : element;

    private static List<IElement> GetTopLevelElements(IElement? content)
    {
        if (content == null) return [];
        var resolved = Resolve(content);
        if (resolved is ColumnElement column)
            return column.Items.ToList();
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
