using System.Globalization;
using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Rdlc.Tests;

/// <summary>
/// Report constructs as the Visual Studio report designer writes them: row groups with headers and
/// subtotals, sorting, filters, Format, visibility, borders, rectangles and page numbers.
/// </summary>
public class RdlcReportFeatureTests
{
    private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";

    private static readonly CultureInfo EsUy = new("es-UY");

    private static readonly List<object> Sales =
    [
        new Dictionary<string, object> { ["Cat"] = "B", ["Item"] = "b1", ["Amount"] = 10m, ["Date"] = new DateTime(2026, 3, 5) },
        new Dictionary<string, object> { ["Cat"] = "A", ["Item"] = "a1", ["Amount"] = 1m,  ["Date"] = new DateTime(2026, 3, 1) },
        new Dictionary<string, object> { ["Cat"] = "B", ["Item"] = "b2", ["Amount"] = 20m, ["Date"] = new DateTime(2026, 3, 6) },
        new Dictionary<string, object> { ["Cat"] = "A", ["Item"] = "a2", ["Amount"] = 2m,  ["Date"] = new DateTime(2026, 3, 2) },
    ];

    // ── RDLC builders ────────────────────────────────────────────────────────

    private static string Report(string body, string page = "", string dataSets = "") => $"""
        <Report xmlns="{Ns}">
          <DataSets><DataSet Name="Sales">{dataSets}</DataSet></DataSets>
          <ReportSections><ReportSection>
            <Body><ReportItems>{body}</ReportItems><Height>2in</Height></Body>
            <Width>6in</Width>
            <Page>{page}<PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
          </ReportSection></ReportSections>
        </Report>
        """;

    private static string Box(string value, string name = "T", string style = "", string runStyle = "",
                              string top = "0in", string left = "0in", string width = "1in", string height = "0.25in", string extra = "") => $"""
        <Textbox Name="{name}">
          <Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value><Style>{runStyle}</Style></TextRun></TextRuns></Paragraph></Paragraphs>
          <Top>{top}</Top><Left>{left}</Left><Height>{height}</Height><Width>{width}</Width>{extra}
          <Style>{style}</Style>
        </Textbox>
        """;

    private static string Cell(string value, int colSpan = 1, string runStyle = "", string style = "")
        => $"<TablixCell><CellContents>{Box(value, "C" + Guid.NewGuid().ToString("N")[..8], style, runStyle)}"
           + (colSpan > 1 ? $"<ColSpan>{colSpan}</ColSpan>" : "") + "</CellContents></TablixCell>"
           + string.Concat(Enumerable.Repeat("<TablixCell />", colSpan - 1));

    private static string Row(params string[] cells) => $"<TablixRow><Height>0.25in</Height><TablixCells>{string.Concat(cells)}</TablixCells></TablixRow>";

    private static string Tablix(string rows, string rowHierarchy, int columns = 2, string extra = "", string columnHierarchy = "") => $"""
        <Tablix Name="Table">
          <TablixBody>
            <TablixColumns>{string.Concat(Enumerable.Repeat("<TablixColumn><Width>1in</Width></TablixColumn>", columns))}</TablixColumns>
            <TablixRows>{rows}</TablixRows>
          </TablixBody>
          <TablixColumnHierarchy><TablixMembers>{(columnHierarchy.Length > 0 ? columnHierarchy : string.Concat(Enumerable.Repeat("<TablixMember />", columns)))}</TablixMembers></TablixColumnHierarchy>
          <TablixRowHierarchy><TablixMembers>{rowHierarchy}</TablixMembers></TablixRowHierarchy>
          <DataSetName>Sales</DataSetName>{extra}
          <Top>0in</Top><Left>0in</Left><Height>1in</Height><Width>2in</Width>
        </Tablix>
        """;

    private const string Details = "<TablixMember><Group Name=\"Details\" /></TablixMember>";

