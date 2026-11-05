using FluentReport.Rendering;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

/// <summary>The library's bundled DejaVu Sans, shared by all tests so text metrics do not depend on installed fonts.</summary>
internal static class TestFonts
{
    /// <summary>Returns shared instances: the renderer must not dispose resolver typefaces.</summary>
    public static readonly PdfOptions Options = new() { FontResolver = Resolve };

    public static SKTypeface Resolve(TextStyle style) => BundledFonts.For(style.EffectiveBold);
}
