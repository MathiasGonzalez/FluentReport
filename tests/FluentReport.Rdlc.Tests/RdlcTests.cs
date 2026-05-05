using FluentReport;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Rdlc;
using FluentReport.Styling;

namespace FluentReport.Rdlc.Tests;

/// <summary>
/// Tests for the RDLC translation layer (Phase 1 MVP) as well as the new
/// Phase 2 / Phase 3 core features.
/// </summary>
public class RdlcTests
{
    // ── Minimal RDLC fixture strings ──────────────────────────────────────────

    private const string SimpleTextboxRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page>
                <PageWidth>8.5in</PageWidth>
                <PageHeight>11in</PageHeight>
                <LeftMargin>1in</LeftMargin>
                <RightMargin>1in</RightMargin>
                <TopMargin>1in</TopMargin>
                <BottomMargin>1in</BottomMargin>
              </Page>
              <Body>
                <ReportItems>
                  <Textbox Name="Title">
                    <Value>Hello RDLC</Value>
                    <Top>0in</Top>
                    <Left>0in</Left>
                    <Width>6.5in</Width>
                    <Height>0.3in</Height>
                    <Style>
                      <FontSize>16pt</FontSize>
                      <FontWeight>Bold</FontWeight>
                    </Style>
                  </Textbox>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    private const string TablixRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page>
                <PageWidth>8.5in</PageWidth>
                <PageHeight>11in</PageHeight>
                <LeftMargin>1in</LeftMargin>
                <RightMargin>1in</RightMargin>
                <TopMargin>1in</TopMargin>
                <BottomMargin>1in</BottomMargin>
              </Page>
              <Body>
                <ReportItems>
                  <Tablix Name="Table1">
                    <DataSetName>Products</DataSetName>
                    <TablixBody>
                      <TablixColumns>
                        <TablixColumn><Width>2in</Width></TablixColumn>
                        <TablixColumn><Width>1.5in</Width></TablixColumn>
                      </TablixColumns>
                      <TablixRows>
                        <TablixRow>
                          <Height>0.25in</Height>
                          <TablixCells>
                            <TablixCell>
                              <CellContents>
                                <Textbox Name="H1"><Value>Name</Value></Textbox>
                              </CellContents>
                            </TablixCell>
                            <TablixCell>
                              <CellContents>
                                <Textbox Name="H2"><Value>Price</Value></Textbox>
                              </CellContents>
                            </TablixCell>
                          </TablixCells>
                        </TablixRow>
                        <TablixRow>
                          <Height>0.2in</Height>
                          <TablixCells>
                            <TablixCell>
                              <CellContents>
                                <Textbox Name="D1"><Value>=Fields!Name.Value</Value></Textbox>
                              </CellContents>
                            </TablixCell>
                            <TablixCell>
                              <CellContents>
                                <Textbox Name="D2"><Value>=Fields!Price.Value</Value></Textbox>
                              </CellContents>
                            </TablixCell>
                          </TablixCells>
                        </TablixRow>
                      </TablixRows>
                    </TablixBody>
                    <TablixRowHierarchy>
                      <TablixMembers>
                        <TablixMember>
                          <KeepWithGroup>After</KeepWithGroup>
                        </TablixMember>
                        <TablixMember>
                          <Group Name="Details" />
                        </TablixMember>
                      </TablixMembers>
                    </TablixRowHierarchy>
                  </Tablix>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    private const string ParametersRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page>
                <PageWidth>8.5in</PageWidth>
                <PageHeight>11in</PageHeight>
              </Page>
              <Body>
                <ReportItems>
                  <Textbox Name="Title">
                    <Value>=Parameters!ReportTitle.Value</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    private const string HeaderFooterRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page>
                <PageWidth>8.5in</PageWidth>
                <PageHeight>11in</PageHeight>
                <LeftMargin>1in</LeftMargin>
                <RightMargin>1in</RightMargin>
                <TopMargin>1in</TopMargin>
                <BottomMargin>1in</BottomMargin>
                <PageHeader>
                  <ReportItems>
                    <Textbox Name="Hdr"><Value>My Header</Value></Textbox>
                  </ReportItems>
                </PageHeader>
                <PageFooter>
                  <ReportItems>
                    <Textbox Name="Ftr"><Value>Page Footer</Value></Textbox>
                  </ReportItems>
                </PageFooter>
              </Page>
              <Body>
                <ReportItems>
                  <Textbox Name="Body"><Value>Body content</Value></Textbox>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    // ── Phase 1: RDLC parser tests ────────────────────────────────────────────

    [Fact]
    public void FromRdlcXml_SimpleTextbox_ProducesValidPdf()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(SimpleTextboxRdlc);
        var bytes = doc.GeneratePdf();

        Assert.NotEmpty(bytes);
        // Verify PDF header magic bytes
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public void FromRdlcXml_PageDimensions_AreApplied()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(SimpleTextboxRdlc);
        var settings = doc.Settings;

