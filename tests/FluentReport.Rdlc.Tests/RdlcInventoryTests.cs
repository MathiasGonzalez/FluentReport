using FluentReport.Elements;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rdlc.Tests;

/// <summary>
/// Reports shaped like a typical line-of-business inventory: RDL 2008 and 2010 schemas, detail-only
/// tables over lists of C# objects with string fields, styled textboxes, embedded logos, separator
/// lines, and visibility expressions such as <c>=IIf(Fields!X.Value Is Nothing, True, False)</c>.
/// </summary>
public class RdlcInventoryTests
{
    private const string Ns2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";
    private const string Ns2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

    public sealed class Line
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }   // formatted in C#
        public string? Note { get; set; }
    }

    private static readonly List<object> Lines =
    [
        new Line { Code = "A-1", Description = "  Primera  ", Amount = "1.234,50", Note = "urgente" },
        new Line { Code = "B-2", Description = "Segunda", Amount = "10,00", Note = null },
        new Line { Code = "C-3", Description = "Tercera", Amount = "5,00", Note = "" },
    ];

    private static RdlcDocumentFactory Factory() => new(new Dictionary<string, IEnumerable<object>> { ["dsLines"] = Lines });

    // ── Images ───────────────────────────────────────────────────────────────

    private static byte[] Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>A 200×100 px PNG (150×75 pt at 96 dpi).</summary>
    private static readonly byte[] Logo = Encode(200, 100, SKEncodedImageFormat.Png);

    private static string EmbeddedImages(byte[] png) => $"""
        <EmbeddedImages><EmbeddedImage Name="logo"><MIMEType>image/png</MIMEType>
        <ImageData>{Convert.ToBase64String(png)}</ImageData></EmbeddedImage></EmbeddedImages>
        """;

    private static string ImageItem(string sizing, string width = "2in", string height = "2in", string extra = "") => $"""
        <Image Name="Logo{Guid.NewGuid():N}"><Source>Embedded</Source><Value>logo</Value>{(sizing.Length > 0 ? $"<Sizing>{sizing}</Sizing>" : "")}
          <Top>0in</Top><Left>0in</Left><Height>{height}</Height><Width>{width}</Width>{extra}<Style /></Image>
        """;

    // ── RDLC builders (2008: Body/Page under Report; 2010: ReportSections) ──

    private static string Report2008(string body, string extra = "", string page = "") => $"""
        <Report xmlns="{Ns2008}" xmlns:rd="http://schemas.microsoft.com/SQLServer/reporting/reportdesigner">
          <Body><ReportItems>{body}</ReportItems><Height>3in</Height><Style /></Body>
          <Width>6.5in</Width>
          <Page>{page}<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin><TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin><Style /></Page>
          {DataSets}
          {extra}
          <Language>es-UY</Language>
        </Report>
        """;

    private static string Report2010(string body, string page = "", string extra = "") => $"""
        <Report xmlns="{Ns2010}" xmlns:rd="http://schemas.microsoft.com/SQLServer/reporting/reportdesigner">
          <ReportSections><ReportSection>
            <Body><ReportItems>{body}</ReportItems><Height>3in</Height><Style /></Body>
            <Width>6.5in</Width>
            <Page>{page}<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Style /></Page>
          </ReportSection></ReportSections>
          {DataSets}
          {extra}
        </Report>
        """;

    private const string DataSets = """
        <DataSources><DataSource Name="ds"><ConnectionProperties><DataProvider>System.Data.DataSet</DataProvider><ConnectString>/* Local Connection */</ConnectString></ConnectionProperties></DataSource></DataSources>
        <DataSets><DataSet Name="dsLines"><Query><DataSourceName>ds</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
          <Fields>
            <Field Name="Code"><DataField>Code</DataField><rd:TypeName>System.String</rd:TypeName></Field>
            <Field Name="Descripcion"><DataField>Description</DataField><rd:TypeName>System.String</rd:TypeName></Field>
            <Field Name="Amount"><DataField>Amount</DataField><rd:TypeName>System.String</rd:TypeName></Field>
            <Field Name="Note"><DataField>Note</DataField><rd:TypeName>System.String</rd:TypeName></Field>
            <Field Name="Label"><Value>=Fields!Code.Value &amp; " - " &amp; Trim(Fields!Descripcion.Value)</Value></Field>
          </Fields></DataSet></DataSets>
        """;

    private static string Box(string value, string name, string top = "0in", string left = "0in", string width = "2in",
                              string hidden = "", string style = "", string extra = "") => $"""
        <Textbox Name="{name}"><CanGrow>true</CanGrow><KeepTogether>true</KeepTogether>
          <Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value>
            <Style><FontFamily>Poppins</FontFamily><FontSize>9pt</FontSize></Style></TextRun></TextRuns>
            <Style><TextAlign>Right</TextAlign></Style></Paragraph></Paragraphs>
          <rd:DefaultName>{name}</rd:DefaultName>
          <Top>{top}</Top><Left>{left}</Left><Height>0.25in</Height><Width>{width}</Width>
          {(hidden.Length > 0 ? $"<Visibility><Hidden>{hidden}</Hidden></Visibility>" : "")}{extra}
          <Style><FontFamily>Calibri</FontFamily><Border><Style>Solid</Style><Color>#d9d9d9</Color></Border>
            <PaddingLeft>2pt</PaddingLeft><PaddingRight>2pt</PaddingRight><PaddingTop>2pt</PaddingTop><PaddingBottom>2pt</PaddingBottom>{style}</Style>
        </Textbox>
        """;

    private const string NoteIsNothing = "=IIf(Fields!Note.Value Is Nothing, True, False)";

    /// <summary>Detail-only table: Code, Label, Amount and a Note cell hidden when Note Is Nothing.</summary>
    private static string DetailTable(string rowHidden = "") => $"""
        <Tablix Name="tbLines">
          <TablixBody>
            <TablixColumns>{string.Concat(Enumerable.Repeat("<TablixColumn><Width>1.5in</Width></TablixColumn>", 4))}</TablixColumns>
            <TablixRows>
              <TablixRow><Height>0.25in</Height><TablixCells>
                <TablixCell><CellContents>{Box("Código", "hCode")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("Detalle", "hLabel")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("Monto", "hAmount")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("Nota", "hNote")}</CellContents></TablixCell>
              </TablixCells></TablixRow>
              <TablixRow><Height>0.25in</Height><TablixCells>
                <TablixCell><CellContents>{Box("=Replace(Fields!Code.Value, \"-\", \"/\")", "dCode")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("=Fields!Label.Value", "dLabel", extra: "<ToolTip>=Fields!Descripcion.Value</ToolTip>")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("=Fields!Amount.Value", "dAmount")}</CellContents></TablixCell>
                <TablixCell><CellContents>{Box("=Fields!Note.Value", "dNote", hidden: NoteIsNothing)}</CellContents></TablixCell>
              </TablixCells></TablixRow>
            </TablixRows>
          </TablixBody>
          <TablixColumnHierarchy><TablixMembers><TablixMember /><TablixMember /><TablixMember /><TablixMember /></TablixMembers></TablixColumnHierarchy>
          <TablixRowHierarchy><TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name="Details" />{(rowHidden.Length > 0 ? $"<Visibility><Hidden>{rowHidden}</Hidden></Visibility>" : "")}</TablixMember>
          </TablixMembers></TablixRowHierarchy>
          <DataSetName>dsLines</DataSetName>
          <Top>1in</Top><Left>0in</Left><Height>0.5in</Height><Width>6in</Width><Style />
        </Tablix>
        """;

    // ── Tree helpers ─────────────────────────────────────────────────────────

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

    private static IElement Body(Document doc) => doc.Settings.Pages[0].ContentElement!;

    private static List<string> DataRows(Document doc)
    {
        var table = Descendants(Body(doc)).OfType<TableElement>().Single();
        return table.PartitionIntoRows(table.DataCells).Select(r => string.Join("|", r.Select(c => TextOf(c.Cell.Content)))).ToList();
    }

    // ── Expressions ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("=IIf(Fields!Note.Value Is Nothing, \"none\", \"some\")", "some|none|some")] // "" is not Nothing
    [InlineData("=IIf(Fields!Note.Value IsNot Nothing, \"some\", \"none\")", "some|none|some")]
    [InlineData("=Not Fields!Note.Value Is Nothing", "True|False|True")]
    [InlineData("=IIf(IsNothing(Fields!Note.Value), True, False)", "False|True|False")]
    [InlineData("=Switch(Fields!Code.Value = \"A-1\", \"uno\", Fields!Code.Value = \"B-2\", \"dos\", True, \"otro\")", "uno|dos|otro")]
    [InlineData("=Trim(Fields!Descripcion.Value)", "Primera|Segunda|Tercera")]
    public void InventoryExpressions_EvaluatePerRow(string expression, string expected)
    {
        var table = $"""
            <Tablix Name="t"><TablixBody><TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
              <TablixRows><TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents>{Box(System.Security.SecurityElement.Escape(expression), "v")}</CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
            </TablixBody>
            <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
            <TablixRowHierarchy><TablixMembers><TablixMember><Group Name="Details" /></TablixMember></TablixMembers></TablixRowHierarchy>
            <DataSetName>dsLines</DataSetName></Tablix>
            """;
        var factory = Factory();
        var doc = factory.ParseFromXml(Report2008(table));

        Assert.Equal(expected, string.Join("|", DataRows(doc)));
        Assert.Empty(factory.Warnings);
    }

    [Fact]
    public void FieldNames_ResolveThroughDataField_AndCalculatedFields()
    {
        // "Descripcion" reads the Description property; "Label" is calculated from other fields.
        var doc = Factory().ParseFromXml(Report2008(DetailTable()));
        var rows = DataRows(doc);

        Assert.Equal("A/1|A-1 - Primera|1.234,50|urgente", rows[0]);
        Assert.StartsWith("B/2|B-2 - Segunda|10,00|", rows[1]);
    }

    [Fact]
    public void IsNothing_HidesTheCell_OnlyWhenTheValueIsMissing()
    {
        var doc = Factory().ParseFromXml(Report2008(DetailTable()));
        var table = Descendants(Body(doc)).OfType<TableElement>().Single();
        var noteCells = table.PartitionIntoRows(table.DataCells).Select(r => r[3].Cell.Content).ToList();

        Assert.Equal("urgente", TextOf(noteCells[0]));
        Assert.Empty(Descendants(noteCells[1]).OfType<TextElement>()); // Note is null → hidden
        Assert.Single(Descendants(noteCells[2]).OfType<TextElement>()); // Note is "" → shown (empty)
    }

    [Fact]
    public void HiddenDetailRows_AreDropped()
    {
        var doc = Factory().ParseFromXml(Report2008(DetailTable(rowHidden: "=Fields!Note.Value Is Nothing")));
        Assert.Equal(["A/1", "C/3"], DataRows(doc).Select(r => r.Split('|')[0]).ToList());
    }

    [Fact]
    public void HiddenBodyItems_UseFirstOverTheDataset()
    {
        var hidden = "=IIf(First(Fields!Note.Value, \"dsLines\") Is Nothing, True, False)";
        var doc = Factory().ParseFromXml(Report2008(
            Box("=First(Fields!Code.Value, \"dsLines\")", "first") +
            Box("hidden?", "maybe", top: "0.5in", hidden: System.Security.SecurityElement.Escape(hidden))));

        Assert.Equal("A-1hidden?", TextOf(Body(doc))); // first Note is "urgente" → shown
    }

    // ── Styles, lines, toggles ───────────────────────────────────────────────

    [Fact]
    public void TextboxStyles_AreKept()
    {
        var doc = Factory().ParseFromXml(Report2008(Box("Hola", "t")));

        var text = Descendants(Body(doc)).OfType<TextElement>().Single();
        Assert.Equal("Poppins", text.Spans[0].Style.FontFamily); // run font wins over the textbox's Calibri
        Assert.Equal(9f, text.Spans[0].Style.FontSize);
        Assert.Equal(TextAlignment.Right, text.Spans[0].Style.Alignment);
        Assert.Contains(Descendants(Body(doc)), e => e is PaddingElement { Left: 2, Top: 2 });
        Assert.Contains(Descendants(Body(doc)), e => e is BorderElement { Border.Sides: BorderSide.All, Border.Color.R: 0xD9 });
    }

    [Fact]
    public void SeparatorLines_ToggleItems_AndToolTips_ImportWithoutWarnings()
    {
        var line = """
            <Line Name="sep"><Top>0.5in</Top><Left>0in</Left><Height>0in</Height><Width>6in</Width>
              <Style><Border><Color>#7f7f7f</Color><Style>Solid</Style><Width>0.5pt</Width></Border></Style></Line>
            """;
        var toggled = Box("detalle", "toggled", top: "1in",
            extra: "<ToolTip>Ver detalle</ToolTip>").Replace("<CanGrow>", "<Visibility><Hidden>true</Hidden><ToggleItem>sep</ToggleItem></Visibility><CanGrow>");
        var factory = Factory();
        var doc = factory.ParseFromXml(Report2008(Box("titulo", "title") + line + toggled));

        var lineElement = Descendants(Body(doc)).OfType<LineElement>().Single();
        Assert.Equal(0.5f, lineElement.Thickness);
        Assert.Equal("titulo", TextOf(Body(doc))); // the toggled item starts hidden, as when exported
        Assert.Empty(factory.Warnings);
    }

    // ── Images ───────────────────────────────────────────────────────────────

    private static ImageElement ImportImage(string sizing, string width = "2in", string height = "2in", byte[]? png = null)
    {
        var factory = Factory();
        var doc = factory.ParseFromXml(Report2008(ImageItem(sizing, width, height), extra: EmbeddedImages(png ?? Logo)));
        Assert.Empty(factory.Warnings);
        return Descendants(Body(doc)).OfType<ImageElement>().Single();
    }

    [Fact]
    public void FitProportional_KeepsTheAspectRatioInsideTheBox()
    {
        var image = ImportImage("FitProportional", "2in", "2in"); // 144×144 pt box, 2:1 image
        Assert.Equal(144f, image.FixedWidth!.Value, precision: 1);
        Assert.Equal(72f, image.FixedHeight!.Value, precision: 1);
    }

    [Fact]
    public void Fit_StretchesToTheBox()
    {
        var image = ImportImage("Fit", "2in", "2in");
        Assert.Equal((144f, 144f), (image.FixedWidth!.Value, image.FixedHeight!.Value));
    }

    [Fact]
    public void AutoSize_IsTheDefault_AndUsesTheNaturalSize()
    {
        var image = ImportImage("", "0.5in", "0.5in"); // 200×100 px at 96 dpi
        Assert.Equal(150f, image.FixedWidth!.Value, precision: 1);
        Assert.Equal(75f, image.FixedHeight!.Value, precision: 1);
    }

    [Fact]
    public void Clip_ScalesDownToTheBox_KeepingTheRatio()
    {
        var image = ImportImage("Clip", "1in", "2in");
        Assert.Equal(72f, image.FixedWidth!.Value, precision: 1);
        Assert.Equal(36f, image.FixedHeight!.Value, precision: 1);
    }

    [Fact]
    public void NaturalSize_HonorsThePngResolution()
    {
        // Insert a pHYs chunk of 192 dpi (7559 px/m) after IHDR: the image is half as large.
        var phys = new byte[21];
        phys[3] = 9;
        "pHYs"u8.CopyTo(phys.AsSpan(4));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(phys.AsSpan(8), 7559);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(phys.AsSpan(12), 7559);
        phys[16] = 1;
        var png = Logo[..33].Concat(phys).Concat(Logo[33..]).ToArray();

        var size = ImageDimensions.SizeInPoints(png)!.Value;
        Assert.Equal(75f, size.Width, precision: 0);
        Assert.Equal(37.5f, size.Height, precision: 0);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    public void NaturalSize_IsReadFromTheHeader(SKEncodedImageFormat format)
    {
        var size = ImageDimensions.SizeInPoints(Encode(320, 160, format))!.Value;
        Assert.Equal((240f, 120f), (size.Width, size.Height));
    }

    [Fact]
    public void UnreadableImageSize_FillsTheBox()
    {
        Assert.Null(ImageDimensions.SizeInPoints([0x89, 0x50, 0x4E, 0x47]));
        Assert.Null(ImageDimensions.SizeInPoints("not an image"u8.ToArray()));
    }

    // ── Schemas and end to end ───────────────────────────────────────────────

    [Fact]
    public void Schema2010_WithPageHeaderLogo_RendersToPdf()
    {
        var header = $"""
            <PageHeader><Height>1in</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage>
              <ReportItems>{ImageItem("FitProportional", "1.5in", "0.75in")}{Box("Estado de cuenta", "title", left: "3in", width: "3in")}</ReportItems>
              <Style /></PageHeader>
            """;
        var factory = Factory();
        var doc = factory.ParseFromXml(Report2010(DetailTable(), page: header, extra: EmbeddedImages(Logo)));

        var logo = Descendants(doc.Settings.Pages[0].HeaderElement).OfType<ImageElement>().Single();
        Assert.Equal(108f, logo.FixedWidth!.Value, precision: 1);
        Assert.Equal(54f, logo.FixedHeight!.Value, precision: 1);
        Assert.Contains("Estado de cuenta", TextOf(doc.Settings.Pages[0].HeaderElement));
        Assert.Equal(3, DataRows(doc).Count);

        var pdf = doc.GeneratePdf();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Empty(factory.Warnings);
    }

    [Fact]
    public void Schema2008_WithLogoTableAndLines_RendersToPdf()
    {
        var factory = Factory();
        var body = ImageItem("FitProportional", "1.5in", "0.75in")
                   + """<Line Name="sep"><Top>0.8in</Top><Left>0in</Left><Height>0in</Height><Width>6in</Width><Style><Border><Style>Solid</Style></Border></Style></Line>"""
                   + DetailTable();
        var doc = factory.ParseFromXml(Report2008(body, extra: EmbeddedImages(Logo)));

        Assert.Equal(1, doc.GetPdfPageCount());
        Assert.Empty(factory.Warnings);
    }
}
