using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

/// <summary>Fluent builder for <see cref="ChartElement"/>.</summary>
public class ChartBuilder
{
    private readonly ChartElement _chart;
    private readonly ContainerBuilder _parent;

    internal ChartBuilder(ChartElement chart, ContainerBuilder parent)
    {
        _chart = chart;
        _parent = parent;
    }

    /// <summary>Sets the chart type (bar or line).</summary>
    public ChartBuilder Type(ChartType type) { _chart.Type = type; return this; }

    /// <summary>Sets an optional title drawn above the chart.</summary>
    public ChartBuilder Title(string title) { _chart.Title = title; return this; }

    /// <summary>Sets the fixed height of the chart in points. Default is 200.</summary>
    public ChartBuilder Height(float height) { _chart.FixedHeight = height; return this; }

    /// <summary>Sets the category (X-axis) labels.</summary>
    public ChartBuilder Categories(IEnumerable<string> labels)
    {
        _chart.CategoryLabels.Clear();
        _chart.CategoryLabels.AddRange(labels);
        return this;
    }

    /// <summary>Adds a data series.</summary>
    /// <param name="label">Series name shown in the legend.</param>
    /// <param name="values">Data values, one per category.</param>
    /// <param name="colorHex">Optional hex color, e.g. <c>"#4682B4"</c>. When null, a default palette color is used automatically.</param>
    public ChartBuilder AddSeries(string label, IEnumerable<double> values, string? colorHex = null)
    {
        _chart.Series.Add(new ChartSeries
        {
            Label = label,
            Values = values.ToList().AsReadOnly(),
            Color = colorHex != null ? ReportColor.FromHex(colorHex) : null
        });
        return this;
    }

    // ── Proxy methods to allow chaining back to ContainerBuilder ─────────────

    /// <inheritdoc cref="ContainerBuilder.Padding(float)"/>
    public ContainerBuilder Padding(float all) => _parent.Padding(all);

    /// <inheritdoc cref="ContainerBuilder.Background(string)"/>
    public ContainerBuilder Background(string hex) => _parent.Background(hex);
}
