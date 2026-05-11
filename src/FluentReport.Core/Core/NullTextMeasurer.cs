using FluentReport.Styling;

namespace FluentReport.Core;

/// <summary>
/// Default <see cref="ITextMeasurer"/> that throws <see cref="InvalidOperationException"/>.
/// Used as the default value in <see cref="MeasureContext"/> so that elements that
/// require text measurement fail fast with a clear message when no measurer is configured.
/// </summary>
internal sealed class NullTextMeasurer : ITextMeasurer
{
    public static readonly NullTextMeasurer Instance = new();

    private NullTextMeasurer() { }

    public float MeasureText(string text, TextStyle style)
        => throw new InvalidOperationException(
            "No ITextMeasurer is configured. Set MeasureContext.Measurer before calling Measure() on text-bearing elements.");

    public float MeasureText(string text, float fontSize, string? fontFamily = null)
        => throw new InvalidOperationException(
            "No ITextMeasurer is configured. Set MeasureContext.Measurer before calling Measure() on text-bearing elements.");

    public float GetTextAscent(TextStyle style)
        => throw new InvalidOperationException(
            "No ITextMeasurer is configured. Set MeasureContext.Measurer before calling Measure() on text-bearing elements.");

    public List<string> WrapText(string text, TextStyle style, float maxWidth)
        => throw new InvalidOperationException(
            "No ITextMeasurer is configured. Set MeasureContext.Measurer before calling Measure() on text-bearing elements.");
}