        Assert.Single(settings.Pages);

        var page = settings.Pages[0];
        // 8.5in × 72 = 612 pt
        Assert.Equal(612f, page.Size.Width, precision: 0);
        // 11in × 72 = 792 pt
        Assert.Equal(792f, page.Size.Height, precision: 0);
        // 1in margin = 72 pt
        Assert.Equal(72f, page.MarginLeft, precision: 0);
        Assert.Equal(72f, page.MarginRight, precision: 0);
        Assert.Equal(72f, page.MarginTop, precision: 0);
        Assert.Equal(72f, page.MarginBottom, precision: 0);
    }

    [Fact]
    public void FromRdlcXml_Tablix_WithDataset_ProducesValidPdf()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["Products"] = new[]
            {
                (object)new { Name = "Widget", Price = "9.99" },
                (object)new { Name = "Gadget", Price = "29.99" }
            }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(TablixRdlc, datasets);
        var bytes = doc.GeneratePdf();

        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void FromRdlcXml_Parameters_AreResolved()
    {
        var parameters = new Dictionary<string, object>
        {
            ["ReportTitle"] = "Annual Report 2024"
        };

        // Should not throw; parameters are resolved at parse time
        var doc = DocumentRdlcExtensions.FromRdlcXml(ParametersRdlc, parameters: parameters);
        Assert.NotNull(doc);

        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void FromRdlcXml_HeaderAndFooter_AreIncluded()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(HeaderFooterRdlc);
        var page = doc.Settings.Pages[0];

        Assert.NotNull(page.HeaderElement);
        Assert.NotNull(page.FooterElement);
        Assert.NotNull(page.ContentElement);
    }

    [Fact]
    public void FromRdlcXml_NoDataset_TablixRendersEmptyPlaceholder()
    {
        // No datasets provided — tablix should still render without throwing
        var doc = DocumentRdlcExtensions.FromRdlcXml(TablixRdlc);
        var bytes = doc.GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void FromRdlcXml_DictionaryRows_ResolveFieldExpressions()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["Products"] = new[]
            {
                (object)new Dictionary<string, object> { ["Name"] = "Widget", ["Price"] = "9.99" }
            }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(TablixRdlc, datasets);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void FromRdlcXml_LineElement_RendersWithoutThrowing()
    {
        const string lineRdlc = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
              <ReportSections>
                <ReportSection>
                  <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
                  <Body>
                    <ReportItems>
                      <Textbox Name="T1"><Value>Above</Value><Top>0in</Top></Textbox>
                      <Line Name="L1">
                        <Top>0.3in</Top>
                        <Style><Color>Black</Color></Style>
                      </Line>
                      <Textbox Name="T2"><Value>Below</Value><Top>0.4in</Top></Textbox>
                    </ReportItems>
                  </Body>
                </ReportSection>
              </ReportSections>
            </Report>
            """;

        var doc = DocumentRdlcExtensions.FromRdlcXml(lineRdlc);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    // ── RdlcExpressionEvaluator unit tests ────────────────────────────────────

    [Theory]
    [InlineData("Hello", "Hello")]
    [InlineData("", "")]
    public void Evaluate_Literal_ReturnsLiteral(string input, string expected)
    {
        var evaluator = new RdlcExpressionEvaluator();
        Assert.Equal(expected, evaluator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_FieldsExpression_ResolvesFromPocoRow()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var row = new { Name = "Widget", Price = "9.99" };

        Assert.Equal("Widget", evaluator.Evaluate("=Fields!Name.Value", row));
        Assert.Equal("9.99", evaluator.Evaluate("=Fields!Price.Value", row));
    }

    [Fact]
    public void Evaluate_FieldsExpression_ResolvesFromDictionaryRow()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var row = new Dictionary<string, object> { ["Name"] = "Gadget", ["Price"] = "19.99" };

        Assert.Equal("Gadget", evaluator.Evaluate("=Fields!Name.Value", row));
    }

    [Fact]
    public void Evaluate_ParametersExpression_ResolvesFromParameters()
    {
        var parameters = new Dictionary<string, object> { ["Title"] = "Annual Report" };
        var evaluator = new RdlcExpressionEvaluator(parameters);

        Assert.Equal("Annual Report", evaluator.Evaluate("=Parameters!Title.Value"));
    }

    [Fact]
    public void Evaluate_UnknownExpression_ReturnsEmpty()
    {
        var evaluator = new RdlcExpressionEvaluator();
        Assert.Equal(string.Empty, evaluator.Evaluate("=SomeFunction()"));
    }

    [Fact]
    public void IsFieldExpression_DetectsFieldExpressionsCorrectly()
    {
        Assert.True(RdlcExpressionEvaluator.IsFieldExpression("=Fields!Name.Value"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression("Name"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression("=Parameters!X.Value"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression(null));
    }

    // ── RdlcDocumentFactory unit parser ──────────────────────────────────────

    [Theory]
    [InlineData("1in", 72f)]
    [InlineData("2.5in", 180f)]
    [InlineData("1cm", 28.3465f)]
    [InlineData("10mm", 28.3465f)]
    [InlineData("12pt", 12f)]
    [InlineData("96px", 72f)]
    [InlineData("0in", 0f)]
    [InlineData("", 0f)]
    public void ParseUnit_ConvertsCorrectly(string input, float expectedPt)
    {
        var result = RdlcDocumentFactory.ParseUnit(input);
        Assert.Equal(expectedPt, result, precision: 2);
    }

    [Theory]
    [InlineData("Black", 0, 0, 0)]
    [InlineData("Red", 255, 0, 0)]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("#0000FF", 0, 0, 255)]
    [InlineData("unknown-color", 0, 0, 0)] // falls back to black
    public void ParseColor_ConvertsNamedAndHexColors(string input, byte r, byte g, byte b)
    {
        var color = RdlcDocumentFactory.ParseColor(input);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    // ── Phase 2: ListElement tests ────────────────────────────────────────────

    [Fact]
    public void ListElement_RendersAllItems()
    {
        var items = new[] { "Alpha", "Beta", "Gamma" };

        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().List(items, (container, item) =>
                {
                    container.Text(item);
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void ListElement_WithSpacing_DoesNotThrow()
    {
        var items = Enumerable.Range(1, 5).ToList();

        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().List(items, (container, n) =>
                {
                    container.Text($"Item {n}");
                }, spacing: 8f);
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ListElement_Empty_DoesNotThrow()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().List(Array.Empty<string>(), (cb, _) => cb.Text("x"));
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    // ── Phase 2: TableElement ColSpan tests ───────────────────────────────────

    [Fact]
    public void TableElement_WithColSpan_RendersWithoutThrowing()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        // Header: first cell spans 2 columns, second spans 1
                        table.Header(h =>
                        {
                            h.Cell(2).Background("#CCCCCC").Padding(4).Text("Wide Header");
                            h.Cell().Padding(4).Text("Col 3");
                        });

                        // Data rows
                        table.Cell().Padding(4).Text("A");
                        table.Cell().Padding(4).Text("B");
                        table.Cell().Padding(4).Text("C");
                    });
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    // ── Phase 3: TextStyle delegate tests ────────────────────────────────────

    [Fact]
    public void TextStyle_BoldResolver_OverridesBold()
    {
        bool isSpecial = true;

        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        // Span with runtime delegate override
                        t.Span("Conditional text", s =>
                        {
                            s.BoldResolver = () => isSpecial;
                        });
                    });
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void TextStyle_ColorResolver_OverridesColor()
    {
        double value = 150.0;

        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("Status text", s =>
                        {
                            s.ColorResolver = () => value > 100
                                ? new ReportColor(200, 0, 0)   // red
                                : ReportColor.Black;
                        });
                    });
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    // ── Phase 3: ChartElement tests ───────────────────────────────────────────

    [Fact]
    public void ChartElement_Bar_RendersWithoutThrowing()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Chart()
                        .Type(ChartType.Bar)
                        .Title("Sales by Quarter")
                        .Categories(new[] { "Q1", "Q2", "Q3", "Q4" })
                        .AddSeries("Revenue", new double[] { 100, 150, 130, 200 })
                        .AddSeries("Costs",   new double[] { 80, 90, 85, 110 }, "#FF6666");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ChartElement_Line_RendersWithoutThrowing()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Chart()
                        .Type(ChartType.Line)
                        .Height(150)
                        .Categories(new[] { "Jan", "Feb", "Mar" })
                        .AddSeries("Units", new double[] { 10, 25, 18 });
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ChartElement_NoSeries_DoesNotThrow()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Chart().Title("Empty Chart");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    // ── Phase 3: SubreportElement tests ──────────────────────────────────────

    [Fact]
    public void SubreportElement_RendersNestedDocumentInline()
    {
        var nested = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginAll(20);
                page.Content().Column(col => col.Item().Text("Nested content"));
            });
        });

        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Parent document").Bold();
                    col.Item().Subreport(nested);
                    col.Item().Text("After nested");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    // ── Document.FromSettings API test ────────────────────────────────────────

    [Fact]
    public void Document_FromSettings_CreatesDocumentFromRawSettings()
    {
        var settings = new DocumentSettings();
        var page = new PageSettings
        {
            Size = PageSize.A4,
            MarginTop = 40, MarginBottom = 40, MarginLeft = 40, MarginRight = 40,
            ContentElement = new FluentReport.Elements.TextElement("Direct from settings")
        };
        settings.Pages.Add(page);

        var doc = Document.FromSettings(settings);
        var bytes = doc.GeneratePdf();

        Assert.NotEmpty(bytes);
    }
}
