using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

/// <summary>Embedded DejaVu Sans, shared by all tests so text metrics do not depend on installed fonts.</summary>
internal static class TestFonts
{
    private static readonly SKTypeface Regular = Load("DejaVuSans.ttf");
    private static readonly SKTypeface Bold = Load("DejaVuSans-Bold.ttf");

    /// <summary>Returns shared instances: the renderer must not dispose resolver typefaces.</summary>
    public static readonly PdfOptions Options = new() { FontResolver = Resolve };

    public static SKTypeface Resolve(TextStyle style) => style.EffectiveBold ? Bold : Regular;

    private static SKTypeface Load(string name)
    {
        using var stream = typeof(TestFonts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded font '{name}' not found.");
        return SKTypeface.FromStream(stream) ?? throw new InvalidOperationException($"Font '{name}' could not be loaded.");
    }
}
