using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>The width of one table column.</summary>
public class TableColumnDefinition
{
    /// <summary>Width in points; <c>null</c> makes the column relative.</summary>
    public float? FixedWidth { get; set; }
    /// <summary>Share of the width left by the fixed columns, relative to the other relative columns. Default 1.</summary>
    public float RelativeWidth { get; set; } = 1;
    /// <summary>True when the column has no fixed width.</summary>
    public bool IsRelative => !FixedWidth.HasValue;
}

/// <summary>One cell of a table.</summary>
public class TableCell
{
    /// <summary>The cell's content.</summary>
    public IElement? Content { get; set; }

    /// <summary>Number of columns this cell spans. Default is 1.</summary>
    public int ColumnSpan { get; set; } = 1;
}

/// <summary>
/// A grid of cells. Cells are listed left-to-right, top-to-bottom; rows are formed by
/// filling <see cref="Columns"/> while respecting each cell's <see cref="TableCell.ColumnSpan"/>
/// (see <see cref="PartitionIntoRows"/>).
/// </summary>
public class TableElement : IElement
{
    /// <summary>The columns, left to right.</summary>
    public List<TableColumnDefinition> Columns { get; } = new();
    /// <summary>Cells of the header rows, repeated on every page the table continues on.</summary>
    public List<TableCell> HeaderCells { get; } = new();
    /// <summary>Cells of the data rows.</summary>
    public List<TableCell> DataCells { get; } = new();

    /// <summary>Rows rendered after the data rows (totals, notes).</summary>
    public List<TableCell> FooterCells { get; } = new();
    /// <summary>Width of the border drawn around every cell; 0 draws none.</summary>
    public float BorderWidth { get; set; }
    /// <summary>Color of the cell borders. Default black.</summary>
    public ReportColor BorderColor { get; set; } = ReportColor.Black;

    /// <summary>
    /// Splits a flat list of cells into rows of <see cref="Columns"/>.<c>Count</c> columns.
    /// Each entry is <c>(cell, startColumn, effectiveSpan)</c>; a span is clamped to the columns left in its row.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<(TableCell Cell, int StartColumn, int Span)>> PartitionIntoRows(IEnumerable<TableCell> cells)
    {
        int cols = Columns.Count;
        var rows = new List<IReadOnlyList<(TableCell, int, int)>>();
        if (cols == 0) return rows;

        var row = new List<(TableCell, int, int)>();
        int colIdx = 0;
        foreach (var cell in cells)
        {
            int span = Math.Clamp(cell.ColumnSpan, 1, cols - colIdx);
            row.Add((cell, colIdx, span));
            colIdx += span;
            if (colIdx >= cols)
            {
                rows.Add(row);
                row = new List<(TableCell, int, int)>();
                colIdx = 0;
            }
        }
        if (row.Count > 0) rows.Add(row);
        return rows;
    }
}
