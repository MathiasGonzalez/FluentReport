using FluentReport.Core;

namespace FluentReport.Builders;

public class DocumentBuilder
{
    private readonly DocumentSettings _settings = new();

    public DocumentBuilder Page(Action<PageBuilder> configure)
    {
        var page = new PageSettings();
        configure(new PageBuilder(page));
        _settings.Pages.Add(page);
        return this;
    }

    internal DocumentSettings Build() => _settings;
}