    /// <summary>Header row, then one group per Cat (sorted) with a header, details and a subtotal, then a grand total.</summary>
    private static string GroupedTable(string detailSort = "") => Tablix(
        Row(Cell("Item"), Cell("Amount"))
        + Row(Cell("=Fields!Cat.Value", colSpan: 2))
        + Row(Cell("=Fields!Item.Value"), Cell("=Fields!Amount.Value"))
        + Row(Cell("Subtotal"), Cell("=Sum(Fields!Amount.Value)"))
        + Row(Cell("Total"), Cell("=Sum(Fields!Amount.Value)")),
        $"""
        <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
        <TablixMember>
          <Group Name="Cat"><GroupExpressions><GroupExpression>=Fields!Cat.Value</GroupExpression></GroupExpressions></Group>
          <SortExpressions><SortExpression><Value>=Fields!Cat.Value</Value></SortExpression></SortExpressions>
          <TablixMembers>
            <TablixMember />
            <TablixMember><Group Name="Details" />{detailSort}</TablixMember>
            <TablixMember />
          </TablixMembers>
        </TablixMember>
        <TablixMember />
        """);

    private static Document Parse(string xml, RdlcDocumentFactory? factory = null)
        => (factory ?? Factory()).ParseFromXml(xml);

    private static RdlcDocumentFactory Factory(IDictionary<string, object>? parameters = null) => new(
        datasets: new Dictionary<string, IEnumerable<object>> { ["Sales"] = Sales },
        parameters: parameters,
        options: new RdlcImportOptions { Culture = EsUy });

    // ── Tree helpers ─────────────────────────────────────────────────────────

    private static IElement Content(Document doc) => doc.Settings.Pages[0].ContentElement!;

    private static IEnumerable<IElement> Descendants(IElement? element)
    {
        if (element == null) yield break;
        yield return element;
        IEnumerable<IElement?> children = element switch
        {
            ColumnElement c => c.Items,
            RowElement r => r.Items.Select(i => i.Element),
            PaddingElement p => [p.Child],
            BorderElement b => [b.Child],
            AlignElement a => [a.Child],
            TableElement t => t.HeaderCells.Concat(t.DataCells).Concat(t.FooterCells).Select(c => c.Content),
            _ => []
        };
        foreach (var child in children)
            foreach (var d in Descendants(child))
                yield return d;
    }

    private static string TextOf(IElement? element)
        => string.Concat(Descendants(element).OfType<TextElement>().SelectMany(t => t.Spans).Select(s => s.StaticText));

    private static TableElement Table(Document doc) => Descendants(Content(doc)).OfType<TableElement>().Single();

    /// <summary>Rows of a table section as cell texts joined with '|'.</summary>
    private static List<string> Rows(TableElement table, List<TableCell> cells)
        => table.PartitionIntoRows(cells).Select(r => string.Join("|", r.Select(c => TextOf(c.Cell.Content)))).ToList();

    // ── Row groups ───────────────────────────────────────────────────────────

    [Fact]
    public void RowGroups_RenderAHeaderAndASubtotalPerGroup()
    {
        var table = Table(Parse(Report(GroupedTable())));

        Assert.Equal(["Item|Amount"], Rows(table, table.HeaderCells));
        Assert.Equal(
            ["A", "a1|1", "a2|2", "Subtotal|3", "B", "b1|10", "b2|20", "Subtotal|30"],
            Rows(table, table.DataCells));
        Assert.Equal(["Total|33"], Rows(table, table.FooterCells));
    }

    [Fact]
    public void DetailSortExpressions_OrderTheRowsInsideEachGroup()
    {
        var sort = "<SortExpressions><SortExpression><Value>=Fields!Amount.Value</Value><Direction>Descending</Direction></SortExpression></SortExpressions>";
        var table = Table(Parse(Report(GroupedTable(sort))));

        Assert.Equal(
            ["A", "a2|2", "a1|1", "Subtotal|3", "B", "b2|20", "b1|10", "Subtotal|30"],
            Rows(table, table.DataCells));
    }

    [Fact]
    public void AggregateInADetailRow_UsesTheRow_UnlessAScopeIsNamed()
    {
        var table = Table(Parse(Report(Tablix(
            Row(Cell("=Sum(Fields!Amount.Value)"), Cell("=Sum(Fields!Amount.Value, \"Sales\")")),
            Details))));

        Assert.Equal(["10|33", "1|33", "20|33", "2|33"], Rows(table, table.DataCells));
    }

