using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>A data series of a <see cref="ChartElement"/>.</summary>
public class ChartSeries
{
    /// <summary>Name shown in the legend.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>One value per category.</summary>
    public IReadOnlyList<double> Values { get; set; } = [];

    /// <summary>Series color; <c>null</c> picks a palette color.</summary>
    public ReportColor? Color { get; set; }
}

public enum ChartType { Bar, Line }

/// <summary>A bar or line chart. Drawn by the PDF renderer; HTML and Excel render <see cref="Fallback"/>.</summary>
public class ChartElement : IRendererSpecificElement
{
    public ChartType Type { get; set; } = ChartType.Bar;
    public string? Title { get; set; }
    public List<string> CategoryLabels { get; } = new();
    public List<ChartSeries> Series { get; } = new();

    /// <summary>Height of the chart area in points. Default 200.</summary>
    public float FixedHeight { get; set; } = 200f;

    /// <summary>Rendered instead of the chart by renderers that cannot draw it. Default: nothing.</summary>
    public IElement? Fallback { get; set; }
}
