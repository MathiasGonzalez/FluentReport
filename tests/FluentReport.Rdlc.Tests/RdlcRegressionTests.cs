using System.Globalization;
using FluentReport.Elements;
using FluentReport.Html;
using FluentReport.Styling;

namespace FluentReport.Rdlc.Tests;

/// <summary>Regressions found in the October 2026 audit (culture, expressions, layout, images).</summary>
public class RdlcRegressionTests
{
    private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

    private static string Report(string items, string extra = "") => $"""
        <Report xmlns="{Ns}">{extra}<ReportSections><ReportSection>
          <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
          <Body><ReportItems>{items}</ReportItems></Body>
        </ReportSection></ReportSections></Report>
        """;

    private static string Textbox(string value, string style = "", string name = "T") =>
        $"<Textbox Name=\"{name}\"><Value>{value}</Value><Style>{style}</Style></Textbox>";

    private static readonly Dictionary<string, IEnumerable<object>> Amounts = new()
    {
        ["DS"] = new object[]
        {
            new Dictionary<string, object> { ["Name"] = "A", ["Amount"] = 1234.5m, ["Active"] = false },
            new Dictionary<string, object> { ["Name"] = "B", ["Amount"] = 10.25m, ["Active"] = true },
        }
    };

    private static IElement Content(Document doc) => doc.Settings.Pages[0].ContentElement!;

    private static TextElement FirstText(IElement element) => element switch
    {
        TextElement t => t,
        ColumnElement c => FirstText(c.Items.First(i => i is not SpacerElement)),
        PaddingElement p => FirstText(p.Child!),
        BorderElement b => FirstText(b.Child!),
        AlignElement a => FirstText(a.Child!),
        _ => throw new InvalidOperationException(element.GetType().Name)
    };

    private static string Text(Document doc) => string.Concat(FirstText(Content(doc)).Spans.Select(s => s.StaticText));

    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try { return action(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    // ── Culture ──────────────────────────────────────────────────────────────

    [Fact]
    public void Sum_IsCorrect_WhatEverTheMachineCulture()
    {
        var doc = WithCulture("es-UY", () => DocumentRdlcExtensions.FromRdlcXml(
            Report(Textbox("=Sum(Fields!Amount.Value, \"DS\")")), Amounts));

        Assert.Equal("1244.75", Text(doc));
    }

    [Fact]
    public void Culture_FromOptions_FormatsNumbersAndCurrency()
    {
        var options = new RdlcImportOptions { Culture = new CultureInfo("es-UY") };
        var sum = DocumentRdlcExtensions.FromRdlcXml(Report(Textbox("=Sum(Fields!Amount.Value, \"DS\")")), Amounts, options: options);
        var currency = DocumentRdlcExtensions.FromRdlcXml(Report(Textbox("=Format(Sum(Fields!Amount.Value, \"DS\"), \"C2\")")), Amounts, options: options);

        Assert.Equal("1244,75", Text(sum));
        Assert.Contains("1.244,75", Text(currency));
        Assert.DoesNotContain("¤", Text(currency));
    }

    [Fact]
    public void Culture_DefaultsToTheReportLanguage()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(
            Report(Textbox("=Format(Sum(Fields!Amount.Value, \"DS\"), \"N2\")"), "<Language>es-UY</Language>"), Amounts);

        Assert.Equal("1.244,75", Text(doc));
    }

    [Fact]
    public void Comparison_AndDateFormat_WorkUnderSpanishCulture()
    {
        var row = new Dictionary<string, object> { ["Amount"] = 1234.5m, ["Fecha"] = new DateTime(2026, 5, 10) };
        var ev = new RdlcExpressionEvaluator(culture: new CultureInfo("es-UY"));

        Assert.Equal("chico", ev.Evaluate("=IIF(Fields!Amount.Value > 2000, \"grande\", \"chico\")", row));
        Assert.Equal("2026-05-10", ev.Evaluate("=Format(Fields!Fecha.Value, \"yyyy-MM-dd\")", row));
    }

    // ── Expressions ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("=IIF(Fields!Active.Value, \"SI\", \"NO\")", "NO")]
    [InlineData("=IIF(Not Fields!Active.Value, \"SI\", \"NO\")", "SI")]
    [InlineData("=Fields!Qty.Value * Fields!Price.Value", "7.5")]
    [InlineData("=Fields!Qty.Value + 1", "4")]
    [InlineData("=IIF(Fields!Qty.Value > 1 And Fields!Price.Value > 100, \"ok\", \"fail\")", "fail")]
    [InlineData("=IIF(Fields!Qty.Value > 1 Or Fields!Price.Value > 100, \"ok\", \"fail\")", "ok")]
    [InlineData("=\"Total: \" & Format(Fields!Qty.Value * Fields!Price.Value, \"0.00\")", "Total: 7.50")]
    [InlineData("=UCase(Left(Fields!Name.Value, 3))", "WID")]
    [InlineData("=IIF(IsNothing(Fields!Missing.Value), \"-\", \"x\")", "-")]
    public void Expressions_AreEvaluatedWithTypes(string expression, string expected)
    {
        var row = new Dictionary<string, object> { ["Active"] = false, ["Qty"] = 3, ["Price"] = 2.5m, ["Name"] = "widget" };
        Assert.Equal(expected, new RdlcExpressionEvaluator().Evaluate(expression, row));
    }

    [Fact]
    public void UnsupportedExpression_IsReportedAsWarning()
    {
        var factory = new RdlcDocumentFactory();
        factory.ParseFromXml(Report(Textbox("=RowNumber(Nothing)")));

        Assert.Contains(factory.Warnings, w => w.Contains("RowNumber"));
    }

    [Fact]
    public void UnsupportedExpression_ThrowsInStrictMode()
    {
        var factory = new RdlcDocumentFactory(options: new RdlcImportOptions { StrictExpressions = true });
        Assert.Throws<NotSupportedException>(() => factory.ParseFromXml(Report(Textbox("=RowNumber(Nothing)"))));
    }

    // ── Layout and styles ────────────────────────────────────────────────────

    [Fact]
    public void ParagraphTextAlign_IsKept()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report("""
            <Textbox Name="T"><Paragraphs><Paragraph>
              <TextRuns><TextRun><Value>9.99</Value><Style><FontSize>10pt</FontSize></Style></TextRun></TextRuns>
              <Style><TextAlign>Right</TextAlign></Style>
            </Paragraph></Paragraphs><Style><PaddingLeft>2pt</PaddingLeft></Style></Textbox>
            """));

