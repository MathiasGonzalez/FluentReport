using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Rendering;

namespace FluentReport;

public class Document
{
    private readonly DocumentSettings _settings;

    private Document(DocumentSettings settings) => _settings = settings;

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
}
