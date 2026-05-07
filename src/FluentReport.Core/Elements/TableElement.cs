using FluentReport.Core;
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
    /// <summary>Number of rows this cell spans. Rendering currently treats this as 1; the model is preserved for future use.</summary>
    public int RowSpan { get; set; } = 1;
    public bool IsHeader { get; set; }
}

public class TableElement : ElementBase
{
    public List<TableColumnDefinition> Columns { get; } = new();
    public List<TableCell> HeaderCells { get; } = new();
    public List<TableCell> DataCells { get; } = new();
    public float BorderWidth { get; set; } = 0;
    public ReportColor BorderColor { get; set; } = ReportColor.Black;

    private float[] GetColumnWidths(float availableWidth)
    {
        if (Columns.Count == 0) return Array.Empty<float>();
        var fixedTotal = Columns.Where(c => !c.IsRelative).Sum(c => c.FixedWidth ?? 0);
        var relTotal = Columns.Where(c => c.IsRelative).Sum(c => c.RelativeWidth);
        var remaining = Math.Max(0f, availableWidth - fixedTotal);
        return Columns.Select(c => c.IsRelative
            ? (relTotal > 0 ? remaining * (c.RelativeWidth / relTotal) : 0)
            : (c.FixedWidth ?? 0)).ToArray();
    }

    /// <summary>
    /// Partitions a flat list of cells into rows, respecting <see cref="TableCell.ColumnSpan"/>.
    /// Each element is <c>(cell, startColumnIndex, effectiveSpan)</c>.
    /// </summary>
    private List<List<(TableCell Cell, int StartCol, int Span)>> PartitionIntoRows(IList<TableCell> cells, int cols)
    {
        var rows = new List<List<(TableCell, int, int)>>();
        var row = new List<(TableCell, int, int)>();
        int colIdx = 0;

        foreach (var cell in cells)
        {
            // Start a new row when the current row is full.
            if (colIdx >= cols)
            {
                rows.Add(row);
                row = new List<(TableCell, int, int)>();
                colIdx = 0;
            }

            int span = Math.Max(1, Math.Min(cell.ColumnSpan, cols - colIdx));
            row.Add((cell, colIdx, span));
            colIdx += span;

            // If this cell fills the row exactly, seal the row.
            if (colIdx >= cols)
            {
                rows.Add(row);
                row = new List<(TableCell, int, int)>();
                colIdx = 0;
            }
        }

        if (row.Count > 0)
            rows.Add(row);

        return rows;
    }

    public override Size Measure(MeasureContext context)
    {
        var colWidths = GetColumnWidths(context.AvailableWidth);
        var cols = Columns.Count;
        if (cols == 0) return new(context.AvailableWidth, 0);

        float totalHeight = 0;
        totalHeight += MeasureRows(HeaderCells, colWidths, cols, context);
        totalHeight += MeasureRows(DataCells, colWidths, cols, context);
        return new(context.AvailableWidth, totalHeight);
    }

    private float MeasureRows(IList<TableCell> cells, float[] colWidths, int cols, MeasureContext context)
    {
        float total = 0;
        foreach (var row in PartitionIntoRows(cells, cols))
        {
            float rowHeight = 0;
            foreach (var (cell, startCol, span) in row)
            {
                float cellWidth = SumWidths(colWidths, startCol, span);
                var s = cell.Content?.Measure(context.WithDimensions(cellWidth, context.AvailableHeight)) ?? Size.Zero;
                if (s.Height > rowHeight) rowHeight = s.Height;
            }
            total += rowHeight;
        }
        return total;
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        var colWidths = GetColumnWidths(size.Width);
        var cols = Columns.Count;
        if (cols == 0) return;

        float y = position.Y;
        y = RenderRows(context, position, size, HeaderCells, colWidths, cols, y);
        RenderRows(context, position, size, DataCells, colWidths, cols, y);
    }

    private float RenderRows(RenderContext ctx, Position position, Size size, IList<TableCell> cells, float[] colWidths, int cols, float y)
    {
        foreach (var row in PartitionIntoRows(cells, cols))
        {
            // Measure row height first
            float rowHeight = 0;
            foreach (var (cell, startCol, span) in row)
            {
                float cellWidth = SumWidths(colWidths, startCol, span);
                var s = cell.Content?.Measure(ctx.MeasureContextFor(cellWidth, size.Height)) ?? Size.Zero;
                if (s.Height > rowHeight) rowHeight = s.Height;
            }

            // Render each cell in the row
            foreach (var (cell, startCol, span) in row)
            {
                float cellX = position.X + SumWidths(colWidths, 0, startCol);
                float cellWidth = SumWidths(colWidths, startCol, span);
                var cellPos = new Position(cellX, y);
                var cellSize = new Size(cellWidth, rowHeight);
                cell.Content?.Render(ctx, cellPos, cellSize);

                if (BorderWidth > 0)
                    ctx.Canvas.DrawStrokedRect(cellX, y, cellWidth, rowHeight, BorderColor, BorderWidth);
            }

            y += rowHeight;
        }
        return y;
    }

    private static float SumWidths(float[] colWidths, int startCol, int span)
    {
        float total = 0;
        for (int c = startCol; c < startCol + span && c < colWidths.Length; c++)
            total += colWidths[c];
        return total;
    }
}