    [Fact]
    public void RowNumber_AndRunningValue_FollowTheDisplayedOrder()
    {
        var table = Table(Parse(Report(Tablix(
            Row(Cell("=RowNumber(Nothing)"), Cell("=RowNumber(\"Cat\")"), Cell("=RunningValue(Fields!Amount.Value, Sum, Nothing)")),
            """
            <TablixMember>
              <Group Name="Cat"><GroupExpressions><GroupExpression>=Fields!Cat.Value</GroupExpression></GroupExpressions></Group>
              <SortExpressions><SortExpression><Value>=Fields!Cat.Value</Value></SortExpression></SortExpressions>
              <TablixMembers><TablixMember><Group Name="Details" /></TablixMember></TablixMembers>
            </TablixMember>
            """, columns: 3))));

        // Displayed: a1, a2, b1, b2.
        Assert.Equal(["1|1|1", "2|2|3", "3|1|13", "4|2|33"], Rows(table, table.DataCells));
    }

    [Fact]
    public void TablixFilters_AndDataSetFilters_AreApplied()
    {
        var tablixFilter = """
            <Filters><Filter><FilterExpression>=Fields!Amount.Value</FilterExpression><Operator>GreaterThan</Operator>
            <FilterValues><FilterValue>=Parameters!Min.Value</FilterValue></FilterValues></Filter></Filters>
            """;
        var table = Table(Parse(Report(Tablix(Row(Cell("=Fields!Item.Value"), Cell("=Sum(Fields!Amount.Value, \"Table\")")), Details, extra: tablixFilter)),
            Factory(new Dictionary<string, object> { ["Min"] = 5 })));
        Assert.Equal(["b1|30", "b2|30"], Rows(table, table.DataCells));

        var dataSetFilter = """
            <Filters><Filter><FilterExpression>=Fields!Cat.Value</FilterExpression><Operator>In</Operator>
            <FilterValues><FilterValue>A</FilterValue></FilterValues></Filter></Filters>
            """;
        table = Table(Parse(Report(Tablix(Row(Cell("=Fields!Item.Value"), Cell("=Sum(Fields!Amount.Value, \"Sales\")")), Details), dataSets: dataSetFilter)));
        Assert.Equal(["a1|3", "a2|3"], Rows(table, table.DataCells));
    }

    [Fact]
    public void NoRowsMessage_ReplacesAnEmptyTable()
    {
        var factory = new RdlcDocumentFactory(datasets: new Dictionary<string, IEnumerable<object>> { ["Sales"] = [] });
        var doc = factory.ParseFromXml(Report(Tablix(Row(Cell("=Fields!Item.Value"), Cell("x")), Details, extra: "<NoRowsMessage>Sin datos</NoRowsMessage>")));

        Assert.Empty(Descendants(Content(doc)).OfType<TableElement>());
        Assert.Equal("Sin datos", TextOf(Content(doc)));
    }

    // ── Format, visibility ───────────────────────────────────────────────────

    [Fact]
    public void FormatProperty_IsAppliedToNumbersAndDates()
    {
        var table = Table(Parse(Report(Tablix(
            Row(Cell("=Fields!Amount.Value", runStyle: "<Format>N2</Format>"),
                Cell("=Fields!Date.Value", style: "<Format>dd/MM/yyyy</Format>"),
                Cell("=Fields!Item.Value", runStyle: "<Format>N2</Format>")),
            Details, columns: 3))));

        Assert.Equal("10,00|05/03/2026|b1", Rows(table, table.DataCells)[0]);
    }

    [Fact]
    public void HiddenItems_RowsAndColumns_AreNotRendered()
    {
        var hidden = "<Visibility><Hidden>=True</Hidden></Visibility>";
        var doc = Parse(Report(Box("secret", name: "Hidden", extra: hidden) + Box("shown", name: "Shown", top: "0.5in")));
        Assert.Equal("shown", TextOf(Content(doc)));

        // Second column hidden; rows hidden by an expression on the detail member.
        var table = Table(Parse(Report(Tablix(
            Row(Cell("=Fields!Item.Value"), Cell("=Fields!Amount.Value")),
            "<TablixMember><Group Name=\"Details\" /><Visibility><Hidden>=Fields!Cat.Value = \"B\"</Hidden></Visibility></TablixMember>",
            columnHierarchy: $"<TablixMember /><TablixMember>{hidden}</TablixMember>"))));

        Assert.Single(table.Columns);
        Assert.Equal(["a1", "a2"], Rows(table, table.DataCells));
    }

