using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// Resolves and caches typefaces and fonts for one render. Typefaces returned by a custom
/// resolver belong to the caller and are never disposed here; system typefaces are disposed
/// with the cache.
/// </summary>
internal sealed class FontCache(Func<TextStyle, SKTypeface?>? resolver) : IDisposable
{
    private readonly Dictionary<(string Family, bool Bold, bool Italic), SKTypeface> _typefaces = new();
    private readonly Dictionary<(SKTypeface, float, bool, bool), SKFont> _fonts = new();
    private readonly List<SKTypeface> _owned = new();

    public SKFont Font(TextStyle style)
    {
        var typeface = Typeface(style);
        var key = (typeface, style.FontSize, style.EffectiveBold, style.EffectiveItalic);
        if (!_fonts.TryGetValue(key, out var font))
        {
            // Grayscale anti-aliasing and whole-pixel glyph origins keep raster output identical across platforms.
            // When no bold or italic face exists, Skia synthesizes it instead of silently dropping the style.
            _fonts[key] = font = new SKFont(typeface, style.FontSize)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = false,
                Embolden = key.Item3 && !IsBold(typeface),
                SkewX = key.Item4 && !IsItalic(typeface) ? -0.2f : 0,
            };
        }
        return font;
    }

    public float Measure(string text, TextStyle style) => text.Length == 0 ? 0 : Font(style).MeasureText(text);

    private SKTypeface Typeface(TextStyle style)
    {
        var key = (style.FontFamily, style.EffectiveBold, style.EffectiveItalic);
        if (_typefaces.TryGetValue(key, out var cached))
            return cached;

        var typeface = (resolver ?? SkiaFonts.TypefaceFactory)?.Invoke(style);
        if (typeface == null)
        {
            var fontStyle = new SKFontStyle(
                key.Item2 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                key.Item3 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
            typeface = SKTypeface.FromFamilyName(style.FontFamily, fontStyle) ?? SKTypeface.Default;

            // SkiaSharp 4 ignores weight and slant for generic names such as "sans-serif" on macOS;
            // asking again with the concrete family it resolved to finds the styled face.
            if ((key.Item2 && !IsBold(typeface)) || (key.Item3 && !IsItalic(typeface)))
            {
                var styled = SKTypeface.FromFamilyName(typeface.FamilyName, fontStyle);
                if (styled != null && (!key.Item2 || IsBold(styled)) && (!key.Item3 || IsItalic(styled)))
                {
                    if (!ReferenceEquals(typeface, SKTypeface.Default)) typeface.Dispose();
                    typeface = styled;
                }
                else styled?.Dispose();
            }

            if (!ReferenceEquals(typeface, SKTypeface.Default))
                _owned.Add(typeface);
        }
        return _typefaces[key] = typeface;
    }

    private static bool IsBold(SKTypeface typeface) => typeface.FontWeight >= (int)SKFontStyleWeight.SemiBold;

    private static bool IsItalic(SKTypeface typeface) => typeface.FontSlant != SKFontStyleSlant.Upright;

    public void Dispose()
    {
        foreach (var font in _fonts.Values) font.Dispose();
        foreach (var typeface in _owned) typeface.Dispose();
        _fonts.Clear();
        _typefaces.Clear();
        _owned.Clear();
    }
}
