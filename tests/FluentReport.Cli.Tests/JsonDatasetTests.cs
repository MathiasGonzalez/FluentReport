using System.Text.Json;

namespace FluentReport.Cli.Tests;

public sealed class JsonDatasetTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateTempSubdirectory("fluentreport-cli-");

    public void Dispose() => _dir.Delete(recursive: true);

    private string File(string name, string json)
    {
        var path = Path.Combine(_dir.FullName, name);
        System.IO.File.WriteAllText(path, json);
        return path;
    }

    private static Dictionary<string, IEnumerable<object>> Load(IReadOnlyList<string> declared, params DataArgument[] data)
        => JsonDatasets.Load(data, declared, TextReader.Null);

    private static Dictionary<string, object?> Row(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonDatasets.Row(doc.RootElement);
    }

    [Fact]
    public void Values_AreConverted()
    {
        var row = Row("""{"s": "x", "n": 12.50, "big": 1e400, "t": true, "f": false, "z": null, "o": {"a": 1}, "list": [1, "b"]}""");

        Assert.Equal("x", row["s"]);
        Assert.Equal(12.50m, row["n"]);
        Assert.IsType<double>(row["big"]);
        Assert.Equal(true, row["t"]);
        Assert.Equal(false, row["f"]);
        Assert.Null(row["z"]);
        Assert.Equal(1m, Assert.IsType<Dictionary<string, object?>>(row["o"])["a"]);
        Assert.Equal(new object?[] { 1m, "b" }, Assert.IsType<List<object?>>(row["list"]));
    }

    [Fact]
    public void ARepeatedProperty_KeepsItsLastValue()
        => Assert.Equal(2m, Row("""{"a": 1, "a": 2}""")["a"]);

    [Fact]
    public void ANamedFile_IsOneDataset()
    {
        var datasets = Load([], new DataArgument("Lines", File("l.json", """[{"A": 1}, {"A": 2}]""")));
        Assert.Equal(2, datasets["lines"].Count());
    }

    [Fact]
    public void AnUnnamedObject_HoldsSeveralDatasets()
    {
        var datasets = Load([], new DataArgument(null, File("d.json", """{"Lines": [{"A": 1}], "Taxes": []}""")));
        Assert.Single(datasets["Lines"]);
        Assert.Empty(datasets["Taxes"]);
    }

    [Fact]
    public void AnUnnamedArray_GoesToTheOnlyDataset()
    {
        var datasets = Load(["Products"], new DataArgument(null, File("p.json", """[{"A": 1}]""")));
        Assert.Single(datasets["Products"]);
    }

    [Fact]
    public void AnUnnamedArray_NeedsANameWhenTheReportHasSeveralDatasets()
    {
        var path = File("p.json", "[]");
        var ex = Assert.Throws<CliException>(() => Load(["A", "B"], new DataArgument(null, path)));
        Assert.Contains("several datasets (A, B)", ex.Message);
        Assert.Contains($"--data Name={path}", ex.Message);
    }

    [Fact]
    public void ALaterFile_ReplacesADataset()
    {
        var datasets = Load([],
            new DataArgument("Lines", File("a.json", """[{"A": 1}, {"A": 2}]""")),
            new DataArgument("Lines", File("b.json", """[{"A": 3}]""")));
        Assert.Single(datasets["Lines"]);
    }

    [Fact]
    public void CommentsAndTrailingCommas_AreAccepted()
        => Assert.Single(Load([], new DataArgument("L", File("c.json", "[ // rows\n {\"A\": 1,}, ]")))["L"]);

    [Fact]
    public void StandardInput_IsReadForTheFileDash()
    {
        var datasets = JsonDatasets.Load([new DataArgument("L", "-")], [], new StringReader("""[{"A": 1}]"""));
        Assert.Single(datasets["L"]);
    }

    [Theory]
    [InlineData("""{"A": 1}""", "dataset 'L' must be an array of rows, not an object")]
    [InlineData("""[1]""", "row 1 of 'L' must be an object, not a number")]
    [InlineData("""[{"A": 1}, "x"]""", "row 2 of 'L' must be an object, not a string")]
    [InlineData("""[{"A": 1""", "is not valid JSON")]
    public void BadData_IsReportedWithTheFileAndTheRow(string json, string message)
    {
        var ex = Assert.Throws<CliException>(() => Load([], new DataArgument("L", File("bad.json", json))));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void AnUnnamedScalar_IsRejected()
        => Assert.Contains("expected an array of rows or an object of datasets, not a number",
            Assert.Throws<CliException>(() => Load([], new DataArgument(null, File("n.json", "42")))).Message);

    [Fact]
    public void AMissingFile_IsReported()
        => Assert.Contains("data file not found",
            Assert.Throws<CliException>(() => Load([], new DataArgument("L", Path.Combine(_dir.FullName, "nope.json")))).Message);
}
