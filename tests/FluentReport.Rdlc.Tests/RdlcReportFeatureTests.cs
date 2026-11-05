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

    // ── Group order, nesting, row functions ──────────────────────────────────

    private static readonly List<object> Products =
    [
        new Dictionary<string, object> { ["Cat"] = "Limpieza", ["Item"] = "l1", ["Amount"] = 5m },
        new Dictionary<string, object> { ["Cat"] = "Lácteos",  ["Item"] = "x1", ["Amount"] = 1m },
        new Dictionary<string, object> { ["Cat"] = "Bebidas",  ["Item"] = "b1", ["Amount"] = 100m },
        new Dictionary<string, object> { ["Cat"] = "Lácteos",  ["Item"] = "x2", ["Amount"] = 2m },
    ];

    private static RdlcDocumentFactory ProductsFactory(IDictionary<string, object>? parameters = null) => new(
        datasets: new Dictionary<string, IEnumerable<object>> { ["Sales"] = Products },
        parameters: parameters,
        options: new RdlcImportOptions { Culture = EsUy });

    private static string CatGroup(string sort = "", string groupExtra = "") => $"""
        <TablixMember>
          <Group Name="Cat"><GroupExpressions><GroupExpression>=Fields!Cat.Value</GroupExpression></GroupExpressions>{groupExtra}</Group>
          {sort}
          <TablixMembers><TablixMember /><TablixMember><Group Name="Details" /></TablixMember></TablixMembers>
        </TablixMember>
        """;

    private static string CatTable(string sort = "", string groupExtra = "", string detailCell = "=Fields!Amount.Value") => Tablix(
        Row(Cell("=Fields!Cat.Value"), Cell("=Sum(Fields!Amount.Value)")) + Row(Cell("=Fields!Item.Value"), Cell(detailCell)),
        CatGroup(sort, groupExtra));

    private static List<string> GroupHeaders(TableElement table)
        => Rows(table, table.DataCells).Where(r => r.Split('|')[0].Length > 2).Select(r => r.Split('|')[0]).ToList();

    [Fact]
    public void Groups_WithoutSort_KeepFirstAppearanceOrder()
    {
        var table = Table(ProductsFactory().ParseFromXml(Report(CatTable())));
        Assert.Equal(["Limpieza", "Lácteos", "Bebidas"], GroupHeaders(table));
    }

    [Fact]
    public void GroupSort_UsesTheCultureCollation()
    {
        // Ordinal comparison would put "Lácteos" after "Limpieza".
        var sort = "<SortExpressions><SortExpression><Value>=Fields!Cat.Value</Value></SortExpression></SortExpressions>";
        var table = Table(ProductsFactory().ParseFromXml(Report(CatTable(sort))));
        Assert.Equal(["Bebidas", "Lácteos", "Limpieza"], GroupHeaders(table));
    }

    [Fact]
    public void GroupSort_CanUseAnAggregateOfTheGroup()
    {
        var sort = "<SortExpressions><SortExpression><Value>=Sum(Fields!Amount.Value)</Value><Direction>Descending</Direction></SortExpression></SortExpressions>";
        var table = Table(ProductsFactory().ParseFromXml(Report(CatTable(sort))));
        Assert.Equal(["Bebidas|100", "b1|100", "Limpieza|5", "l1|5", "Lácteos|3", "x1|1", "x2|2"], Rows(table, table.DataCells));
    }

    [Fact]
    public void GroupFilters_DropRowsBeforeGrouping()
    {
        var filter = """
            <Filters><Filter><FilterExpression>=Fields!Amount.Value</FilterExpression><Operator>LessThan</Operator>
            <FilterValues><FilterValue>50</FilterValue></FilterValues></Filter></Filters>
            """;
        var table = Table(ProductsFactory().ParseFromXml(Report(CatTable(groupExtra: filter))));
        Assert.Equal(["Limpieza", "Lácteos"], GroupHeaders(table));
    }

    [Fact]
    public void Previous_ReadsThePreviouslyDisplayedRow()
    {
        var table = Table(ProductsFactory().ParseFromXml(Report(CatTable(detailCell: "=Previous(Fields!Item.Value)"))));
        // Displayed: l1, x1, x2, b1.
        Assert.Equal(["Limpieza|5", "l1|", "Lácteos|3", "x1|l1", "x2|x1", "Bebidas|100", "b1|x2"], Rows(table, table.DataCells));
    }

    [Fact]
    public void NestedTablix_ShowsTheGroupRows_AndNumbersThemInItsOwnRegion()
    {
        var inner = $"""
            <Tablix Name="Inner">
              <TablixBody>
                <TablixColumns><TablixColumn><Width>1in</Width></TablixColumn><TablixColumn><Width>1in</Width></TablixColumn></TablixColumns>
                <TablixRows>{Row(Cell("=Fields!Item.Value"), Cell("=RowNumber(Nothing)"))}</TablixRows>
              </TablixBody>
              <TablixColumnHierarchy><TablixMembers><TablixMember /><TablixMember /></TablixMembers></TablixColumnHierarchy>
              <TablixRowHierarchy><TablixMembers>
                <TablixMember><Group Name="InnerDetails" />
                  <SortExpressions><SortExpression><Value>=Fields!Item.Value</Value><Direction>Descending</Direction></SortExpression></SortExpressions>
                </TablixMember>
              </TablixMembers></TablixRowHierarchy>
              <DataSetName>Sales</DataSetName>
            </Tablix>
            """;
        var outerSort = "<SortExpressions><SortExpression><Value>=Fields!Cat.Value</Value></SortExpression></SortExpressions>";
        var doc = ProductsFactory().ParseFromXml(Report(Tablix(
            $"<TablixRow><Height>1in</Height><TablixCells><TablixCell><CellContents>{inner}</CellContents></TablixCell></TablixCells></TablixRow>"
            + Row(Cell("=Fields!Item.Value &amp; \"#\" &amp; RowNumber(Nothing)")),
            CatGroup(outerSort), columns: 1)));

        var tables = Descendants(Content(doc)).OfType<TableElement>().ToList();
        var outer = tables[0];
        var inners = tables.Skip(1).Select(t => Rows(t, t.DataCells)).ToList();

        Assert.Equal([["b1|1"], ["x2|1", "x1|2"], ["l1|1"]], inners);
        // The inner tables do not disturb the outer numbering or order.
        Assert.Equal(["b1#1", "x1#2", "x2#3", "l1#4"], Rows(outer, outer.DataCells).Where(r => r.Contains('#')).ToList());
    }

    [Fact]
    public void ColSpan_OverAHiddenColumn_Shrinks()
    {
        var hidden = "<Visibility><Hidden>true</Hidden></Visibility>";
        var table = Table(Parse(Report(Tablix(
            Row(Cell("Title", colSpan: 3)) + Row(Cell("=Fields!Item.Value"), Cell("hidden"), Cell("=Fields!Amount.Value")),
            "<TablixMember />" + Details, columns: 3,
            columnHierarchy: $"<TablixMember /><TablixMember>{hidden}</TablixMember><TablixMember />"))));

        Assert.Equal(2, table.Columns.Count);
        Assert.Equal(2, Assert.Single(table.HeaderCells).ColumnSpan);
        Assert.Equal("b1|10", Rows(table, table.DataCells)[0]);
    }

    [Fact]
    public void TableWithOnlyStaticRows_RendersThemAsTheBody()
    {
        var table = Table(Parse(Report(Tablix(Row(Cell("Count"), Cell("=Count(Fields!Item.Value)")), "<TablixMember />"))));

        Assert.Empty(table.HeaderCells);
        Assert.Equal(["Count|4"], Rows(table, table.DataCells));
    }

    // ── Filter operators ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("=Fields!Amount.Value", "Between", "<FilterValue>2</FilterValue><FilterValue>5</FilterValue>", "l1,x2")]
    [InlineData("=Fields!Item.Value", "Like", "<FilterValue>x*</FilterValue>", "x1,x2")]
    [InlineData("=Fields!Cat.Value", "NotEqual", "<FilterValue>Lácteos</FilterValue>", "l1,b1")]
    [InlineData("=Fields!Cat.Value", "Equal", "<FilterValue>Bebidas</FilterValue>", "b1")]
    [InlineData("=Fields!Amount.Value", "GreaterThanOrEqual", "<FilterValue>5</FilterValue>", "l1,b1")]
    [InlineData("=Fields!Amount.Value", "LessThanOrEqual", "<FilterValue>2</FilterValue>", "x1,x2")]
    [InlineData("=Fields!Cat.Value", "In", "<FilterValue>=Parameters!Cats.Value</FilterValue>", "l1,b1")]
    public void FilterOperators(string expression, string op, string values, string expected)
    {
        var filter = $"<Filters><Filter><FilterExpression>{expression}</FilterExpression><Operator>{op}</Operator><FilterValues>{values}</FilterValues></Filter></Filters>";
        var factory = ProductsFactory(new Dictionary<string, object> { ["Cats"] = new[] { "Limpieza", "Bebidas" } });
        var table = Table(factory.ParseFromXml(Report(Tablix(Row(Cell("=Fields!Item.Value"), Cell("x")), Details, extra: filter))));

        Assert.Equal(expected, string.Join(",", Rows(table, table.DataCells).Select(r => r.Split('|')[0])));
        Assert.Empty(factory.Warnings);
    }

    [Fact]
    public void UnsupportedFilterOperator_IsReported_AndIgnored()
    {
        var filter = "<Filters><Filter><FilterExpression>=Fields!Amount.Value</FilterExpression><Operator>TopN</Operator><FilterValues><FilterValue>2</FilterValue></FilterValues></Filter></Filters>";
        var factory = ProductsFactory();
        var table = Table(factory.ParseFromXml(Report(Tablix(Row(Cell("=Fields!Item.Value"), Cell("x")), Details, extra: filter))));

        Assert.Equal(4, Rows(table, table.DataCells).Count);
        Assert.Contains(factory.Warnings, w => w.Contains("TopN"));
    }

    // ── Styles, items, globals ───────────────────────────────────────────────

    [Fact]
    public void Rdl2005Borders_AreImported()
    {
        var style = """
            <BorderStyle><Default>Solid</Default><Left>None</Left></BorderStyle>
            <BorderColor><Default>Blue</Default></BorderColor>
            <BorderWidth><Default>2pt</Default></BorderWidth>
            """;
        var border = Descendants(Content(Parse(Report(Box("x", style: style))))).OfType<BorderElement>().Single();

        Assert.Equal(BorderSide.Top | BorderSide.Right | BorderSide.Bottom, border.Border.Sides);
        Assert.Equal(2f, border.Border.Width);
        Assert.Equal(255, border.Border.Color.B);
    }

    [Fact]
    public void Line_TakesColorAndWidthFromItsBorder()
    {
        var line = """
            <Line Name="L"><Top>0in</Top><Left>0in</Left><Height>0in</Height><Width>2in</Width>
              <Style><Border><Style>Solid</Style><Color>Red</Color><Width>3pt</Width></Border></Style></Line>
            """;
        var element = Descendants(Content(Parse(Report(line)))).OfType<LineElement>().Single();

        Assert.Equal(3f, element.Thickness);
        Assert.Equal(255, element.Color.R);
    }

    [Fact]
    public void PageBreaks_OnBodyItems_AreKept()
    {
        var breakAfter = "<PageBreak><BreakLocation>End</BreakLocation></PageBreak>";
        var doc = Parse(Report(Box("one", name: "A", extra: breakAfter) + Box("two", name: "B", top: "0.5in")));

        var items = Assert.IsType<ColumnElement>(Content(doc)).Items;
        Assert.Contains(items, i => i is PageBreakElement);
        Assert.Equal(2, doc.GetPdfPageCount());
    }

    [Fact]
    public void HiddenItems_TakeNoSpace()
    {
        var hidden = "<Visibility><Hidden>true</Hidden></Visibility>";
        var doc = Parse(Report(
            Box("a", name: "A") + Box("hidden", name: "H", top: "1in", extra: hidden) + Box("b", name: "B", top: "1.25in")));

        // Only the gap from "a" to "b" is reproduced: 1.25in - 0.25in = 72pt.
        var spacer = Assert.Single(Assert.IsType<ColumnElement>(Content(doc)).Items.OfType<SpacerElement>());
        Assert.Equal(72f, spacer.Height, precision: 1);
    }

    [Fact]
    public void Rectangle_KeepsItsDesignedHeight()
    {
        var rect = $"""
            <Rectangle Name="R"><ReportItems>{Box("x")}</ReportItems>
              <Top>0in</Top><Left>0in</Left><Height>2in</Height><Width>2in</Width></Rectangle>
            """;
        var column = Descendants(Content(Parse(Report(rect)))).OfType<ColumnElement>().Last();

        Assert.Equal(126f, Assert.IsType<SpacerElement>(column.Items[^1]).Height, precision: 1); // 2in - 0.25in
    }

    [Theory]
    [InlineData("=42", "D6", "000042")]
    [InlineData("=255", "X", "FF")]
    [InlineData("=1234.5", "#,0.00", "1.234,50")]
    [InlineData("=1234.5", "C", "$ 1.234,50")]
    public void FormatProperty_SupportsIntegerAndCustomFormats(string value, string format, string expected)
    {
        var factory = Factory();
        Assert.Equal(expected, TextOf(Content(Parse(Report(Box(value, runStyle: $"<Format>{format}</Format>")), factory))));
        Assert.Empty(factory.Warnings);
    }

    [Fact]
    public void InvalidFormat_IsReported_AndTheValueKept()
    {
        var factory = Factory();
        var doc = Parse(Report(Box("=1234.5", runStyle: "<Format>N1000000000</Format>")), factory);

        Assert.Equal("1234,5", TextOf(Content(doc)));
        Assert.Contains(factory.Warnings, w => w.Contains("Format"));
    }

    [Fact]
    public void ExecutionTime_AndReportName_HaveDefaults_ThatGlobalsOverride()
    {
        var path = Path.Combine(Path.GetTempPath(), $"Ventas-{Guid.NewGuid():N}.rdlc");
        File.WriteAllText(path, Report(Box("=Globals!ReportName &amp; \"|\" &amp; Year(Globals!ExecutionTime)")
                                       .Replace("Year(Globals!ExecutionTime)", "Format(Globals!ExecutionTime, \"yyyy\")")));
        try
        {
            var factory = Factory();
            var text = TextOf(Content(factory.ParseFromFile(path)));
            Assert.Equal($"{Path.GetFileNameWithoutExtension(path)}|{DateTime.Now.Year}", text);
            Assert.Empty(factory.Warnings);

            var overridden = new RdlcDocumentFactory(globals: new Dictionary<string, object> { ["ReportName"] = "Custom", ["ExecutionTime"] = new DateTime(2020, 1, 1) });
            Assert.Equal("Custom|2020", TextOf(Content(overridden.ParseFromFile(path))));
        }
        finally
        {
            File.Delete(path);
        }
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