    // ── Borders and rectangles ───────────────────────────────────────────────

    [Fact]
    public void Borders_AreImportedPerSide()
    {
        var style = """
            <Border><Style>Solid</Style><Color>Black</Color><Width>1pt</Width></Border>
            <TopBorder><Style>None</Style></TopBorder>
            <BottomBorder><Color>Red</Color><Style>Solid</Style><Width>2pt</Width></BottomBorder>
            <BackgroundColor>LightGrey</BackgroundColor>
            """;
        var doc = Parse(Report(Box("x", style: style)));

        var borders = Descendants(Content(doc)).OfType<BorderElement>().ToList();
        Assert.Contains(borders, b => b.Border.Sides == (BorderSide.Left | BorderSide.Right) && b.Border.Width == 1);
        Assert.Contains(borders, b => b.Border.Sides == BorderSide.Bottom && b.Border.Width == 2 && b.Border.Color.R == 255);
        Assert.Single(borders, b => b.BackgroundColor != null);
        Assert.Equal("x", TextOf(Content(doc)));
    }

    [Fact]
    public void Rectangle_ImportsItsItems_WithItsBorder()
    {
        var rect = $"""
            <Rectangle Name="Header">
              <ReportItems>{Box("Left", name: "L", width: "2in")}{Box("Right", name: "R", left: "3in", width: "2in")}</ReportItems>
              <Top>0in</Top><Left>0in</Left><Height>1in</Height><Width>6in</Width>
              <Style><Border><Style>Solid</Style></Border></Style>
            </Rectangle>
            """;
        var factory = Factory();
        var doc = Parse(Report(rect), factory);

        Assert.DoesNotContain(factory.Warnings, w => w.Contains("Rectangle"));
        var border = Descendants(Content(doc)).OfType<BorderElement>().First(b => b.Border.Sides == BorderSide.All);
        Assert.Equal("LeftRight", TextOf(border));
        Assert.Contains(Descendants(border).OfType<RowElement>(), r => r.Items.Count == 3); // Left, gap, Right
    }

    // ── Page numbers and warnings ────────────────────────────────────────────

    [Fact]
    public void PageNumberGlobals_BecomePageNumberSpans()
    {
        var footer = $"""
            <PageFooter><Height>0.5in</Height><ReportItems>
            {Box("=\"Página \" &amp; Globals!PageNumber &amp; \" de \" &amp; Globals!TotalPages", width: "3in")}
            </ReportItems></PageFooter>
            """;
        var factory = Factory();
        var doc = Parse(Report(Box("body"), page: footer), factory);

        var spans = Descendants(doc.Settings.Pages[0].FooterElement).OfType<TextElement>().Single().Spans;
        Assert.Equal("Página ", spans[0].StaticText);
        Assert.True(spans[1].IsCurrentPage);
        Assert.Equal(" de ", spans[2].StaticText);
        Assert.True(spans[3].IsTotalPages);
        Assert.Empty(factory.Warnings);
    }

    [Fact]
    public void Warnings_AreReportedOnce()
    {
        var factory = Factory();
        Parse(Report(Tablix(Row(Cell("=InStr(Fields!Item.Value, \"1\")"), Cell("x")), Details)), factory);

        Assert.Single(factory.Warnings, w => w.Contains("InStr"));
    }

    // ── End to end ───────────────────────────────────────────────────────────

    [Fact]
    public void GroupedReport_RendersToPdf()
    {
        var footer = $"<PageFooter><Height>0.5in</Height><ReportItems>{Box("=Globals!PageNumber &amp; \"/\" &amp; Globals!TotalPages")}</ReportItems></PageFooter>";
        var many = Enumerable.Range(1, 200).Select(i => (object)new Dictionary<string, object>
        {
            ["Cat"] = "C" + i % 4, ["Item"] = "item " + i, ["Amount"] = (decimal)i, ["Date"] = new DateTime(2026, 1, 1).AddDays(i)
        }).ToList();
        var factory = new RdlcDocumentFactory(datasets: new Dictionary<string, IEnumerable<object>> { ["Sales"] = many });

        var doc = factory.ParseFromXml(Report(GroupedTable(), page: footer));
        var pdf = doc.GeneratePdf();

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(doc.GetPdfPageCount() > 1);
        Assert.Empty(factory.Warnings);
    }
}