        Assert.Equal(TextAlignment.Right, FirstText(Content(doc)).Spans[0].Style.Alignment);
    }

    [Fact]
    public void TransparentBackground_AddsNoFill_AndNamedColorsResolve()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report(
            Textbox("a", "<BackgroundColor>Transparent</BackgroundColor><Color>SteelBlue</Color>")));

        var content = Content(doc);
        Assert.IsType<TextElement>(content is ColumnElement c ? c.Items[0] : content);
        Assert.Equal(70, FirstText(content).Spans[0].Style.Color.R);
    }

    private static string Cell(string value) =>
        $"<TablixCell><CellContents><Textbox Name=\"c{Guid.NewGuid():N}\"><Value>{value}</Value><Style><Color>=IIF(Fields!Amount.Value &lt; 100, \"Red\", \"Black\")</Color></Style></Textbox></CellContents></TablixCell>";

    private static readonly string TotalsTablix = $"""
        <Tablix Name="T"><DataSetName>DS</DataSetName><TablixBody>
          <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn><TablixColumn><Width>1in</Width></TablixColumn></TablixColumns>
          <TablixRows>
            <TablixRow><Height>0.2in</Height><TablixCells>{Cell("Producto")}{Cell("Importe")}</TablixCells></TablixRow>
            <TablixRow><Height>0.2in</Height><TablixCells>{Cell("=Fields!Name.Value")}{Cell("=Fields!Amount.Value")}</TablixCells></TablixRow>
            <TablixRow><Height>0.2in</Height><TablixCells>{Cell("TOTAL")}{Cell("=Sum(Fields!Amount.Value, \"DS\")")}</TablixCells></TablixRow>
          </TablixRows></TablixBody>
          <TablixRowHierarchy><TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name="Details" /></TablixMember>
            <TablixMember><KeepWithGroup>Before</KeepWithGroup></TablixMember>
          </TablixMembers></TablixRowHierarchy>
        </Tablix>
        """;

    private static TableElement Table(Document doc)
    {
        var content = Content(doc);
        return (TableElement)(content is ColumnElement c ? c.Items[0] : content);
    }

    [Fact]
    public void StaticRowsAfterTheDetails_BecomeFooterRows()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report(TotalsTablix), Amounts);
        var table = Table(doc);

        Assert.Equal(2, table.HeaderCells.Count);
        Assert.Equal(4, table.DataCells.Count);
        Assert.Equal(2, table.FooterCells.Count);

        var html = doc.GenerateHtml();
        Assert.True(html.IndexOf(">A<", StringComparison.Ordinal) < html.IndexOf("TOTAL", StringComparison.Ordinal));
    }

    [Fact]
    public void ColorExpressions_AreEvaluatedPerRow()
    {
        var table = Table(DocumentRdlcExtensions.FromRdlcXml(Report(TotalsTablix), Amounts));

        Assert.Equal(0, FirstText(table.DataCells[0].Content!).Spans[0].Style.Color.R);   // 1234.5 → Black
        Assert.Equal(255, FirstText(table.DataCells[2].Content!).Spans[0].Style.Color.R); // 10.25 → Red
    }

    [Fact]
    public void ColSpan_InsideCellContents_IsApplied()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report("""
            <Tablix Name="T"><TablixBody>
              <TablixColumns><TablixColumn><Width>1in</Width></TablixColumn><TablixColumn><Width>1in</Width></TablixColumn></TablixColumns>
              <TablixRows><TablixRow><Height>0.2in</Height><TablixCells>
                <TablixCell><CellContents><Textbox Name="M"><Value>MERGED</Value></Textbox><ColSpan>2</ColSpan></CellContents></TablixCell>
                <TablixCell />
              </TablixCells></TablixRow></TablixRows></TablixBody>
            </Tablix>
            """));

        var cells = Table(doc).HeaderCells.Concat(Table(doc).DataCells).ToList();
        Assert.Single(cells);
        Assert.Equal(2, cells[0].ColumnSpan);
    }

    [Fact]
    public void VerticalGapsBetweenItems_BecomeSpacers()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report(
            "<Textbox Name=\"A\"><Value>a</Value><Top>0in</Top><Height>0.25in</Height></Textbox>" +
            "<Textbox Name=\"B\"><Value>b</Value><Top>1.25in</Top><Height>0.25in</Height></Textbox>"));

        var column = Assert.IsType<ColumnElement>(Content(doc));
        var spacer = Assert.IsType<SpacerElement>(column.Items[1]);
        Assert.Equal(72f, spacer.Height, precision: 1);
    }

    [Fact]
    public void UnsupportedReportItems_AreReported()
    {
        var factory = new RdlcDocumentFactory();
        factory.ParseFromXml(Report("<Rectangle Name=\"Box\"><Top>0in</Top></Rectangle>" + Textbox("x")));

        Assert.Contains(factory.Warnings, w => w.Contains("Rectangle") && w.Contains("Box"));
    }

    // ── External images ──────────────────────────────────────────────────────

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];

    private static string ImageItem(string path) =>
        $"<Image Name=\"Logo\"><Source>External</Source><Value>{path}</Value></Image>";

    private static ImageElement Image(Document doc)
    {
        var content = Content(doc);
        return (ImageElement)(content is ColumnElement c ? c.Items[0] : content);
    }

    [Fact]
    public void ExternalImages_AreNotLoaded_WithoutAnAllowedDirectory()
    {
        var file = Path.Combine(Path.GetTempPath(), $"logo-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(file, Png);
        try
        {
            var factory = new RdlcDocumentFactory();
            var doc = factory.ParseFromXml(Report(ImageItem(file)));

            Assert.Empty(Image(doc).SourceBytes);
            Assert.Contains(factory.Warnings, w => w.Contains("ExternalImageDirectory"));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void ExternalImages_LoadOnlyFromInsideTheAllowedDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("fr-images-");
        var outside = Path.Combine(Path.GetTempPath(), $"secret-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(Path.Combine(dir.FullName, "logo.png"), Png);
        File.WriteAllBytes(outside, Png);
        try
        {
            var options = new RdlcImportOptions { ExternalImageDirectory = dir.FullName };

            var inside = DocumentRdlcExtensions.FromRdlcXml(Report(ImageItem("logo.png")), options: options);
            var traversal = DocumentRdlcExtensions.FromRdlcXml(Report(ImageItem("../" + Path.GetFileName(outside))), options: options);
            var absolute = DocumentRdlcExtensions.FromRdlcXml(Report(ImageItem(outside)), options: options);

            Assert.Equal("image/png", Image(inside).MimeType);
            Assert.Empty(Image(traversal).SourceBytes);
            Assert.Empty(Image(absolute).SourceBytes);
        }
        finally
        {
            dir.Delete(recursive: true);
            File.Delete(outside);
        }
    }

    [Fact]
    public void ExternalImages_FromFile_ResolveNextToTheReport()
    {
        var dir = Directory.CreateTempSubdirectory("fr-report-");
        try
        {
            File.WriteAllBytes(Path.Combine(dir.FullName, "logo.png"), Png);
            var reportPath = Path.Combine(dir.FullName, "report.rdlc");
            File.WriteAllText(reportPath, Report(ImageItem("logo.png")));

            Assert.Equal("image/png", Image(DocumentRdlcExtensions.FromRdlc(reportPath)).MimeType);
        }
        finally { dir.Delete(recursive: true); }
    }
}
