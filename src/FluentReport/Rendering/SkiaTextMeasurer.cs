using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// SkiaSharp implementation of <see cref="ITextMeasurer"/>.
/// Used during the measure phase (before a canvas is available).
/// </summary>
public sealed class SkiaTextMeasurer : ITextMeasurer
{
    public float MeasureText(string text, TextStyle style)
    {
        using var typeface = SkiaFonts.CreateTypeface(style);
        using var font = new SKFont(typeface, style.FontSize);
        using var paint = new SKPaint { IsAntialias = true };
        return font.MeasureText(text, paint);
    }

    public float MeasureText(string text, float fontSize, string? fontFamily = null)
    {
        using var typeface = SkiaFonts.CreateTypeface(fontFamily);
        using var font = new SKFont(typeface, fontSize);
        return font.MeasureText(text);
    }

    public List<string> WrapText(string text, TextStyle style, float maxWidth)
    {
        using var typeface = SkiaFonts.CreateTypeface(style);
        using var font = new SKFont(typeface, style.FontSize);
        using var paint = new SKPaint { IsAntialias = true };
        return WrapTextCore(text, font, paint, maxWidth);
    }

    internal static List<string> WrapTextCore(string text, SKFont font, SKPaint paint, float maxWidth)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }

        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (maxWidth <= 0 || font.MeasureText(line) <= maxWidth)
            {
                result.Add(line);
                continue;
            }

            var words = line.Split(' ');
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var test = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(test) <= maxWidth)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0) result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }
            if (current.Length > 0) result.Add(current.ToString());
        }

        return result.Count > 0 ? result : [""];
    }
}
