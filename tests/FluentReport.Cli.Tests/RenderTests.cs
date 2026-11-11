using System.Text;

namespace FluentReport.Cli.Tests;

/// <summary>The whole command, as the tool runs it: arguments in, files and exit code out.</summary>
public sealed class RenderTests : IDisposable
{
    private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

    private static string Report(string dataSets = "") => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="{Ns}">
          {dataSets}
          <ReportSections><ReportSection>
            <Page><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight></Page>
            <Body><ReportItems>
              <Textbox Name="Title"><Value>=Parameters!Title.Value</Value></Textbox>
              <Tablix Name="Table1">
                <DataSetName>Products</DataSetName>
                <TablixBody>
                  <TablixColumns>
                    <TablixColumn><Width>2in</Width></TablixColumn>
                    <TablixColumn><Width>1.5in</Width></TablixColumn>
                  </TablixColumns>
                  <TablixRows>
                    <TablixRow><Height>0.25in</Height><TablixCells>
                      <TablixCell><CellContents><Textbox Name="D1"><Value>=Fields!Name.Value</Value></Textbox></CellContents></TablixCell>
                      <TablixCell><CellContents><Textbox Name="D2"><Value>=Format(Fields!Price.Value, "N2")</Value></Textbox></CellContents></TablixCell>
                    </TablixCells></TablixRow>
                  </TablixRows>
                </TablixBody>
                <TablixRowHierarchy><TablixMembers><TablixMember><Group Name="Details" /></TablixMember></TablixMembers></TablixRowHierarchy>
              </Tablix>
            </ReportItems></Body>
          </ReportSection></ReportSections>
        </Report>
        """;

    private const string Products = """[{"Name": "Widget", "Price": 1234.5}, {"name": "Gadget", "price": 7}]""";

    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("fluentreport-cli-");
    private readonly StringWriter _out = new(), _err = new();

    public void Dispose() => _dir.Delete(recursive: true);

    private string P(string name) => Path.Combine(_dir.FullName, name);

    private string Write(string name, string content)
    {
        File.WriteAllText(P(name), content);
        return P(name);
    }

    private int Run(params string[] args) => CliApp.Run(args, TextReader.Null, _out, _err);

    [Fact]
    public void RendersAPdf()
    {
        var report = Write("r.rdlc", Report());
        var data = Write("p.json", Products);

        Assert.Equal(CliApp.Success, Run("render", report, "-d", data, "-p", "Title=T", "-o", P("out.pdf")));

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(File.ReadAllBytes(P("out.pdf")), 0, 4));
        Assert.Equal(P("out.pdf"), _out.ToString().Trim());
        Assert.Equal("", _err.ToString());
    }

    [Fact]
    public void RendersHtml_WithTheDataTheParametersAndTheCulture()
    {
        var report = Write("r.rdlc", Report());
        var data = Write("p.json", Products);

        Assert.Equal(CliApp.Success, Run("render", report, "-d", $"Products={data}", "-p", "Title=Price list",
            "--culture", "es-UY", "-o", P("out.html")));

        var html = File.ReadAllText(P("out.html"));
        Assert.Contains("Price list", html);
        Assert.Contains("Widget", html);
        Assert.Contains("1.234,50", html); // es-UY number format
        Assert.Contains("Gadget", html);    // field names match without regard to case
    }

    [Fact]
    public void RendersExcel()
    {
        var report = Write("r.rdlc", Report());
        Assert.Equal(CliApp.Success, Run("render", report, "-d", Write("p.json", Products), "-o", P("out.xlsx")));
        Assert.Equal("PK", Encoding.ASCII.GetString(File.ReadAllBytes(P("out.xlsx")), 0, 2));
    }

    [Fact]
    public void RendersPng_IntoANewDirectory()
    {
        var report = Write("r.rdlc", Report());
        var output = Path.Combine(_dir.FullName, "images", "out.png");

        Assert.Equal(CliApp.Success, Run("render", report, "-d", Write("p.json", Products), "-o", output, "--scale", "1"));

        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, File.ReadAllBytes(output)[..4]);
    }

    [Fact]
    public void ReadsDataFromStandardInput()
    {
        var report = Write("r.rdlc", Report());
        int code = CliApp.Run(["render", report, "-d", "-", "-o", P("out.html")], new StringReader(Products), _out, _err);

        Assert.Equal(CliApp.Success, code);
        Assert.Contains("Widget", File.ReadAllText(P("out.html")));
    }

    [Fact]
    public void ADatasetTheReportDoesNotHave_IsAWarning()
    {
        var report = Write("r.rdlc", Report());
        Assert.Equal(CliApp.Success, Run("render", report, "-d", $"Prodcts={Write("p.json", Products)}", "-o", P("out.pdf")));
        Assert.Contains("warning: the report has no dataset named 'Prodcts' (it has Products)", _err.ToString());
    }

    [Fact]
    public void ImportWarnings_GoToStandardError()
    {
        var report = Write("r.rdlc", Report().Replace("=Parameters!Title.Value", "=NoSuchFunction(1)"));
        Assert.Equal(CliApp.Success, Run("render", report, "-o", P("out.pdf")));
        Assert.Contains("warning:", _err.ToString());
        Assert.Contains("NoSuchFunction", _err.ToString());
    }

    [Fact]
    public void Strict_TurnsAnExpressionThatCannotBeEvaluatedIntoAFailure()
    {
        var report = Write("r.rdlc", Report().Replace("=Parameters!Title.Value", "=NoSuchFunction(1)"));
        Assert.Equal(CliApp.Failure, Run("render", report, "--strict", "-o", P("out.pdf")));
        Assert.StartsWith("error:", _err.ToString());
        Assert.DoesNotContain("   at ", _err.ToString()); // a message, not a stack trace
        Assert.False(File.Exists(P("out.pdf")));
    }

    [Fact]
    public void DatasetNames_ComeFromTheDeclarationsAndTheDataRegions()
    {
        var report = Write("r.rdlc", Report("""<DataSets><DataSet Name="Extra" /><DataSet Name="products" /></DataSets>"""));
        Assert.Equal(["Extra", "products"], CliApp.DeclaredDatasets(report));
    }

    [Fact]
    public void AMissingReport_Fails()
    {
        Assert.Equal(CliApp.Failure, Run("render", P("nope.rdlc")));
        Assert.Contains("report not found", _err.ToString());
    }

    [Fact]
    public void AnInvalidReport_Fails()
    {
        var report = Write("r.rdlc", "<Report");
        Assert.Equal(CliApp.Failure, Run("render", report, "-o", P("out.pdf")));
        Assert.StartsWith("error:", _err.ToString());
    }

    [Fact]
    public void AReportWithADtd_IsRejected()
    {
        var report = Write("r.rdlc", "<!DOCTYPE r [<!ENTITY x \"y\">]>" + Report().Replace("""<?xml version="1.0" encoding="utf-8"?>""", ""));
        Assert.Equal(CliApp.Failure, Run("render", report, "-o", P("out.pdf")));
    }

    [Fact]
    public void BadArguments_ExitWithTwo_AndPointToTheHelp()
    {
        Assert.Equal(CliApp.BadUsage, Run("render"));
        Assert.Contains("fluentreport --help", _err.ToString());
    }

    [Fact]
    public void AnUnknownCommand_ExitsWithTwo()
        => Assert.Equal(CliApp.BadUsage, Run("convert", "x.rdlc"));

    [Fact]
    public void NoArguments_PrintTheUsageToStandardError()
    {
        Assert.Equal(CliApp.BadUsage, Run());
        Assert.Contains("Usage:", _err.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("help")]
    [InlineData("render", "--help")]
    public void Help_PrintsTheUsage(params string[] args)
    {
        Assert.Equal(CliApp.Success, Run(args));
        Assert.Contains("fluentreport render <report.rdlc>", _out.ToString());
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("version")] // dnx takes --version for itself
    public void Version_IsTheShortVersion(string command)
    {
        Assert.Equal(CliApp.Success, Run(command));
        var version = _out.ToString().Trim();
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
        Assert.DoesNotContain("+", version);
    }
}
