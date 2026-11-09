using FluentReport.Core;

namespace FluentReport.Builders;

/// <summary>Adds pages to a document. Received by <see cref="Document.Create"/>.</summary>
public class DocumentBuilder
{
    private readonly DocumentSettings _settings = new();

    /// <summary>Adds a page definition: size, margins, header, content and footer.</summary>
    public DocumentBuilder Page(Action<PageBuilder> configure)
    {
        var page = new PageSettings();
        configure(new PageBuilder(page));
        _settings.Pages.Add(page);
        return this;
    }

    internal DocumentSettings Build()
    {
        foreach (var page in _settings.Pages)
        {
            if (page.HeaderElement != null) page.HeaderElement = ElementTree.Resolve(page.HeaderElement);
            if (page.ContentElement != null) page.ContentElement = ElementTree.Resolve(page.ContentElement);
            if (page.FooterElement != null) page.FooterElement = ElementTree.Resolve(page.FooterElement);
        }
        return _settings;
    }
}
