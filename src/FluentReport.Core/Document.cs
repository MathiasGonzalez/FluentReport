using FluentReport.Builders;
using FluentReport.Core;

namespace FluentReport;

/// <summary>A report: one or more pages of content, built with <see cref="Create"/> and rendered by the PDF, HTML and Excel packages.</summary>
public class Document
{
    private Document(DocumentSettings settings) => Settings = settings;

    /// <summary>The document model the renderers read: its pages and their content.</summary>
    public DocumentSettings Settings { get; }

    /// <summary>Builds a document with the fluent API. At least one page is required.</summary>
    public static Document Create(Action<DocumentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new DocumentBuilder();
        configure(builder);
        return FromSettings(builder.Build());
    }

    /// <summary>
    /// Creates a <see cref="Document"/> directly from pre-built <see cref="DocumentSettings"/>.
    /// Intended for translation layers (e.g. RDLC) that construct settings programmatically.
    /// </summary>
    public static Document FromSettings(DocumentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Pages.Count == 0)
            throw new ArgumentException("DocumentSettings must contain at least one page.", nameof(settings));
        return new(settings);
    }
}
