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
    private readonly Func<TextStyle, SKTypeface>? _previousFactory;

    public SnapshotFontFixture()
    {
        // Capture the current factory so it can be restored in Dispose, preventing
        // global state leaks across test collections that may run concurrently.
        _previousFactory = SkiaFonts.TypefaceFactory;

        var asm = typeof(SnapshotFontFixture).Assembly;
        _regularPath = ExtractResource(asm, "DejaVuSans.ttf");
        _boldPath = ExtractResource(asm, "DejaVuSans-Bold.ttf");

        // Each factory call returns a NEW SKTypeface instance because callers wrap
        // the returned value in a 'using' block and dispose it after use.
        // Returning a shared instance would cause the fixture's typeface to be
        // disposed on the first call, causing subsequent renders to fall back to
        // a system font and produce non-deterministic output.
        SkiaFonts.TypefaceFactory = (TextStyle style) =>
            SKTypeface.FromFile(style.EffectiveBold ? _boldPath : _regularPath);
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
        // Restore the factory that was active before this fixture was created.
        SkiaFonts.TypefaceFactory = _previousFactory;
        // Best-effort cleanup of temp font files.
        try { File.Delete(_regularPath); } catch { }
        try { File.Delete(_boldPath); } catch { }
    }
}

