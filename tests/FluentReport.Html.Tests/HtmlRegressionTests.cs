using System.Globalization;
using FluentReport.Html;

namespace FluentReport.Html.Tests;

public class HtmlRegressionTests
{
    private static string Html(Action<Builders.PageBuilder> page) =>
        Document.Create(c => c.Page(page)).GenerateHtml();

    [Fact]
    public void CssNumbers_UseDotDecimalSeparator_UnderSpanishCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-UY");
        try
        {
            var html = Html(p => p.Content().Column(col =>
            {
                col.Item().Text("x").FontSize(10.5f);
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); d.RelativeColumn(); });
                    t.Cell().Text("a"); t.Cell().Text("b"); t.Cell().Text("c");
                });
            }));

            Assert.Contains("font-size: 10.5px", html);
            Assert.Contains("line-height: 1.2", html);
            Assert.Contains("33.3%", html);
            Assert.DoesNotContain("10,5px", html);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void RowSpacing_IsNotOverriddenByPaddingShorthand()
    {
        var html = Html(p => p.Content().Row(r => { r.Spacing(20); r.RelativeItem().Text("L"); r.RelativeItem().Text("R"); }));
        Assert.Contains("padding: 0; width: 50%; padding-right: 20px;", html);
    }

    [Fact]
    public void TableColumnWidths_AreEmitted()
    {
        var html = Html(p => p.Content().Table(t =>
        {
            t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(1); });
            t.Cell().Text("wide"); t.Cell().Text("narrow");
        }));
        Assert.Contains("width: 75.0%", html);
        Assert.Contains("width: 25.0%", html);
    }

    [Fact]
    public void TextAlignment_InsideTableCell_IsAppliedToTheCell()
    {
        var html = Html(p => p.Content().Table(t =>
        {
            t.ColumnsDefinition(d => d.RelativeColumn());
            t.Cell().Text("9.99").AlignRight();
        }));
        Assert.Contains("<td style=\"vertical-align: top; padding: 4px; text-align: right;\">", html);
    }

    [Fact]
    public void FooterRows_AreRenderedAfterTheData()
    {
        var html = Html(p => p.Content().Table(t =>
        {
            t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
            t.Cell().Text("ROW"); t.Cell().Text("1");
            t.Footer(f => { f.Cell().Text("TOTAL"); f.Cell().Text("1"); });
        }));
        Assert.True(html.IndexOf("ROW", StringComparison.Ordinal) < html.IndexOf("TOTAL", StringComparison.Ordinal));
        Assert.Contains("<tfoot>", html);
    }

    [Fact]
    public void PageNumbers_ShowPageIndexAndPageCount()
    {
        var html = Document.Create(c =>
        {
            for (int i = 0; i < 2; i++)
                c.Page(p => p.Footer().Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); }));
        }).GenerateHtml();

        Assert.Contains(">1</span><span", html);
        Assert.Contains(">2</span><span", html);
        Assert.DoesNotContain(">?<", html);
    }

    [Fact]
    public void FontFamily_CannotBreakOutOfTheStyleAttribute()
    {
        var html = Html(p => p.Content().Text("hi").FontFamily("x\" onmouseover=\"alert(1)"));
        Assert.DoesNotContain("onmouseover=\"", html);
    }

    [Fact]
    public void GenerateHtml_ToStream_WritesNoByteOrderMark()
    {
        using var ms = new MemoryStream();
        Document.Create(c => c.Page(p => p.Content().Text("x"))).GenerateHtml(ms);
        Assert.Equal((byte)'<', ms.ToArray()[0]);
    }
}
