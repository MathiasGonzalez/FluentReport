using FluentReport.Styling;

namespace FluentReport.Elements;

public class TableColumnDefinition
{
    public float? FixedWidth { get; set; }
    public float RelativeWidth { get; set; } = 1;
    public bool IsRelative => !FixedWidth.HasValue;
}

public class TableCell
{
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
    public List<TableColumnDefinition> Columns { get; } = new();
    public List<TableCell> HeaderCells { get; } = new();
    public List<TableCell> DataCells { get; } = new();

    /// <summary>Rows rendered after the data rows (totals, notes).</summary>
    public List<TableCell> FooterCells { get; } = new();
    public float BorderWidth { get; set; }
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
