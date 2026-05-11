using FluentReport;
using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Html;

namespace FluentReport.Html.Tests;

public class HtmlRenderingTests
{
    private static readonly HtmlRendererOptions OutlookOptions = new() { OutlookCompatible = true };

    private static Document SimpleDocument(Action<PageBuilder>? configure = null) =>
        Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(20);
                if (configure != null)
                    configure(page);
                else
                    page.Content().Column(col => col.Item().Text("Hello, HTML!"));
            });
        });

    // ── Full document ─────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_ReturnsNonEmptyString()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.NotEmpty(html);
    }

    [Fact]
    public void GenerateHtml_FullDocument_HasDoctype()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.StartsWith("<!DOCTYPE html>", html.TrimStart());
    }

    [Fact]
    public void GenerateHtml_FullDocument_HasHtmlAndBodyTags()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.Contains("<html", html);
        Assert.Contains("<head>", html);
        Assert.Contains("<body", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public void GenerateHtml_FullDocument_HasMetaCharset()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.Contains("charset=\"UTF-8\"", html);
    }

    // ── Fragment ──────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtmlFragment_DoesNotContainHtmlTag()
    {
        var fragment = SimpleDocument().GenerateHtmlFragment();
        Assert.DoesNotContain("<html", fragment);
        Assert.DoesNotContain("<head>", fragment);
        Assert.DoesNotContain("<body", fragment);
    }

    [Fact]
    public void GenerateHtmlFragment_ContainsTableWrapper()
    {
        var fragment = SimpleDocument().GenerateHtmlFragment();
        Assert.Contains("<table", fragment);
        Assert.Contains("</table>", fragment);
    }

    // ── Content ───────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_TextElement_ContentAppearsInOutput()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.Contains("Hello, HTML!", html);
    }

    [Fact]
    public void GenerateHtml_WithTable_RendersTheadAndTbody()
    {
        var html = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(20);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Name");
                        h.Cell().Text("Value");
                    });
                    table.Cell().Text("Row 1");
                    table.Cell().Text("100");
                });
            });
        }).GenerateHtml();

        Assert.Contains("<thead>", html);
        Assert.Contains("<tbody>", html);
        Assert.Contains("Name", html);
        Assert.Contains("Row 1", html);
    }

    // ── Options ───────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_WithMaxWidth_TableHasWidthAttribute()
    {
        var html = SimpleDocument().GenerateHtml(new HtmlRendererOptions { MaxWidth = 700 });
        Assert.Contains("width=\"700\"", html);
        Assert.Contains("max-width: 700px", html);
    }

    [Fact]
    public void GenerateHtml_WithNullMaxWidth_TableHasFullWidth()
    {
        var html = SimpleDocument().GenerateHtml(new HtmlRendererOptions { MaxWidth = null });
        Assert.Contains("width=\"100%\"", html);
    }

    // ── Images ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_WithImageBytes_RendersBase64DataUri()
    {
        // 1×1 transparent PNG
        byte[] pngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0B, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02,
            0x00, 0x01, 0xE5, 0x27, 0xDE, 0xFC, 0x00, 0x00,
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
            0x60, 0x82
        ];

        var html = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(20);
                page.Content().Image(pngBytes);
            });
        }).GenerateHtml();

        Assert.Contains("data:image/png;base64,", html);
    }

    // ── Multi-page ────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_MultiplePages_BothPagesRendered()
    {
        var html = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(20);
                page.Content().Column(col => col.Item().Text("Page One Content"));
            });
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(20);
                page.Content().Column(col => col.Item().Text("Page Two Content"));
            });
        }).GenerateHtml();

        Assert.Contains("Page One Content", html);
        Assert.Contains("Page Two Content", html);
    }

    // ── OutlookCompatible ─────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_OutlookCompatible_AddsRolePresentationToTables()
    {
        var html = SimpleDocument().GenerateHtml(OutlookOptions);
        Assert.Contains("role=\"presentation\"", html);
    }

    [Fact]
    public void GenerateHtml_Default_DoesNotAddRolePresentationToTables()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.DoesNotContain("role=\"presentation\"", html);
    }

    [Fact]
    public void GenerateHtml_OutlookCompatible_AddsBgcolorAttribute()
    {
        var html = SimpleDocument().GenerateHtml(OutlookOptions);
        Assert.Contains("bgcolor=", html);
    }

    [Fact]
    public void GenerateHtml_Default_DoesNotAddBgcolorAttribute()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.DoesNotContain("bgcolor=", html);
    }

    [Fact]
    public void GenerateHtml_OutlookCompatible_InjectsOfficeDocumentSettingsBlock()
    {
        var html = SimpleDocument().GenerateHtml(OutlookOptions);
        Assert.Contains("o:OfficeDocumentSettings", html);
        Assert.Contains("o:PixelsPerInch", html);
    }

    [Fact]
    public void GenerateHtml_Default_DoesNotInjectOfficeDocumentSettingsBlock()
    {
        var html = SimpleDocument().GenerateHtml();
        Assert.DoesNotContain("o:OfficeDocumentSettings", html);
    }

    [Fact]
    public void GenerateHtml_OutlookCompatible_OfficeDocumentSettingsHasXmlnsDeclaration()
    {
        var html = SimpleDocument().GenerateHtml(OutlookOptions);
        Assert.Contains("xmlns:o=\"urn:schemas-microsoft-com:office:office\"", html);
    }

    [Fact]
    public void GenerateHtmlFragment_OutlookCompatible_DoesNotContainOfficeDocumentSettings()
    {
        // The OfficeDocumentSettings block belongs in <head> — it must NOT appear in fragments.
        var fragment = SimpleDocument().GenerateHtmlFragment(OutlookOptions);
        Assert.DoesNotContain("o:OfficeDocumentSettings", fragment);
    }

    // ── File / stream overloads ───────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_ToFile_CreatesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fluentreport_test_{Guid.NewGuid()}.html");
        try
        {
            SimpleDocument().GenerateHtml(path);
            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("<!DOCTYPE html>", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void GenerateHtml_ToStream_WritesContent()
    {
        using var ms = new MemoryStream();
        SimpleDocument().GenerateHtml(ms);
        ms.Position = 0;
        var content = new StreamReader(ms).ReadToEnd();
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("Hello, HTML!", content);
    }
}
