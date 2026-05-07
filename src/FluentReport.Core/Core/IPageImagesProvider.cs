namespace FluentReport.Core;

/// <summary>
/// Provides page images for a nested document. Implemented by the PDF renderer and
/// injected via <see cref="RenderContext.PageImagesProvider"/> so that
/// <see cref="FluentReport.Elements.SubreportElement"/> can render inline without a
/// direct dependency on SkiaSharp.
/// </summary>
public interface IPageImagesProvider
{
    /// <summary>Returns PNG byte arrays for every rendered page of the given document settings.</summary>
    IReadOnlyList<byte[]> GetPageImages(DocumentSettings settings, float scale = 1f);
}
