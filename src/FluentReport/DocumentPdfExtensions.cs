using FluentReport.Rendering;

namespace FluentReport;

/// <summary>
/// PDF rendering extension methods for <see cref="Document"/>.
/// Requires the <c>FluentReport</c> package (SkiaSharp-based PDF renderer).
/// </summary>
public static class DocumentPdfExtensions
{
    public static void GeneratePdf(this Document document, string filePath)
    {
        using var stream = File.Create(filePath);
        document.GeneratePdf(stream);
    }

    public static void GeneratePdf(this Document document, Stream stream)
    {
        var renderer = new DocumentRenderer(document.Settings);
        renderer.RenderToStream(stream);
    }

    public static byte[] GeneratePdf(this Document document)
    {
        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders every logical page to a PNG byte array.
    /// Useful for visual / snapshot testing.
    /// </summary>
    /// <param name="scale">Pixel-per-point scale factor. Must be greater than zero.</param>
    public static IReadOnlyList<byte[]> GenerateImages(this Document document, float scale = 1f)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be greater than zero.");

        var renderer = new DocumentRenderer(document.Settings);
        return renderer.RenderAllPages(scale);
    }
}
