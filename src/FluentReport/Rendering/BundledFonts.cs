using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// DejaVu Sans, embedded in the assembly. Used when the system has no usable font (for example a
/// Linux container without fonts), so text is never rendered as empty boxes. Shared for the whole
/// process and never disposed.
/// </summary>
internal static class BundledFonts
{
    private static readonly Lazy<SKTypeface> RegularFace = new(() => Load("FluentReport.Fonts.DejaVuSans.ttf"));
    private static readonly Lazy<SKTypeface> BoldFace = new(() => Load("FluentReport.Fonts.DejaVuSans-Bold.ttf"));

    public static SKTypeface Regular => RegularFace.Value;

    public static SKTypeface Bold => BoldFace.Value;

    public static SKTypeface For(bool bold) => bold ? Bold : Regular;

    /// <summary>
    /// False for the empty typeface SkiaSharp 4 returns when it finds no fonts: it has no glyphs, or
    /// maps basic Latin letters and digits to glyph 0.
    /// </summary>
    public static bool HasGlyphs(SKTypeface typeface) =>
        typeface.GlyphCount > 0 && typeface.GetGlyphs("Aa0").Any(glyph => glyph != 0);

    private static SKTypeface Load(string name)
    {
        using var stream = typeof(BundledFonts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded font '{name}' not found.");
        return SKTypeface.FromStream(stream) ?? throw new InvalidOperationException($"Embedded font '{name}' could not be loaded.");
    }
}
