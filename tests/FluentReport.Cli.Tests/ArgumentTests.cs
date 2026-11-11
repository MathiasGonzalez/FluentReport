using System.Globalization;

namespace FluentReport.Cli.Tests;

public class ArgumentTests
{
    private static RenderOptions Parse(params string[] args) => CliApp.ParseRender(args)!;

    [Theory]
    [InlineData("out.pdf", "Pdf")]
    [InlineData("out.PNG", "Png")]
    [InlineData("out.html", "Html")]
    [InlineData("out.htm", "Html")]
    [InlineData("out.xlsx", "Excel")]
    public void TheOutputExtension_PicksTheFormat(string output, string format)
        => Assert.Equal(Enum.Parse<OutputFormat>(format), Parse("r.rdlc", "-o", output).Format);

    [Fact]
    public void WithoutOutput_ItIsAPdfNamedAfterTheReport_InTheCurrentDirectory()
    {
        var options = Parse(Path.Combine("reports", "invoice.rdlc"));
        Assert.Equal(OutputFormat.Pdf, options.Format);
        Assert.Equal("invoice.pdf", options.Output);
    }

    [Fact]
    public void Format_NamesTheDefaultOutput_AndOverridesTheExtension()
    {
        Assert.Equal("invoice.xlsx", Parse("invoice.rdlc", "--format", "xlsx").Output);
        Assert.Equal(OutputFormat.Html, Parse("invoice.rdlc", "-o", "invoice.txt", "-f", "html").Format);
    }

    [Fact]
    public void Options_TakeTheirValueInline_OrAsTheNextArgument()
    {
        var options = Parse("r.rdlc", "--output=a.png", "--scale=3", "--culture", "es-UY", "--strict", "--no-external-images");
        Assert.Equal("a.png", options.Output);
        Assert.Equal(3f, options.Scale);
        Assert.Equal(CultureInfo.GetCultureInfo("es-UY"), options.Culture);
        Assert.True(options.Strict);
        Assert.False(options.ExternalImages);
    }

    [Fact]
    public void Defaults()
    {
        var options = Parse("r.rdlc");
        Assert.Null(options.Culture);
        Assert.False(options.Strict);
        Assert.True(options.ExternalImages);
        Assert.Equal(2f, options.Scale);
        Assert.Empty(options.Data);
        Assert.Empty(options.Parameters);
    }

    [Fact]
    public void Params_AreNameValuePairs_AndTheValueMayHoldEquals()
    {
        var options = Parse("r.rdlc", "-p", "Title=A=B", "--param", "Empty=");
        Assert.Equal("A=B", options.Parameters["title"]);
        Assert.Equal("", options.Parameters["Empty"]);
    }

    [Theory]
    [InlineData("Lines=lines.json", "Lines", "lines.json")]
    [InlineData("data.json", null, "data.json")]
    [InlineData("-", null, "-")]
    [InlineData("Lines=-", "Lines", "-")]
    [InlineData(@"C:\data\a=b.json", null, @"C:\data\a=b.json")] // a drive letter is not a dataset name
    [InlineData("./a=b.json", null, "./a=b.json")]
    public void Data_IsNamedOnlyWhenThePrefixIsAName(string value, string? name, string path)
        => Assert.Equal(new DataArgument(name, path), CliApp.ParseData(value));

    [Fact]
    public void Data_CanBeRepeated()
        => Assert.Equal(2, Parse("r.rdlc", "-d", "A=a.json", "--data", "b.json").Data.Count);

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_ReturnsNoOptions(string flag)
        => Assert.Null(CliApp.ParseRender(["r.rdlc", flag]));

    [Theory]
    [InlineData(new string[0], "missing the report")]
    [InlineData(new[] { "a.rdlc", "b.rdlc" }, "unexpected argument 'b.rdlc'")]
    [InlineData(new[] { "r.rdlc", "--bogus" }, "unknown option '--bogus'")]
    [InlineData(new[] { "r.rdlc", "-o" }, "-o needs a value")]
    [InlineData(new[] { "r.rdlc", "-o", "r.docx" }, "cannot tell the format")]
    [InlineData(new[] { "r.rdlc", "-f", "docx" }, "unknown format 'docx'")]
    [InlineData(new[] { "r.rdlc", "--scale", "0" }, "--scale must be")]
    [InlineData(new[] { "r.rdlc", "--scale", "NaN" }, "--scale must be")]
    [InlineData(new[] { "r.rdlc", "--culture", "xx-NOPE" }, "unknown culture 'xx-NOPE'")]
    [InlineData(new[] { "r.rdlc", "-p", "novalue" }, "expects Name=value")]
    [InlineData(new[] { "r.rdlc", "--strict=yes" }, "--strict takes no value")]
    [InlineData(new[] { "r.rdlc", "-d", "Lines=" }, "missing the file")]
    public void BadArguments_AreUsageErrors(string[] args, string message)
        => Assert.Contains(message, Assert.Throws<UsageException>(() => CliApp.ParseRender(args)).Message);
}
