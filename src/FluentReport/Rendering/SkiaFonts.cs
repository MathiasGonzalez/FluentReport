using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>Process-wide font fallback. Prefer <see cref="PdfOptions.FontResolver"/>, which applies to one render.</summary>
public static class SkiaFonts
{
    /// <summary>
    /// Optional global typeface resolver, used when <see cref="PdfOptions.FontResolver"/> is not set.
    /// Return <c>null</c> to use the system font. Returned typefaces are cached per render and never
    /// disposed by FluentReport, so a resolver can safely return shared instances.
    /// </summary>
    public static Func<TextStyle, SKTypeface?>? TypefaceFactory { get; set; }
}
