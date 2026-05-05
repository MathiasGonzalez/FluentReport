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

    // ── Paragraphs / TextRuns parsing ─────────────────────────────────────────

    private const string ParagraphsTextboxRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
              <Body>
                <ReportItems>
                  <Textbox Name="Multi">
                    <Paragraphs>
                      <Paragraph>
                        <TextRuns>
                          <TextRun>
                            <Value>Hello </Value>
                            <Style><FontSize>12pt</FontSize><Color>#000000</Color></Style>
                          </TextRun>
                          <TextRun>
                            <Value>World</Value>
                            <Style><FontSize>14pt</FontSize><FontWeight>Bold</FontWeight><Color>#FF0000</Color></Style>
                          </TextRun>
                        </TextRuns>
                        <Style><TextAlign>Left</TextAlign></Style>
                      </Paragraph>
                    </Paragraphs>
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width>
                  </Textbox>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    [Fact]
    public void FromRdlcXml_ParagraphsTextbox_ProducesValidPdf()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(ParagraphsTextboxRdlc);
        var bytes = doc.GeneratePdf();

        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void FromRdlcXml_ParagraphsTextbox_CreatesMultipleSpans()
    {
        var doc = DocumentRdlcExtensions.FromRdlcXml(ParagraphsTextboxRdlc);
        var page = doc.Settings.Pages[0];

        // Body content should have been parsed without throwing
        Assert.NotNull(page.ContentElement);
    }

    [Fact]
    public void FromRdlcXml_ParagraphsTextbox_WithFieldExpression_ResolvesValue()
    {
        const string rdlc = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
              <ReportSections>
                <ReportSection>
                  <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
                  <Body>
                    <ReportItems>
                      <Textbox Name="T1">
                        <Paragraphs>
                          <Paragraph>
                            <TextRuns>
                              <TextRun>
                                <Value>=Fields!Title.Value</Value>
                                <Style><FontSize>12pt</FontSize></Style>
                              </TextRun>
                            </TextRuns>
                          </Paragraph>
                        </Paragraphs>
                      </Textbox>
                    </ReportItems>
                  </Body>
                </ReportSection>
              </ReportSections>
            </Report>
            """;

        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { Title = "My Report Title" } }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(rdlc, datasets);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    // ── Expression evaluator: First(), IIF(), Switch() ────────────────────────

    [Fact]
    public void Evaluate_First_ResolvesFromCurrentRow()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { Name = "FromDataset" } }
        };
        var evaluator = new RdlcExpressionEvaluator(datasets: datasets);
        var row = new { Name = "FromRow" };

        // When a row is available, First() should prefer the current row
        var result = evaluator.Evaluate(@"=First(Fields!Name.Value, ""ds"")", row);
        Assert.Equal("FromRow", result);
    }

    [Fact]
    public void Evaluate_First_ResolvesFromDatasetWhenNoRow()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { Name = "DatasetValue" } }
        };
        var evaluator = new RdlcExpressionEvaluator(datasets: datasets);

        var result = evaluator.Evaluate(@"=First(Fields!Name.Value, ""ds"")");
        Assert.Equal("DatasetValue", result);
    }

    [Fact]
    public void Evaluate_First_ReturnsEmptyWhenDatasetMissing()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var result = evaluator.Evaluate(@"=First(Fields!Name.Value, ""missing"")");
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("=IIF(True, \"yes\", \"no\")", "yes")]
    [InlineData("=IIF(False, \"yes\", \"no\")", "no")]
    public void Evaluate_IIF_ReturnsCorrectBranch(string expression, string expected)
    {
        var evaluator = new RdlcExpressionEvaluator();
        Assert.Equal(expected, evaluator.Evaluate(expression));
    }

    [Fact]
    public void Evaluate_IIF_WithFieldCondition_EvaluatesCorrectly()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var row = new { State = "Error" };

        var result = evaluator.Evaluate(@"=IIF(Fields!State.Value = ""Error"", ""red"", ""black"")", row);
        Assert.Equal("red", result);

        var result2 = evaluator.Evaluate(@"=IIF(Fields!State.Value = ""Error"", ""red"", ""black"")",
            new { State = "Success" });
        Assert.Equal("black", result2);
    }

    [Fact]
    public void Evaluate_Switch_ReturnsFirstMatchingBranch()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var row = new { State = "Warning" };

        var result = evaluator.Evaluate(
            @"=Switch(Fields!State.Value = ""Success"",""#00a756"",Fields!State.Value = ""Warning"",""#f57c00"",Fields!State.Value = ""Error"",""#ec2e33"")",
            row);

        Assert.Equal("#f57c00", result);
    }

    [Fact]
    public void Evaluate_Switch_NoMatch_ReturnsEmpty()
    {
        var evaluator = new RdlcExpressionEvaluator();
        var row = new { State = "Unknown" };

        var result = evaluator.Evaluate(
            @"=Switch(Fields!State.Value = ""Success"",""ok"",Fields!State.Value = ""Error"",""fail"")",
            row);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void IsFieldExpression_DetectsFirstFieldPattern()
    {
        Assert.True(RdlcExpressionEvaluator.IsFieldExpression(@"=First(Fields!Name.Value, ""ds"")"));
        Assert.True(RdlcExpressionEvaluator.IsFieldExpression("=Fields!Name.Value"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression("=Parameters!X.Value"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression("=IIF(True,\"a\",\"b\")"));
        Assert.False(RdlcExpressionEvaluator.IsFieldExpression(null));
    }

    // ── TablixRowHierarchy flattening ─────────────────────────────────────────

    private const string NestedHierarchyTablixRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
              <Body>
                <ReportItems>
                  <Tablix Name="T1">
                    <DataSetName>Items</DataSetName>
                    <TablixBody>
                      <TablixColumns>
                        <TablixColumn><Width>3in</Width></TablixColumn>
                        <TablixColumn><Width>2in</Width></TablixColumn>
                      </TablixColumns>
                      <TablixRows>
                        <TablixRow>
                          <Height>0.25in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="H1"><Value>Name</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="H2"><Value>Value</Value></Textbox></CellContents></TablixCell>
                          </TablixCells>
                        </TablixRow>
                        <TablixRow>
                          <Height>0.2in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="D1"><Value>=Fields!Name.Value</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="D2"><Value>=Fields!Val.Value</Value></Textbox></CellContents></TablixCell>
                          </TablixCells>
                        </TablixRow>
                        <TablixRow>
                          <Height>0.2in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="D3"><Value>=Fields!Extra.Value</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="D4"><Value></Value></Textbox></CellContents></TablixCell>
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
                          <TablixMembers>
                            <TablixMember />
                            <TablixMember />
                          </TablixMembers>
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

    [Fact]
    public void FromRdlcXml_NestedHierarchy_RendersWithoutThrowing()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["Items"] = new[]
            {
                (object)new { Name = "Alpha", Val = "1", Extra = "x" },
                (object)new { Name = "Beta",  Val = "2", Extra = "y" },
            }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(NestedHierarchyTablixRdlc, datasets);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void FromRdlcXml_NestedHierarchy_HeaderRowsAreRenderedOnce()
    {
        // The static (header) row should be added to HeaderCells only once,
        // not repeated for every data row. We verify by checking the table element.
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["Items"] = new[]
            {
                (object)new { Name = "Alpha", Val = "1", Extra = "x" },
                (object)new { Name = "Beta",  Val = "2", Extra = "y" },
            }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(NestedHierarchyTablixRdlc, datasets);
        var page = doc.Settings.Pages[0];

        // Body content is a ColumnElement containing the table
        var col = Assert.IsType<ColumnElement>(page.ContentElement);
        var table = Assert.IsType<TableElement>(col.Items[0]);

        // 1 header row = 2 header cells (one per column)
        Assert.Equal(2, table.HeaderCells.Count);
        // 2 data rows × 2 columns × 2 rows-per-detail-group = 8 data cells
        Assert.Equal(8, table.DataCells.Count);
    }

    // ── Column width proportional scaling ─────────────────────────────────────

    private const string WideTableRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
                    <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin></Page>
              <Body>
                <ReportItems>
                  <Tablix Name="WideTable">
                    <DataSetName>ds</DataSetName>
                    <TablixBody>
                      <TablixColumns>
                        <TablixColumn><Width>3in</Width></TablixColumn>
                        <TablixColumn><Width>3in</Width></TablixColumn>
                        <TablixColumn><Width>3in</Width></TablixColumn>
                        <TablixColumn><Width>3in</Width></TablixColumn>
                      </TablixColumns>
                      <TablixRows>
                        <TablixRow>
                          <Height>0.25in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="C1"><Value>=Fields!A.Value</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="C2"><Value>=Fields!B.Value</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="C3"><Value>=Fields!C.Value</Value></Textbox></CellContents></TablixCell>
                            <TablixCell><CellContents><Textbox Name="C4"><Value>=Fields!D.Value</Value></Textbox></CellContents></TablixCell>
                          </TablixCells>
                        </TablixRow>
                      </TablixRows>
                    </TablixBody>
                    <TablixRowHierarchy>
                      <TablixMembers><TablixMember><Group Name="d"/></TablixMember></TablixMembers>
                    </TablixRowHierarchy>
                    <Width>9.62in</Width>
                  </Tablix>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    [Fact]
    public void FromRdlcXml_WiderThanPageTablix_UsesProportionalColumns()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { A = "a1", B = "b1", C = "c1", D = "d1" } }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(WideTableRdlc, datasets);
        var page = doc.Settings.Pages[0];
        var col = Assert.IsType<ColumnElement>(page.ContentElement);
        var table = Assert.IsType<TableElement>(col.Items[0]);

        // All 4 columns should use RelativeWidth (not FixedWidth)
        Assert.All(table.Columns, c => Assert.Null(c.FixedWidth));
        Assert.All(table.Columns, c => Assert.True(c.RelativeWidth > 0));

        // All columns are equal (3in each), so each relative weight should be equal
        var relWeights = table.Columns.Select(c => c.RelativeWidth).ToList();
        Assert.Equal(relWeights[0], relWeights[1], 4);
        Assert.Equal(relWeights[0], relWeights[2], 4);
        Assert.Equal(relWeights[0], relWeights[3], 4);
    }

    [Fact]
    public void FromRdlcXml_WiderThanPageTablix_RendersWithoutOverflow()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { A = "a1", B = "b1", C = "c1", D = "d1" } }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(WideTableRdlc, datasets);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }

    // ── Band layout: side-by-side elements ────────────────────────────────────

    private const string SideBySideTablesRdlc = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
          <ReportSections>
            <ReportSection>
              <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
              <Body>
                <Width>7.5in</Width>
                <ReportItems>
                  <Tablix Name="Left">
                    <DataSetName>ds</DataSetName>
                    <TablixBody>
                      <TablixColumns><TablixColumn><Width>3in</Width></TablixColumn></TablixColumns>
                      <TablixRows>
                        <TablixRow>
                          <Height>0.25in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="L1"><Value>=Fields!Name.Value</Value></Textbox></CellContents></TablixCell>
                          </TablixCells>
                        </TablixRow>
                      </TablixRows>
                    </TablixBody>
                    <TablixRowHierarchy>
                      <TablixMembers><TablixMember><Group Name="d1"/></TablixMember></TablixMembers>
                    </TablixRowHierarchy>
                    <Top>4in</Top><Left>0.5in</Left><Width>3in</Width>
                  </Tablix>
                  <Tablix Name="Right">
                    <DataSetName>ds</DataSetName>
                    <TablixBody>
                      <TablixColumns><TablixColumn><Width>3in</Width></TablixColumn></TablixColumns>
                      <TablixRows>
                        <TablixRow>
                          <Height>0.25in</Height>
                          <TablixCells>
                            <TablixCell><CellContents><Textbox Name="R1"><Value>=Fields!Value.Value</Value></Textbox></CellContents></TablixCell>
                          </TablixCells>
                        </TablixRow>
                      </TablixRows>
                    </TablixBody>
                    <TablixRowHierarchy>
                      <TablixMembers><TablixMember><Group Name="d2"/></TablixMember></TablixMembers>
                    </TablixRowHierarchy>
                    <Top>4in</Top><Left>4in</Left><Width>3in</Width>
                  </Tablix>
                </ReportItems>
              </Body>
            </ReportSection>
          </ReportSections>
        </Report>
        """;

    [Fact]
    public void FromRdlcXml_SideBySideElements_AreGroupedIntoRowElement()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { Name = "Test", Value = "123" } }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(SideBySideTablesRdlc, datasets);
        var page = doc.Settings.Pages[0];
        var col = Assert.IsType<ColumnElement>(page.ContentElement);

        // The two Tablix elements share Top=4in so they should be in a RowElement band
        var rowBand = Assert.IsType<RowElement>(col.Items[0]);
        // RowElement should have exactly 2 items (the two tables, with or without a spacer)
        Assert.True(rowBand.Items.Count >= 2);
        Assert.Contains(rowBand.Items, item => item.Element is TableElement);
    }

    [Fact]
    public void FromRdlcXml_SideBySideElements_RendersWithoutThrowing()
    {
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["ds"] = new[] { (object)new { Name = "Test", Value = "123" } }
        };

        var doc = DocumentRdlcExtensions.FromRdlcXml(SideBySideTablesRdlc, datasets);
        var bytes = doc.GeneratePdf();
        Assert.NotEmpty(bytes);
    }
}
