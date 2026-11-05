using System.Diagnostics;
using System.Xml;
using FluentReport.Elements;
using FluentReport.Html;

namespace FluentReport.Rdlc.Tests;

/// <summary>
/// Hostile report definitions: each must fail with a catchable exception or a warning, never by
/// reading files, hanging the render or overflowing the stack (which ends the process).
/// </summary>
public class RdlcSecurityTests
{
    private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

    private static string Report(string items, string dtd = "", string dataSets = "") => $"""
        {dtd}<Report xmlns="{Ns}">{dataSets}<ReportSections><ReportSection>
          <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
          <Body><ReportItems>{items}</ReportItems></Body>
        </ReportSection></ReportSections></Report>
        """;

    private static string Textbox(string value) => $"<Textbox Name=\"T\"><Value>{value}</Value></Textbox>";

    private static string Text(Document doc) => doc.GenerateHtml();

    // ── XML ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ExternalEntity_IsRejected_AndTheFileIsNotRead()
    {
        var secret = Path.Combine(Path.GetTempPath(), $"secret-{Guid.NewGuid():N}.txt");
        File.WriteAllText(secret, "TOP-SECRET");
        try
        {
            var xml = Report(Textbox("&x;"), dtd: $"<!DOCTYPE Report [<!ENTITY x SYSTEM \"{new Uri(secret)}\">]>");

            Assert.Throws<XmlException>(() => DocumentRdlcExtensions.FromRdlcXml(xml));
            Assert.Throws<XmlException>(() => DocumentRdlcExtensions.FromRdlcStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml))));
        }
        finally { File.Delete(secret); }
    }

    [Fact]
    public void EntityExpansion_IsRejected()
    {
        var dtd = "<!DOCTYPE Report [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\"><!ENTITY c \"&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;\">]>";

        Assert.Throws<XmlException>(() => DocumentRdlcExtensions.FromRdlcXml(Report(Textbox("&c;"), dtd)));
    }

    [Fact]
    public void DeeplyNestedXml_IsRejected()
    {
        const int levels = 5_000;
        var open = string.Concat(Enumerable.Range(0, levels).Select(i => $"<Rectangle Name=\"R{i}\"><ReportItems>"));
        var close = string.Concat(Enumerable.Repeat("</ReportItems></Rectangle>", levels));

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentRdlcExtensions.FromRdlcXml(Report(open + Textbox("x") + close)));
        Assert.Contains("nested", ex.Message);
    }

    [Fact]
    public void NestedRectangles_WithinTheLimit_StillRender()
    {
        const int levels = 100;
        var open = string.Concat(Enumerable.Range(0, levels).Select(i => $"<Rectangle Name=\"R{i}\"><ReportItems>"));
        var close = string.Concat(Enumerable.Repeat("</ReportItems></Rectangle>", levels));

        var doc = DocumentRdlcExtensions.FromRdlcXml(Report(open + Textbox("deep") + close));

        Assert.Contains("deep", Text(doc));
        Assert.True(doc.GeneratePdf().Length > 0);
    }

    // ── Expressions ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(100_000, "(", ")")]
    [InlineData(100_000, "Not (", ")")]
    [InlineData(100_000, "-", "")]
    [InlineData(100_000, "IIf(True, ", ", 0)")]
    public void DeeplyNestedExpression_IsReported_InsteadOfCrashing(int levels, string open, string close)
    {
        var expression = "=" + string.Concat(Enumerable.Repeat(open, levels)) + "1" + string.Concat(Enumerable.Repeat(close, levels));
        var factory = new RdlcDocumentFactory();

        var doc = factory.ParseFromXml(Report(Textbox(expression)));

        Assert.Contains(factory.Warnings, w => w.Contains("nested more than"));
        Assert.Throws<NotSupportedException>(() =>
            new RdlcDocumentFactory(options: new RdlcImportOptions { StrictExpressions = true }).ParseFromXml(Report(Textbox(expression))));
        Assert.NotNull(doc);
    }

    [Fact]
    public void LongOperatorChain_IsReported_InsteadOfCrashing()
    {
        var factory = new RdlcDocumentFactory();

        factory.ParseFromXml(Report(Textbox("=1" + string.Concat(Enumerable.Repeat(" + 1", 200_000)))));

        Assert.Contains(factory.Warnings, w => w.Contains("chained operations"));
    }

    [Fact]
    public void RealisticExpressions_AreWithinTheLimits()
    {
        var nested = "=" + string.Concat(Enumerable.Repeat("(", 50)) + "1" + string.Concat(Enumerable.Repeat(")", 50));
        var chain = "=1" + string.Concat(Enumerable.Repeat(" + 1", 400));
        var factory = new RdlcDocumentFactory();

        var html = Text(factory.ParseFromXml(Report(Textbox(nested) + Textbox(chain).Replace("\"T\"", "\"T2\""))));

        Assert.Empty(factory.Warnings);
        Assert.Contains(">1<", html);
        Assert.Contains(">401<", html);
    }

    // ── Filters ──────────────────────────────────────────────────────────────

    [Fact]
    public void LikeFilter_WithManyWildcards_RunsInLinearTime()
    {
        var dataSets = """
            <DataSets><DataSet Name="DS"><Fields><Field Name="A"><DataField>A</DataField></Field></Fields>
              <Filters><Filter><FilterExpression>=Fields!A.Value</FilterExpression><Operator>Like</Operator>
                <FilterValues><FilterValue>*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*a*b</FilterValue></FilterValues></Filter></Filters>
            </DataSet></DataSets>
            """;
        var datasets = new Dictionary<string, IEnumerable<object>>
        {
            ["DS"] = [new Dictionary<string, object> { ["A"] = new string('a', 5_000) + "c" }]
        };

        var watch = Stopwatch.StartNew();
        var doc = DocumentRdlcExtensions.FromRdlcXml(Report(Textbox("=Count(Fields!A.Value, \"DS\")"), dataSets: dataSets), datasets);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Like took {watch.Elapsed}");
        Assert.Contains(">0<", Text(doc));
    }

    // ── External images ──────────────────────────────────────────────────────

    [Fact]
    public void ExternalImage_ThroughASymbolicLink_IsNotLoaded()
    {
        var dir = Directory.CreateTempSubdirectory("fr-images-");
        var outside = Directory.CreateTempSubdirectory("fr-outside-");
        try
        {
            File.WriteAllBytes(Path.Combine(outside.FullName, "secret.png"), Png);
            try
            {
                File.CreateSymbolicLink(Path.Combine(dir.FullName, "link.png"), Path.Combine(outside.FullName, "secret.png"));
                Directory.CreateSymbolicLink(Path.Combine(dir.FullName, "linkdir"), outside.FullName);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException)
            {
                return; // creating links needs privileges on some Windows machines
            }

            var factory = new RdlcDocumentFactory(options: new RdlcImportOptions { ExternalImageDirectory = dir.FullName });
            var doc = factory.ParseFromXml(Report(Image("link.png") + Image("linkdir/secret.png", "I2")));

            Assert.All(Images(doc), image => Assert.Empty(image.SourceBytes));
            Assert.Equal(2, factory.Warnings.Count(w => w.Contains("symbolic link")));
        }
        finally
        {
            dir.Delete(recursive: true);
            outside.Delete(recursive: true);
        }
    }

    private static string Image(string path, string name = "I") =>
        $"<Image Name=\"{name}\"><Source>External</Source><Value>{path}</Value><Height>1in</Height><Width>1in</Width></Image>";

    private static List<ImageElement> Images(Document doc)
    {
        var found = new List<ImageElement>();
        var pending = new Stack<IElement>([doc.Settings.Pages[0].ContentElement!]);
        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case ImageElement image: found.Add(image); break;
                case ColumnElement column: foreach (var item in column.Items) pending.Push(item); break;
                case PaddingElement { Child: { } child }: pending.Push(child); break;
                case BorderElement { Child: { } child }: pending.Push(child); break;
                case AlignElement { Child: { } child }: pending.Push(child); break;
            }
        }
        return found;
    }

    // 1x1 transparent PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
}
