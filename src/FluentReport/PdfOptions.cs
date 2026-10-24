using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport;

/// <summary>Options for <c>GeneratePdf</c> and <c>GenerateImages</c>.</summary>
public sealed class PdfOptions
{
    /// <summary>
    /// Resolves the typeface for a text style (e.g. to use embedded fonts). Return <c>null</c> to use
    /// the system font. Typefaces are cached per render and never disposed by FluentReport.
    /// </summary>
    public Func<TextStyle, SKTypeface?>? FontResolver { get; init; }

    /// <summary>PDF document title metadata.</summary>
    public string? Title { get; init; }

    /// <summary>PDF document author metadata.</summary>
    public string? Author { get; init; }
}
