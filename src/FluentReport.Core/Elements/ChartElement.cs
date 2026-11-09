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

/// <summary>How a <see cref="ChartElement"/> draws its series.</summary>
public enum ChartType
{
    /// <summary>Vertical bars, grouped by category.</summary>
    Bar,
    /// <summary>One line per series.</summary>
    Line
}

/// <summary>A bar or line chart. Drawn by the PDF renderer; HTML and Excel render <see cref="Fallback"/>.</summary>
public class ChartElement : IRendererSpecificElement
{
    /// <summary>Bar or line. Default bar.</summary>
    public ChartType Type { get; set; } = ChartType.Bar;
    /// <summary>Title drawn above the chart.</summary>
    public string? Title { get; set; }
    /// <summary>Labels of the X axis, one per category.</summary>
    public List<string> CategoryLabels { get; } = new();
    /// <summary>The data series.</summary>
    public List<ChartSeries> Series { get; } = new();

    /// <summary>Height of the chart area in points. Default 200.</summary>
    public float FixedHeight { get; set; } = 200f;

    /// <summary>Rendered instead of the chart by renderers that cannot draw it. Default: nothing.</summary>
    public IElement? Fallback { get; set; }
}
