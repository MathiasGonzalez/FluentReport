using FluentReport.Styling;

namespace FluentReport.Core;

/// <summary>
/// Abstracts text measurement operations. Used during the measure phase where no canvas is available.
/// </summary>
public interface ITextMeasurer
{
    float MeasureText(string text, TextStyle style);
    float MeasureText(string text, float fontSize, string? fontFamily = null);
    List<string> WrapText(string text, TextStyle style, float maxWidth);
}
