using FluentReport.Schema;

namespace FluentReport.Schema.Tests;

public class SchemaCodeGeneratorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string Gen(string yaml) => SchemaCodeGenerator.GenerateCSharp(yaml);

    // ─────────────────────────────────────────────────────────────────────────
    // Basic structure
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_MinimalSchema_ContainsDocumentCreate()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Hello
            """;

        var code = Gen(yaml);

        Assert.Contains("Document.Create(container =>", code);
        Assert.Contains("container.Page(page =>", code);
        Assert.Contains("page.Size(PageSizes.A4)", code);
        Assert.Contains("page.MarginAll(40)", code);
        Assert.Contains(".Text(\"Hello\")", code);
    }

    [Fact]
    public void GenerateCSharp_JsonInput_ProducesEquivalentOutput()
    {
        const string json = """
            {
              "kind": "FluentReport",
              "schemaVersion": 1,
              "pages": [
                {
                  "regions": {
                    "content": {
                      "nodes": [
                        { "type": "text", "value": "JSON Test" }
                      ]
                    }
                  }
                }
              ]
            }
            """;

        var code = SchemaCodeGenerator.GenerateCSharp(json, isJson: true);

        Assert.Contains("Document.Create(container =>", code);
        Assert.Contains(".Text(\"JSON Test\")", code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Page settings
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_LandscapeA3_EmitsCorrectSizeCall()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pageDefaults:
              size: A3
              orientation: landscape
              margin:
                top: 20
                right: 30
                bottom: 20
                left: 30
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Hi
            """;

        var code = Gen(yaml);

        Assert.Contains("PageSizes.A3.Landscape()", code);
        Assert.DoesNotContain("PageSizes.A4", code);
    }

    [Fact]
    public void GenerateCSharp_AsymmetricMargins_EmitsMarginWithParams()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pageDefaults:
              margin:
                top: 10
                right: 20
                bottom: 30
                left: 40
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Hi
            """;

        var code = Gen(yaml);

        Assert.Contains("page.Margin(", code);
        Assert.Contains("top:", code);
        Assert.DoesNotContain("page.MarginAll(", code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Text node
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_TextWithStyleRef_InlinesStyleProperties()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            styles:
              heading:
                fontSize: 16
                bold: true
                align: center
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Title
                        styleRef: heading
            """;

        var code = Gen(yaml);

        Assert.Contains(".Text(\"Title\")", code);
        Assert.Contains(".FontSize(16)", code);
        Assert.Contains(".Bold()", code);
        Assert.Contains(".AlignCenter()", code);
    }

    [Fact]
    public void GenerateCSharp_TextWithPageNumberRuns_EmitsDynamicTextBuilder()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  footer:
                    nodes:
                      - type: text
                        runs:
                          - value: "Page "
                          - token: currentPage
                          - value: " of "
                          - token: totalPages
            """;

        var code = Gen(yaml);

        Assert.Contains(".Text(x =>", code);
        Assert.Contains("x.Span(\"Page \")", code);
        Assert.Contains("x.CurrentPageNumber()", code);
        Assert.Contains("x.Span(\" of \")", code);
        Assert.Contains("x.TotalPages()", code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Table node
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_TableWithCellBorder_EmitsBorderEachCellInsideBlock()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: table
                        dataSource: rows
                        cellBorderWidth: 0.5
                        cellBorderColor: "#DDDDDD"
                        columns:
                          - field: name
                            header: Name
                            width: 2
                          - field: value
                            header: Value
                            width: 1
                            align: right
            """;

        var code = Gen(yaml);

        // BorderEachCell must be inside the table configure block
        Assert.Contains("table.BorderEachCell(0.5, \"#DDDDDD\");", code);

        // Must NOT be chained on ContainerBuilder after the closing paren
        Assert.DoesNotContain("}).BorderEachCell", code);

        // Column definitions and header
        Assert.Contains("cols.RelativeColumn(2)", code);
        Assert.Contains("cols.RelativeColumn(1)", code);
        Assert.Contains("h.Cell().Text(\"Name\")", code);

        // Data-row stub
        Assert.Contains("// foreach (var row in rows)", code);
        Assert.Contains(".AlignRight()", code);
    }

    [Fact]
    public void GenerateCSharp_Table_WithoutCellBorder_DoesNotEmitBorderEachCell()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: table
                        dataSource: rows
                        columns:
                          - field: col
                            header: Col
                            width: 1
            """;

        var code = Gen(yaml);

        Assert.DoesNotContain("BorderEachCell", code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Image node
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_ImagePathMode_EmitsImageWithPathLiteral()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: image
                        source:
                          mode: path
                          value: "logo.png"
                        fit: fitwidth
            """;

        var code = Gen(yaml);

        // Should call .Image("logo.png") — not File.ReadAllBytes
        Assert.Contains(".Image(\"logo.png\")", code);
        Assert.DoesNotContain("File.ReadAllBytes", code);
        Assert.DoesNotContain("ImageScaling", code);
        // Fit is emitted as a comment
        Assert.Contains("fit: fitwidth", code);
    }

    [Fact]
    public void GenerateCSharp_ImageBase64Mode_EmitsConvertFromBase64()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: image
                        source:
                          mode: base64
                          value: "AAAA"
            """;

        var code = Gen(yaml);

        Assert.Contains(".Image(Convert.FromBase64String(\"AAAA\"))", code);
        Assert.DoesNotContain("ImageScaling", code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Container decorators
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_ContainerDecorators_EmittedBeforeContentCall()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Hi
                        padding: 5
                        background: "#F0F0F0"
                        borderWidth: 1
                        borderColor: "#333333"
            """;

        var code = Gen(yaml);

        Assert.Contains(".Padding(5)", code);
        Assert.Contains(".Background(\"#F0F0F0\")", code);
        Assert.Contains(".Border(1, \"#333333\")", code);

        // Decorators must come before .Text(...)
        var paddingIdx   = code.IndexOf(".Padding(5)", StringComparison.Ordinal);
        var textIdx      = code.IndexOf(".Text(\"Hi\")", StringComparison.Ordinal);
        Assert.True(paddingIdx < textIdx, "Padding decorator should appear before .Text()");
    }

    [Fact]
    public void GenerateCSharp_UniformPadding_EmitsSinglePaddingCall()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Hi
                        padding: 8
            """;

        var code = Gen(yaml);

        Assert.Contains(".Padding(8)", code);
        Assert.DoesNotContain(".PaddingTop(", code);
    }

    [Fact]
    public void GenerateCSharp_NonTextNodeAlignment_EmittedAsContainerDecorator()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - regions:
                  content:
                    nodes:
                      - type: line
                        thickness: 1
                        align: center
            """;

        var code = Gen(yaml);

        // For non-text nodes, alignment is a container decorator (before the content call)
        Assert.Contains(".AlignCenter()", code);

        var alignIdx = code.IndexOf(".AlignCenter()", StringComparison.Ordinal);
        var lineIdx  = code.IndexOf(".Line(1,", StringComparison.Ordinal);
        Assert.True(alignIdx < lineIdx, "AlignCenter() should appear before .Line()");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parameters and data source preamble
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCSharp_ParametersAndDataSources_EmittedAsComments()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            parameters:
              companyName:
                type: string
                required: true
              period:
                type: string
            dataSources:
              sales:
                type: array
            pages:
              - regions:
                  content:
                    nodes:
                      - type: text
                        value: Report
            """;

        var code = Gen(yaml);

        Assert.Contains("// - companyName (string, required)", code);
        Assert.Contains("// - period (string)", code);
        Assert.Contains("// - sales:", code);
    }
}
