using FluentReport.Styling;

namespace FluentReport.Core;

/// <summary>
/// Abstracts text measurement operations. Used during the measure phase where no canvas is available.
/// </summary>
public interface ITextMeasurer
{
    float MeasureText(string text, TextStyle style);
    float MeasureText(string text, float fontSize, string? fontFamily = null);

    /// <summary>
    /// Returns the ascent (distance from baseline to top of tallest glyph) for the given style.
    /// Default implementation returns a reasonable approximation (<c>style.FontSize * 0.8</c>).
    /// Override in concrete implementations for accurate measurements.
    /// </summary>
    float GetTextAscent(TextStyle style) => style.FontSize * 0.8f;

    List<string> WrapText(string text, TextStyle style, float maxWidth);
}
