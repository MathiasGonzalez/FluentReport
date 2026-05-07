using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// Single point of configuration for SkiaSharp font creation.
/// Set <see cref="TypefaceFactory"/> to override font resolution (e.g. for testing with embedded fonts).
/// </summary>
public static class SkiaFonts
{
    /// <summary>
    /// Optional override for typeface creation. When non-null, called instead of the default
    /// system font lookup. A null return falls back to <see cref="SKTypeface.Default"/>.
    /// </summary>
    public static Func<TextStyle, SKTypeface>? TypefaceFactory { get; set; }

    internal static SKTypeface CreateTypeface(TextStyle style)
    {
        if (TypefaceFactory != null)
            return TypefaceFactory(style) ?? SKTypeface.Default;

        return SKTypeface.FromFamilyName(
            style.FontFamily,
            style.EffectiveBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            style.EffectiveItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
        ) ?? SKTypeface.Default;
    }

    internal static SKTypeface CreateTypeface(string? fontFamily, bool bold = false)
    {
        // Honor TypefaceFactory for consistent behavior when a custom font provider is active.
        if (TypefaceFactory != null)
        {
            var style = new TextStyle { FontFamily = fontFamily ?? "sans-serif", Bold = bold };
            return TypefaceFactory(style) ?? SKTypeface.Default;
        }

        return SKTypeface.FromFamilyName(
               fontFamily ?? "sans-serif",
               bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
               SKFontStyleWidth.Normal,
               SKFontStyleSlant.Upright)
           ?? SKTypeface.Default;
    }
}
