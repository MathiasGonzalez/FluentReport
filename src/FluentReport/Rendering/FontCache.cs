using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// Resolves and caches typefaces and fonts for one render. Typefaces returned by a custom
/// resolver belong to the caller and are never disposed here; system typefaces are disposed
/// with the cache. When the system has no usable font, the bundled DejaVu Sans is used.
/// </summary>
/// <param name="resolver">Custom typeface resolver; <c>null</c> results fall back to the system font.</param>
/// <param name="systemFonts">System font lookup; tests replace it to simulate a machine without fonts.</param>
internal sealed class FontCache(
    Func<TextStyle, SKTypeface?>? resolver,
    Func<string, SKFontStyle, SKTypeface?>? systemFonts = null) : IDisposable
{
    private readonly Func<string, SKFontStyle, SKTypeface?> _systemFonts = systemFonts ?? SKTypeface.FromFamilyName;

    private readonly Dictionary<(string Family, bool Bold, bool Italic), SKTypeface> _typefaces = new();
    private readonly Dictionary<(SKTypeface, float, bool, bool), SKFont> _fonts = new();
    private readonly List<SKTypeface> _owned = new();

    // Font subsetting: glyphs drawn while collecting, then one reduced typeface per original.
    private Dictionary<SKTypeface, HashSet<ushort>>? _usedGlyphs;
    private readonly Dictionary<SKTypeface, SKTypeface> _subsets = new();

    /// <summary>The font to measure with: always the full typeface.</summary>
    public SKFont Font(TextStyle style) => Font(style, Typeface(style), null);

    /// <summary>
    /// The font to draw <paramref name="text"/> with. While glyphs are being collected it records the
    /// ones <paramref name="text"/> needs; afterwards it returns the subset typeface when there is one.
    /// </summary>
    public SKFont DrawFont(TextStyle style, string text)
    {
        var typeface = Typeface(style);
        if (_usedGlyphs != null)
        {
            if (!_usedGlyphs.TryGetValue(typeface, out var used))
                _usedGlyphs[typeface] = used = new();
            used.UnionWith(typeface.GetGlyphs(text));
        }
        return Font(style, typeface, _subsets.GetValueOrDefault(typeface));
    }

    /// <summary>Starts recording the glyphs passed to <see cref="DrawFont"/>.</summary>
    public void BeginGlyphCollection() => _usedGlyphs = new();

    /// <summary>
    /// Stops recording and builds a subset of every typeface drawn with, so a PDF embeds only the
    /// glyphs it uses. Typefaces that cannot be subset keep being drawn whole.
    /// </summary>
    public void EndGlyphCollection()
    {
        if (_usedGlyphs == null) return;
        foreach (var (typeface, glyphs) in _usedGlyphs)
        {
            if (_subsets.ContainsKey(typeface) || FontSubsetter.Subset(typeface, glyphs) is not { } file) continue;
            using var data = SKData.CreateCopy(file);
            var subset = SKTypeface.FromData(data);
            if (subset == null) continue;
            // A subset that does not load as the same font would draw other glyphs: keep the full one.
            if (subset.GlyphCount != typeface.GlyphCount || subset.UnitsPerEm != typeface.UnitsPerEm)
            {
                subset.Dispose();
                continue;
            }
            _subsets[typeface] = subset;
            _owned.Add(subset);
        }
        _usedGlyphs = null;
    }

    private SKFont Font(TextStyle style, SKTypeface typeface, SKTypeface? subset)
    {
        var key = (subset ?? typeface, style.FontSize, style.EffectiveBold, style.EffectiveItalic);
        if (!_fonts.TryGetValue(key, out var font))
        {
            // Grayscale anti-aliasing and whole-pixel glyph origins keep raster output identical across platforms.
            // When no bold or italic face exists, Skia synthesizes it instead of silently dropping the style.
            _fonts[key] = font = new SKFont(subset ?? typeface, style.FontSize)
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
            typeface = _systemFonts(style.FontFamily, fontStyle) ?? SKTypeface.Default;

            // SkiaSharp 4 ignores weight and slant for generic names such as "sans-serif" on macOS;
            // asking again with the concrete family it resolved to finds the styled face.
            if ((key.Item2 && !IsBold(typeface)) || (key.Item3 && !IsItalic(typeface)))
            {
                var styled = _systemFonts(typeface.FamilyName, fontStyle);
                if (styled != null && (!key.Item2 || IsBold(styled)) && (!key.Item3 || IsItalic(styled)))
                {
                    if (!IsShared(typeface)) typeface.Dispose();
                    typeface = styled;
                }
                else if (styled != null && !IsShared(styled)) styled.Dispose();
            }

            // SkiaSharp 4 finds no fonts on Linux without fontconfig (and in font-less containers) and
            // returns an empty typeface, which would render every character as glyph 0.
            if (!BundledFonts.HasGlyphs(typeface))
            {
                if (!IsShared(typeface)) typeface.Dispose();
                typeface = BundledFonts.For(key.Item2);
            }
            else if (!IsShared(typeface))
                _owned.Add(typeface);
        }
        return _typefaces[key] = typeface;
    }

    private static bool IsShared(SKTypeface typeface) =>
        ReferenceEquals(typeface, SKTypeface.Default) || ReferenceEquals(typeface, SKTypeface.Empty);

    private static bool IsBold(SKTypeface typeface) => typeface.FontWeight >= (int)SKFontStyleWeight.SemiBold;

    private static bool IsItalic(SKTypeface typeface) => typeface.FontSlant != SKFontStyleSlant.Upright;

    public void Dispose()
    {
        foreach (var font in _fonts.Values) font.Dispose();
        foreach (var typeface in _owned) typeface.Dispose();
        _fonts.Clear();
        _typefaces.Clear();
        _subsets.Clear();
        _owned.Clear();
    }
}
