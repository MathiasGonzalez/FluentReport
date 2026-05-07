using FluentReport.Builders;
using FluentReport.Core;

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

    /// <summary>
    /// Creates a <see cref="Document"/> directly from pre-built <see cref="DocumentSettings"/>.
    /// Intended for use by format translation layers (e.g. RDLC, HTML) that construct settings
    /// programmatically rather than through the fluent builder API.
    /// </summary>
    public static Document FromSettings(DocumentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Pages.Count == 0)
            throw new ArgumentException("DocumentSettings must contain at least one page.", nameof(settings));
        return new(settings);
    }
}

