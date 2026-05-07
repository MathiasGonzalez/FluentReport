using System.Reflection;
using FluentReport.Rendering;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

/// <summary>
/// xUnit class fixture that loads the embedded DejaVu Sans fonts and installs them as
/// the global <see cref="SkiaFonts.TypefaceFactory"/> so that all PDF snapshot tests
/// render text with the same font on every OS, making golden images reproducible.
/// </summary>
public sealed class SnapshotFontFixture : IDisposable
{
    private readonly string _regularPath;
    private readonly string _boldPath;
    private readonly SKTypeface _regular;
    private readonly SKTypeface _bold;

    public SnapshotFontFixture()
    {
        var asm = typeof(SnapshotFontFixture).Assembly;
        _regularPath = ExtractResource(asm, "DejaVuSans.ttf");
        _boldPath = ExtractResource(asm, "DejaVuSans-Bold.ttf");
        _regular = SKTypeface.FromFile(_regularPath);
        _bold = SKTypeface.FromFile(_boldPath);

        SkiaFonts.TypefaceFactory = (TextStyle style) => style.EffectiveBold ? _bold : _regular;
    }

    private static string ExtractResource(Assembly asm, string logicalName)
    {
        using var src = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException(
                $"Embedded font resource '{logicalName}' not found. " +
                $"Available: {string.Join(", ", asm.GetManifestResourceNames())}");

        var path = Path.Combine(
            Path.GetTempPath(),
            $"fr_font_{Path.GetFileNameWithoutExtension(logicalName)}_{Guid.NewGuid():N}.ttf");

        using var dst = File.Create(path);
        src.CopyTo(dst);
        return path;
    }

    public void Dispose()
    {
        SkiaFonts.TypefaceFactory = null;
        _regular.Dispose();
        _bold.Dispose();
        // SkiaSharp may keep a native handle open; best-effort cleanup only.
        try { File.Delete(_regularPath); } catch { }
        try { File.Delete(_boldPath); } catch { }
    }
}

