using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Elements;

/// <summary>Defines the visual appearance of a data series in a chart.</summary>
public class ChartSeries
{
    /// <summary>Display name shown in the legend.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Numeric values, one per category.</summary>
    public IReadOnlyList<double> Values { get; set; } = Array.Empty<double>();

    /// <summary>Color used to draw bars or lines for this series.</summary>
    public ReportColor Color { get; set; } = ReportColor.Black;
}

/// <summary>Type of chart to render.</summary>
public enum ChartType { Bar, Line }

/// <summary>
/// Renders a basic bar or line chart using SkiaSharp.
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

    internal ReportColor[] GetDefaultColors() => DefaultColors;

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

        // ── Background ───────────────────────────────────────────────────────
        using (var bgPaint = new SKPaint { Color = SKColors.White })
            canvas.DrawRect(position.X, position.Y, size.Width, size.Height, bgPaint);

        // ── Title ─────────────────────────────────────────────────────────────
        if (Title != null)
        {
            using var titleTypeface = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                                      ?? SKTypeface.Default;
            using var titleFont = new SKFont(titleTypeface, 13);
            using var titlePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            float titleX = position.X + size.Width / 2f;
            float titleY = position.Y + titleHeight;
            canvas.DrawText(Title, titleX, titleY, SKTextAlign.Center, titleFont, titlePaint);
        }

        // ── Compute min/max ──────────────────────────────────────────────────
        double maxVal = Series.SelectMany(s => s.Values).DefaultIfEmpty(1).Max();
        double minVal = Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Min();
        if (minVal > 0) minVal = 0;
        double range = maxVal - minVal;
        if (range == 0) range = 1;

        // ── Grid lines + Y-axis labels ────────────────────────────────────────
        using var gridPaint = new SKPaint { Color = new SKColor(220, 220, 220), StrokeWidth = 0.5f, Style = SKPaintStyle.Stroke };
        using var axisTypeface = SKTypeface.FromFamilyName("sans-serif") ?? SKTypeface.Default;
        using var axisFont = new SKFont(axisTypeface, 9);
        using var axisPaint = new SKPaint { Color = SKColors.DarkGray, IsAntialias = true };

        const int gridLines = 5;
        for (int g = 0; g <= gridLines; g++)
        {
            float gy = plotY + plotH - plotH * g / gridLines;
            canvas.DrawLine(plotX, gy, plotX + plotW, gy, gridPaint);

            double labelVal = minVal + range * g / gridLines;
            string labelText = labelVal >= 1000 ? $"{labelVal / 1000:0.#}k" : $"{labelVal:0.##}";
            canvas.DrawText(labelText, plotX - 3, gy + 4, SKTextAlign.Right, axisFont, axisPaint);
        }

        // ── Axis borders ─────────────────────────────────────────────────────
        using var axisBorderPaint = new SKPaint { Color = SKColors.DarkGray, StrokeWidth = 1, Style = SKPaintStyle.Stroke };
        canvas.DrawLine(plotX, plotY, plotX, plotY + plotH, axisBorderPaint);
        canvas.DrawLine(plotX, plotY + plotH, plotX + plotW, plotY + plotH, axisBorderPaint);

        // ── Data rendering ───────────────────────────────────────────────────
        int catCount = Math.Max(1, CategoryLabels.Count > 0 ? CategoryLabels.Count
            : Series.Select(s => s.Values.Count).DefaultIfEmpty(0).Max());

        float catWidth = plotW / catCount;

        if (Type == ChartType.Bar)
            RenderBars(canvas, catCount, catWidth, plotX, plotY, plotH, minVal, range, axisFont, axisPaint);
        else
            RenderLines(canvas, catCount, catWidth, plotX, plotY, plotH, minVal, range);

        // ── X-axis category labels ────────────────────────────────────────────
        for (int c = 0; c < catCount; c++)
        {
            float cx = plotX + catWidth * c + catWidth / 2f;
            string label = c < CategoryLabels.Count ? CategoryLabels[c] : (c + 1).ToString();
            canvas.DrawText(label, cx, plotY + plotH + axisBottom - 4, SKTextAlign.Center, axisFont, axisPaint);
        }

        // ── Legend ────────────────────────────────────────────────────────────
        if (Series.Count > 0)
        {
            float legendY = plotY + plotH + axisBottom + padding;
            float legendX = plotX;

            foreach (var series in Series)
            {
                using var legendDotPaint = new SKPaint { Color = series.Color.ToSkColor(), Style = SKPaintStyle.Fill };
                canvas.DrawRect(legendX, legendY - 8, 10, 10, legendDotPaint);
                canvas.DrawText(series.Label, legendX + 14, legendY, SKTextAlign.Left, axisFont, axisPaint);
                legendX += axisFont.MeasureText(series.Label) + 28;
            }
        }
    }

    private void RenderBars(SKCanvas canvas, int catCount, float catWidth,
        float plotX, float plotY, float plotH,
        double minVal, double range,
        SKFont axisFont, SKPaint axisPaint)
    {
        float groupWidth = catWidth * 0.8f;
        float barWidth = Series.Count > 0 ? groupWidth / Series.Count : groupWidth;
        float groupOffset = catWidth * 0.1f;

        for (int si = 0; si < Series.Count; si++)
        {
            var series = Series[si];
            var color = series.Color.Equals(default(ReportColor)) ? DefaultColors[si % DefaultColors.Length] : series.Color;
            using var barPaint = new SKPaint { Color = color.ToSkColor(), Style = SKPaintStyle.Fill, IsAntialias = true };

            for (int c = 0; c < catCount; c++)
            {
                double val = si < Series.Count && c < series.Values.Count ? series.Values[c] : 0;
                float barH = (float)(plotH * (val - minVal) / range);
                float bx = plotX + catWidth * c + groupOffset + barWidth * si;
                float by = plotY + plotH - barH;
                canvas.DrawRect(bx, by, barWidth - 1, barH, barPaint);
            }
        }
    }

    private void RenderLines(SKCanvas canvas, int catCount, float catWidth,
        float plotX, float plotY, float plotH,
        double minVal, double range)
    {
        for (int si = 0; si < Series.Count; si++)
        {
            var series = Series[si];
            var color = series.Color.Equals(default(ReportColor)) ? DefaultColors[si % DefaultColors.Length] : series.Color;
            using var linePaint = new SKPaint { Color = color.ToSkColor(), StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
            using var dotPaint = new SKPaint { Color = color.ToSkColor(), Style = SKPaintStyle.Fill, IsAntialias = true };

            var path = new SKPath();
            bool first = true;

            for (int c = 0; c < catCount; c++)
            {
                double val = c < series.Values.Count ? series.Values[c] : 0;
                float px = plotX + catWidth * c + catWidth / 2f;
                float py = plotY + plotH - (float)(plotH * (val - minVal) / range);

                if (first) { path.MoveTo(px, py); first = false; }
                else path.LineTo(px, py);

                canvas.DrawCircle(px, py, 3, dotPaint);
            }

            canvas.DrawPath(path, linePaint);
        }
    }
}
