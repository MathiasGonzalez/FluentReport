using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

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
    public int ColumnSpan { get; set; } = 1;
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

    private float[] GetRowHeights(float[] colWidths, IList<TableCell> cells, MeasureContext ctx)
    {
        var cols = Columns.Count;
        if (cols == 0) return Array.Empty<float>();
        var rowCount = (int)Math.Ceiling((double)cells.Count / cols);
        var heights = new float[rowCount];
        for (int i = 0; i < cells.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;
            if (col >= colWidths.Length) continue;
            var cell = cells[i];
            var s = cell.Content?.Measure(new MeasureContext { AvailableWidth = colWidths[col], AvailableHeight = ctx.AvailableHeight }) ?? Size.Zero;
            if (s.Height > heights[row]) heights[row] = s.Height;
        }
        return heights;
    }

    public override Size Measure(MeasureContext context)
    {
        var colWidths = GetColumnWidths(context.AvailableWidth);
        var headerHeights = GetRowHeights(colWidths, HeaderCells, context);
        var dataHeights = GetRowHeights(colWidths, DataCells, context);
        var totalHeight = headerHeights.Sum() + dataHeights.Sum();
        return new(context.AvailableWidth, totalHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        var colWidths = GetColumnWidths(size.Width);
        var cols = Columns.Count;
        if (cols == 0) return;

        float y = position.Y;
        y = RenderRows(context, position, size, HeaderCells, colWidths, y, cols);
        RenderRows(context, position, size, DataCells, colWidths, y, cols);
    }

    private float RenderRows(RenderContext ctx, Position position, Size size, IList<TableCell> cells, float[] colWidths, float y, int cols)
    {
        if (cells.Count == 0) return y;
        var rowCount = (int)Math.Ceiling((double)cells.Count / cols);
        for (int row = 0; row < rowCount; row++)
        {
            float rowHeight = 0;
            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                if (idx >= cells.Count) break;
                var cell = cells[idx];
                if (col >= colWidths.Length) continue;
                var s = cell.Content?.Measure(new MeasureContext { AvailableWidth = colWidths[col], AvailableHeight = size.Height }) ?? Size.Zero;
                if (s.Height > rowHeight) rowHeight = s.Height;
            }

            float x = position.X;
            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                if (idx >= cells.Count) break;
                var cell = cells[idx];
                if (col >= colWidths.Length) continue;
                var cellPos = new Position(x, y);
                var cellSize = new Size(colWidths[col], rowHeight);
                cell.Content?.Render(ctx, cellPos, cellSize);

                if (BorderWidth > 0)
                {
                    using var borderPaint = new SKPaint
                    {
                        Color = BorderColor.ToSkColor(),
                        StrokeWidth = BorderWidth,
                        Style = SKPaintStyle.Stroke
                    };
                    ctx.Canvas.DrawRect(x, y, colWidths[col], rowHeight, borderPaint);
                }

                x += colWidths[col];
            }
            y += rowHeight;
        }
        return y;
    }
}
