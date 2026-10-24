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
    private readonly Dictionary<(SKTypeface, float), SKFont> _fonts = new();
    private readonly List<SKTypeface> _owned = new();

    public SKFont Font(TextStyle style)
    {
        var typeface = Typeface(style);
        var key = (typeface, style.FontSize);
        if (!_fonts.TryGetValue(key, out var font))
        {
            // Grayscale anti-aliasing and whole-pixel glyph origins keep raster output identical across platforms.
            _fonts[key] = font = new SKFont(typeface, style.FontSize)
            {
                Edging = SKFontEdging.Antialias,
                Subpixel = false,
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
            typeface = SKTypeface.FromFamilyName(
                style.FontFamily,
                key.Item2 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                key.Item3 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright) ?? SKTypeface.Default;
            if (!ReferenceEquals(typeface, SKTypeface.Default))
                _owned.Add(typeface);
        }
        return _typefaces[key] = typeface;
    }

    public void Dispose()
    {
        foreach (var font in _fonts.Values) font.Dispose();
        foreach (var typeface in _owned) typeface.Dispose();
        _fonts.Clear();
        _typefaces.Clear();
        _owned.Clear();
    }
}
