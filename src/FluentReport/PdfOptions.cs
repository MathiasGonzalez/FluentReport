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

    /// <summary>
    /// Embeds in the PDF only the glyphs the document uses instead of whole font files, which makes a
    /// short document several hundred KB smaller. Applies to TrueType fonts; others are embedded whole.
    /// Text looks, measures and extracts the same. Default <c>true</c>.
    /// </summary>
    public bool SubsetFonts { get; init; } = true;

    /// <summary>PDF document title metadata.</summary>
    public string? Title { get; init; }

    /// <summary>PDF document author metadata.</summary>
    public string? Author { get; init; }

    /// <summary>Default for <see cref="MaxImagePixels"/>: 100 megapixels (about 400 MB once decoded).</summary>
    public const long DefaultMaxImagePixels = 100_000_000;

    /// <summary>
    /// Largest image, in pixels (width × height), that is decoded. A small compressed file can expand to
    /// gigabytes, so larger images throw <see cref="InvalidOperationException"/> instead of exhausting
    /// memory. <c>null</c> removes the limit. Default <see cref="DefaultMaxImagePixels"/>.
    /// </summary>
    public long? MaxImagePixels { get; init; } = DefaultMaxImagePixels;
}
