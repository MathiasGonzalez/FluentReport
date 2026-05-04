using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Rendering;
using SkiaSharp;

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
    /// Renders every logical page to a PNG byte array.
    /// Useful for visual / snapshot testing.
    /// </summary>
    /// <param name="scale">
    /// Pixel-per-point scale factor (default 1.0 = 1 px per pt, 2.0 = 2× hi-dpi).
    /// </param>
    public IReadOnlyList<byte[]> GenerateImages(float scale = 1f)
    {
        var renderer = new DocumentRenderer(_settings);
        int count = renderer.GetPageCount();
        var result = new List<byte[]>(count);
        for (int i = 0; i < count; i++)
        {
            using var image = renderer.RenderPageToImage(i, scale);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            result.Add(data.ToArray());
        }
        return result;
    }
}
