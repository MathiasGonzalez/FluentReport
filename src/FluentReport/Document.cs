using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Rendering;

namespace FluentReport;

public class Document
{
    private readonly DocumentSettings _settings;

    private Document(DocumentSettings settings) => _settings = settings;

    public DocumentSettings Settings => _settings;

    public static Document Create(Action<DocumentBuilder> configure)
    {
        var builder = new DocumentBuilder();
        configure(builder);
        return new Document(builder.Build());
    }

    public void GeneratePdf(string filePath)
    {
        using var stream = File.Create(filePath);
        GeneratePdf(stream);
    }

    public void GeneratePdf(Stream stream)
    {
        var renderer = new DocumentRenderer(_settings);
        renderer.RenderToStream(stream);
    }

    public byte[] GeneratePdf()
    {
        using var ms = new MemoryStream();
        GeneratePdf(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a <see cref="Document"/> directly from pre-built <see cref="DocumentSettings"/>.
    /// Intended for use by format translation layers (e.g. RDLC, HTML) that construct settings
    /// programmatically rather than through the fluent builder API.
    /// </summary>
    /// <param name="settings">Pre-built document settings. Must not be <c>null</c> and must contain at least one page.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="settings"/> contains no pages.</exception>
    public static Document FromSettings(DocumentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Pages.Count == 0)
            throw new ArgumentException("DocumentSettings must contain at least one page.", nameof(settings));
        return new(settings);
    }

    /// <summary>
    /// Renders every logical page to a PNG byte array.
    /// Useful for visual / snapshot testing.
    /// </summary>
    /// <param name="scale">
    /// Pixel-per-point scale factor (default 1.0 = 1 px per pt, 2.0 = 2× hi-dpi).
    /// Must be greater than zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="scale"/> is ≤ 0.</exception>
    public IReadOnlyList<byte[]> GenerateImages(float scale = 1f)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be greater than zero.");

        var renderer = new DocumentRenderer(_settings);
        return renderer.RenderAllPages(scale);
    }
}
