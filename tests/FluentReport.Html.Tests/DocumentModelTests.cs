using FluentReport.Elements;
using FluentReport.Html;

namespace FluentReport.Html.Tests;

/// <summary>Behaviour of the fluent builders and the element model shared by every renderer.</summary>
public class DocumentModelTests
{
    private static TextElement ContentText(Document doc) => (TextElement)doc.Settings.Pages[0].ContentElement!;

    [Fact]
    public void StyleChainedAfterDynamicText_AppliesToEverySpan()
    {
        var doc = Document.Create(c => c.Page(p =>
            p.Content().Text(x => { x.Span("Page "); x.CurrentPageNumber(); }).FontSize(30).Bold().Color("#FF0000")));

        foreach (var span in ContentText(doc).Spans)
        {
            Assert.Equal(30, span.Style.FontSize);
            Assert.True(span.Style.Bold);
            Assert.Equal(255, span.Style.Color.R);
        }
    }

    [Fact]
    public void SpanOverrides_WinOverStyleChainedOnTheElement()
    {
        var doc = Document.Create(c => c.Page(p =>
            p.Content().Text(x => { x.Span("small", s => s.FontSize = 8); x.Span("default"); }).FontSize(20)));

        var spans = ContentText(doc).Spans;
        Assert.Equal(8, spans[0].Style.FontSize);
        Assert.Equal(20, spans[1].Style.FontSize);
    }

    [Fact]
    public void Create_ResolvesBuilderPlaceholders_SoRenderersOnlySeeElements()
    {
        var doc = Document.Create(c => c.Page(p => p.Content().Column(col =>
        {
            col.Item().Padding(4).Text("a");
            col.Item().Row(r => r.RelativeItem().Text("b"));
            col.Item().Table(t => { t.ColumnsDefinition(d => d.RelativeColumn()); t.Cell().Text("c"); });
        })));

        var column = Assert.IsType<ColumnElement>(doc.Settings.Pages[0].ContentElement);
        Assert.IsType<PaddingElement>(column.Items[0]);
        Assert.IsType<TextElement>(Assert.IsType<RowElement>(column.Items[1]).Items[0].Element);
        Assert.IsType<TextElement>(Assert.IsType<TableElement>(column.Items[2]).DataCells[0].Content);
    }

    [Fact]
    public void Create_WithoutPages_Throws()
        => Assert.Throws<ArgumentException>(() => Document.Create(_ => { }));

    [Fact]
    public void Container_WithTwoContents_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Document.Create(c => c.Page(p =>
        {
            var content = p.Content();
            content.Text("first");
            content.Text("second");
        })));
        Assert.Contains("already has content", ex.Message);
    }

    [Fact]
    public void Image_FromMissingPath_Throws()
        => Assert.Throws<FileNotFoundException>(() => new ImageElement(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png")));

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, "image/gif")]
    [InlineData(new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' }, null)]
    public void Image_DetectsMimeTypeFromSignature(byte[] bytes, string? expected)
        => Assert.Equal(expected, new ImageElement(bytes).MimeType);

    [Fact]
    public void Html_DoesNotEmbedBytesThatAreNotAnImage()
    {
        var secret = "TOP-SECRET"u8.ToArray();
        var html = Document.Create(c => c.Page(p => p.Content().Image(secret))).GenerateHtml();

        Assert.DoesNotContain(Convert.ToBase64String(secret), html);
        Assert.Contains("image not available", html);
    }

    [Fact]
    public void Html_RendersListItems()
    {
        var html = Document.Create(c => c.Page(p =>
            p.Content().List(new[] { "ALPHA", "BETA" }, (cb, s) => cb.Text(s), spacing: 4))).GenerateHtml();

        Assert.Contains("ALPHA", html);
        Assert.Contains("BETA", html);
    }

    private sealed class CustomElement : IElement;

    [Fact]
    public void Html_UnsupportedElement_FailsLoudly()
    {
        var settings = new Core.DocumentSettings();
        settings.Pages.Add(new Core.PageSettings { ContentElement = new CustomElement() });

        Assert.Throws<NotSupportedException>(() => Document.FromSettings(settings).GenerateHtml());
    }
}
