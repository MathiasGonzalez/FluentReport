using FluentReport.Core;
using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>Defines the visual appearance of a data series in a chart.</summary>
public class ChartSeries
{
    /// <summary>Display name shown in the legend.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Numeric values, one per category.</summary>
    public IReadOnlyList<double> Values { get; set; } = Array.Empty<double>();

    /// <summary>Color used to draw bars or lines for this series. When <c>null</c>, a palette color is chosen automatically.</summary>
    public ReportColor? Color { get; set; } = null;
}

/// <summary>Type of chart to render.</summary>
public enum ChartType { Bar, Line }

/// <summary>
/// Renders a basic bar or line chart.
/// Supports multiple series, category labels, and an optional title.
/// </summary>
public class ChartElement : ElementBase
{
    private static readonly ReportColor[] DefaultColors =
    [
        new(70, 130, 180),   // steel blue
        new(220, 80, 80),    // red
        new(80, 160, 80),    // green
        new(210, 140, 40),   // orange
        new(140, 80, 200),   // purple
        new(60, 180, 180),   // teal
    ];

    private static readonly ReportColor GridColor = new(220, 220, 220);
    private static readonly ReportColor AxisColor = new(105, 105, 105);

    private static readonly TextStyle TitleStyle = new() { FontSize = 13, Bold = true };
    private static readonly TextStyle AxisStyle = new() { FontSize = 9, Color = new ReportColor(105, 105, 105) };

    /// <summary>Type of chart: bar or line.</summary>
    public ChartType Type { get; set; } = ChartType.Bar;

    /// <summary>Optional chart title drawn at the top.</summary>
    public string? Title { get; set; }

    /// <summary>Category (X-axis) labels.</summary>
    public List<string> CategoryLabels { get; } = new();

    /// <summary>Data series.</summary>
    public List<ChartSeries> Series { get; } = new();

    /// <summary>Fixed height of the chart area, in points. Default 200.</summary>
    public float FixedHeight { get; set; } = 200f;

    public override Size Measure(MeasureContext context)
        => new(context.AvailableWidth, FixedHeight);

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (Series.Count == 0) return;

        const float titleHeight = 20f;
        const float legendHeight = 18f;
        const float axisLeft = 45f;
        const float axisBottom = 30f;
        const float padding = 8f;

        float titleOffset = Title != null ? titleHeight + padding : 0f;
        float legendOffset = Series.Count > 0 ? legendHeight + padding : 0f;

        float plotX = position.X + axisLeft;
        float plotY = position.Y + titleOffset + padding;
        float plotW = size.Width - axisLeft - padding;
        float plotH = size.Height - titleOffset - legendOffset - axisBottom - padding * 2;

        if (plotW <= 0 || plotH <= 0) return;

        var canvas = context.Canvas;

        // ── Title ─────────────────────────────────────────────────────────────
        if (Title != null)
        {
            float titleX = position.X + size.Width / 2f;
            float titleY = position.Y + titleHeight;
            canvas.DrawText(Title, titleX, titleY, DrawTextAlign.Center, TitleStyle);
        }

        // ── Compute min/max ──────────────────────────────────────────────────
        double maxVal = Series.SelectMany(s => s.Values).DefaultIfEmpty(1).Max();
        double minVal = Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Min();
        if (minVal > 0) minVal = 0;
        double range = maxVal - minVal;
        if (range == 0) range = 1;

        // ── Grid lines + Y-axis labels ────────────────────────────────────────
        const int gridLines = 5;
        for (int g = 0; g <= gridLines; g++)
        {
            float gy = plotY + plotH - plotH * g / gridLines;
            canvas.DrawLine(plotX, gy, plotX + plotW, gy, GridColor, 0.5f);

            double labelVal = minVal + range * g / gridLines;
            string labelText = labelVal >= 1000 ? $"{labelVal / 1000:0.#}k" : $"{labelVal:0.##}";
            canvas.DrawText(labelText, plotX - 3, gy + 4, DrawTextAlign.Right, AxisStyle);
        }

        // ── Axis borders ─────────────────────────────────────────────────────
        canvas.DrawLine(plotX, plotY, plotX, plotY + plotH, AxisColor, 1);
        canvas.DrawLine(plotX, plotY + plotH, plotX + plotW, plotY + plotH, AxisColor, 1);

        // ── Data rendering ───────────────────────────────────────────────────
        int catCount = Math.Max(1, CategoryLabels.Count > 0 ? CategoryLabels.Count
            : Series.Select(s => s.Values.Count).DefaultIfEmpty(0).Max());

        float catWidth = plotW / catCount;

        if (Type == ChartType.Bar)
            RenderBars(canvas, catCount, catWidth, plotX, plotY, plotH, minVal, range);
        else
            RenderLines(canvas, catCount, catWidth, plotX, plotY, plotH, minVal, range);

        // ── X-axis category labels ────────────────────────────────────────────
        for (int c = 0; c < catCount; c++)
        {
            float cx = plotX + catWidth * c + catWidth / 2f;
            string label = c < CategoryLabels.Count ? CategoryLabels[c] : (c + 1).ToString();
            canvas.DrawText(label, cx, plotY + plotH + axisBottom - 4, DrawTextAlign.Center, AxisStyle);
        }

        // ── Legend ────────────────────────────────────────────────────────────
        if (Series.Count > 0)
        {
            float legendY = plotY + plotH + axisBottom + padding;
            float legendX = plotX;

            foreach (var (series, si) in Series.Select((s, i) => (s, i)))
            {
                var legendColor = series.Color ?? DefaultColors[si % DefaultColors.Length];
                canvas.DrawFilledRect(legendX, legendY - 8, 10, 10, legendColor);
                canvas.DrawText(series.Label, legendX + 14, legendY, DrawTextAlign.Left, AxisStyle);
                legendX += canvas.MeasureText(series.Label, AxisStyle) + 28;
            }
        }
    }

    private void RenderBars(IDrawingCanvas canvas, int catCount, float catWidth,
        float plotX, float plotY, float plotH,
        double minVal, double range)
    {
        float groupWidth = catWidth * 0.8f;
        float barWidth = Series.Count > 0 ? groupWidth / Series.Count : groupWidth;
        float groupOffset = catWidth * 0.1f;

        for (int si = 0; si < Series.Count; si++)
        {
            var series = Series[si];
            var color = series.Color ?? DefaultColors[si % DefaultColors.Length];

            for (int c = 0; c < catCount; c++)
            {
                double val = si < Series.Count && c < series.Values.Count ? series.Values[c] : 0;
                float barH = (float)(plotH * (val - minVal) / range);
                float bx = plotX + catWidth * c + groupOffset + barWidth * si;
                float by = plotY + plotH - barH;
                canvas.DrawFilledRect(bx, by, barWidth - 1, barH, color);
            }
        }
    }

    private void RenderLines(IDrawingCanvas canvas, int catCount, float catWidth,
        float plotX, float plotY, float plotH,
        double minVal, double range)
    {
        for (int si = 0; si < Series.Count; si++)
        {
            var series = Series[si];
            var color = series.Color ?? DefaultColors[si % DefaultColors.Length];

            var points = new List<(float X, float Y)>(catCount);
            for (int c = 0; c < catCount; c++)
            {
                double val = c < series.Values.Count ? series.Values[c] : 0;
                float px = plotX + catWidth * c + catWidth / 2f;
                float py = plotY + plotH - (float)(plotH * (val - minVal) / range);
                points.Add((px, py));
                canvas.DrawCircle(px, py, 3, color);
            }

            canvas.DrawPolyline(points, color, 2);
        }
    }
}
