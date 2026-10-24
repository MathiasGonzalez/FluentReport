using FluentReport.Rendering;

namespace FluentReport;

/// <summary>PDF and PNG output for <see cref="Document"/>.</summary>
public static class DocumentPdfExtensions
{
    /// <summary>Writes the document as PDF to <paramref name="filePath"/>. Nothing is written if rendering fails.</summary>
    public static void GeneratePdf(this Document document, string filePath, PdfOptions? options = null)
        => File.WriteAllBytes(filePath, document.GeneratePdf(options));

    /// <summary>Writes the document as PDF to <paramref name="stream"/>.</summary>
    public static void GeneratePdf(this Document document, Stream stream, PdfOptions? options = null)
    {
        using var renderer = new DocumentRenderer(document.Settings, options);
        renderer.RenderPdf(stream);
    }

    /// <summary>Returns the document as PDF bytes.</summary>
    public static byte[] GeneratePdf(this Document document, PdfOptions? options = null)
    {
        using var ms = new MemoryStream();
        document.GeneratePdf(ms, options);
        return ms.ToArray();
    }

    /// <summary>Renders every page to PNG (e.g. for previews or visual tests).</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="scale">Pixels per point; 1 = 72 dpi, 2 = 144 dpi. Must be greater than zero.</param>
    /// <param name="options">Fonts and metadata options.</param>
    public static IReadOnlyList<byte[]> GenerateImages(this Document document, float scale = 1f, PdfOptions? options = null)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be greater than zero.");
        using var renderer = new DocumentRenderer(document.Settings, options);
        return renderer.RenderPng(scale);
    }

    /// <summary>Number of pages the document produces in PDF.</summary>
    public static int GetPdfPageCount(this Document document, PdfOptions? options = null)
    {
        using var renderer = new DocumentRenderer(document.Settings, options);
        return renderer.Pages.Count;
    }
}
