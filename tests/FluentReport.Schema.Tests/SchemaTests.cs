using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Schema;
using FluentReport.Styling;
using System.Text;

namespace FluentReport.Schema.Tests;

public class SchemaTests
{
    [Fact]
    public void FromSchemaYaml_MinimalDocument_BuildsExpectedPageSettings()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pageDefaults:
              size: Letter
              orientation: landscape
              margin:
                top: 10
                right: 20
                bottom: 30
                left: 40
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        value: "Hello {{ parameters.company }}"
            """;

        var doc = DocumentSchemaExtensions.FromSchemaYaml(
            yaml,
            parameters: new Dictionary<string, object> { ["company"] = "Acme" });

        var page = Assert.Single(doc.Settings.Pages);
        Assert.Equal(PageSizes.Letter.Height, page.Size.Width, 2);
        Assert.Equal(PageSizes.Letter.Width, page.Size.Height, 2);
        Assert.Equal(10, page.MarginTop, 2);
        Assert.Equal(20, page.MarginRight, 2);
        Assert.Equal(30, page.MarginBottom, 2);
        Assert.Equal(40, page.MarginLeft, 2);

        var content = Assert.IsType<TextElement>(page.ContentElement);
        Assert.Single(content.Spans);
        Assert.Equal("Hello Acme", content.Spans[0].StaticText);
    }

    [Fact]
    public void FromSchemaYaml_TableAndRepeat_WithDataSource_ProducesValidPdf()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            definitions:
              repeatables:
                - id: repeat-sales
                  type: repeat
                  dataSource: sales
                  itemTemplate: "- {{ row.region }}"
                  itemGap: 4
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: table-1
                        type: table
                        dataSource: sales
                        columns:
                          - field: region
                            header: Region
                            width: 2
                          - field: revenue
                            header: Revenue
                            width: 1
                            align: right
                      - id: pb
                        type: pageBreak
                      - id: rep-1
                        type: repeat
                        definitionRef: repeat-sales
            """;

        var dataSources = new Dictionary<string, IEnumerable<object>>
        {
            ["sales"] =
            [
                new Dictionary<string, object> { ["region"] = "North", ["revenue"] = 1200m },
                new Dictionary<string, object> { ["region"] = "South", ["revenue"] = 980m }
            ]
        };

        var doc = DocumentSchemaExtensions.FromSchemaYaml(yaml, dataSources);
        var pdf = doc.GeneratePdf();

        Assert.NotEmpty(pdf);
        Assert.Equal((byte)'%', pdf[0]);

        var page = Assert.Single(doc.Settings.Pages);
        var col = Assert.IsType<ColumnElement>(page.ContentElement);
        Assert.Contains(col.Items, i => i is TableElement);
        Assert.Contains(col.Items, i => i is PageBreakElement);
        Assert.Contains(col.Items, i => i is ListElement);
    }

    [Fact]
    public void FromSchemaJson_GroupInstance_ExpandsGroupDefinition()
    {
        const string json = """
            {
              "kind": "FluentReport",
              "schemaVersion": 1,
              "definitions": {
                "groups": [
                  {
                    "id": "g-1",
                    "nodes": [
                      { "id": "txt", "type": "text", "value": "Grouped" }
                    ]
                  }
                ]
              },
              "pages": [
                {
                  "id": "p1",
                  "regions": {
                    "content": {
                      "nodes": [
                        { "id": "gi", "type": "groupInstance", "groupRef": "g-1" }
                      ]
                    }
                  }
                }
              ]
            }
            """;

        var doc = DocumentSchemaExtensions.FromSchemaJson(json);

        var page = Assert.Single(doc.Settings.Pages);
        var text = Assert.IsType<TextElement>(page.ContentElement);
        Assert.Equal("Grouped", text.Spans[0].StaticText);
    }

    [Fact]
    public void FromSchemaYaml_WithoutPages_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages: []
            """;

        Assert.Throws<ArgumentException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
    }

    [Fact]
    public void FromSchemaYaml_UnknownStyleRef_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        styleRef: missing-style
                        value: "Hello"
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("missing-style", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_MissingDataSource_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: table-1
                        type: table
                        dataSource: sales
                        columns:
                          - field: region
                            header: Region
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("sales", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_MissingRepeatDefinition_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: rep-1
                        type: repeat
                        definitionRef: missing-repeat
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("missing-repeat", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_MissingGroupDefinition_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: gi
                        type: groupInstance
                        groupRef: missing-group
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("missing-group", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_UnknownNodeType_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: x1
                        type: unsupportedType
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("unsupportedType", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_UnsupportedSchemaVersion_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 2
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        value: "Hello"
            """;

        Assert.Throws<NotSupportedException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
    }

    [Fact]
    public void FromSchemaYaml_InvalidImageBase64_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: img1
                        type: image
                        source:
                          mode: base64
                          value: not-a-valid-base64
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("base64", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromSchemaYaml_UnsupportedImageSourceMode_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: img1
                        type: image
                        source:
                          mode: remote
                          value: image.png
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("Unsupported image source mode", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_InvalidColor_Throws()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        color: "#GGGGGG"
                        value: "Hello"
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("Invalid text color", ex.Message);
    }

    [Fact]
    public void SchemaAndFluentApi_TextLayout_AreEquivalent()
    {
        const string json = """
            {
              "kind": "FluentReport",
              "schemaVersion": 1,
              "pageDefaults": {
                "size": "A4",
                "margin": {
                  "top": 24,
                  "right": 24,
                  "bottom": 24,
                  "left": 24
                }
              },
              "pages": [
                {
                  "id": "p1",
                  "regions": {
                    "content": {
                      "nodes": [
                        {
                          "id": "title",
                          "type": "text",
                          "value": "Sales Summary",
                          "align": "center"
                        },
                        {
                          "id": "divider",
                          "type": "line",
                          "thickness": 2,
                          "color": "#000000"
                        },
                        {
                          "id": "subtitle",
                          "type": "text",
                          "value": "Generated with schema"
                        }
                      ]
                    }
                  }
                }
              ]
            }
            """;

        var fromSchema = DocumentSchemaExtensions.FromSchemaJson(json);

        var fromFluent = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24, 24, 24, 24);
                page.Content().Column(col =>
                {
                    col.Item().Text("Sales Summary").AlignCenter();
                    col.Item().Line(2, "#000000");
                    col.Item().Text("Generated with schema");
                });
            });
        });

        AssertDocumentsEquivalent(fromSchema, fromFluent);
    }

    [Fact]
    public void SchemaAndFluentApi_TableLayout_AreEquivalent()
    {
        // All table styling properties are declared explicitly in the JSON so the
        // equivalence test only compares behaviour that is actually specified in both
        // inputs, rather than relying on private schema-renderer defaults.
        const string json = """
            {
              "kind": "FluentReport",
              "schemaVersion": 1,
              "pages": [
                {
                  "id": "p1",
                  "regions": {
                    "content": {
                      "nodes": [
                        {
                          "id": "table-1",
                          "type": "table",
                          "dataSource": "sales",
                          "cellBorderWidth": 0.5,
                          "cellBorderColor": "#D3D3D3",
                          "headerBold": true,
                          "columns": [
                            {
                              "field": "region",
                              "header": "Region",
                              "width": 2
                            },
                            {
                              "field": "revenue",
                              "header": "Revenue",
                              "width": 1,
                              "align": "right"
                            }
                          ]
                        },
                        {
                          "id": "break",
                          "type": "pageBreak"
                        }
                      ]
                    }
                  }
                }
              ]
            }
            """;

        var salesRows = new[]
        {
            new Dictionary<string, object?> { ["region"] = "North", ["revenue"] = 1200m },
            new Dictionary<string, object?> { ["region"] = "South", ["revenue"] = 980m },
        };

        var dataSources = new Dictionary<string, IEnumerable<object>>
        {
            ["sales"] = salesRows,
        };

        var fromSchema = DocumentSchemaExtensions.FromSchemaJson(json, dataSources);

        var fromFluent = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Region").Bold();
                            header.Cell().Text("Revenue").Bold().AlignRight();
                        });

                        foreach (var row in salesRows)
                        {
                            table.Cell().Text(row["region"]?.ToString() ?? string.Empty);
                            table.Cell().Text(row["revenue"]?.ToString() ?? string.Empty).AlignRight();
                        }

                        table.BorderEachCell(0.5f, "#D3D3D3");
                    });

                    col.Item().PageBreak();
                });
            });
        });

        AssertDocumentsEquivalent(fromSchema, fromFluent);
    }

    private static void AssertDocumentsEquivalent(Document left, Document right)
    {
        Assert.Equal(left.Settings.Pages.Count, right.Settings.Pages.Count);

        for (var i = 0; i < left.Settings.Pages.Count; i++)
        {
            var leftPage = left.Settings.Pages[i];
            var rightPage = right.Settings.Pages[i];

            Assert.Equal(leftPage.Size.Width, rightPage.Size.Width, 2);
            Assert.Equal(leftPage.Size.Height, rightPage.Size.Height, 2);
            Assert.Equal(leftPage.MarginTop, rightPage.MarginTop, 2);
            Assert.Equal(leftPage.MarginRight, rightPage.MarginRight, 2);
            Assert.Equal(leftPage.MarginBottom, rightPage.MarginBottom, 2);
            Assert.Equal(leftPage.MarginLeft, rightPage.MarginLeft, 2);

            var leftStructure = DescribeElement(leftPage.ContentElement);
            var rightStructure = DescribeElement(rightPage.ContentElement);
            Assert.Equal(leftStructure, rightStructure);
        }
    }

    private static string DescribeElement(IElement? element)
    {
        var unwrapped = Unwrap(element);
        if (unwrapped is null)
            return "null";

        return unwrapped switch
        {
            TextElement text => DescribeText(text),
            LineElement line => $"line:{line.Direction}:{line.Thickness:0.###}:{ToColorKey(line.Color)}",
            PageBreakElement => "pageBreak",
            SpacerElement => "spacer",
            ColumnElement column => $"column({column.Spacing:0.###})[{string.Join("|", column.Items.Select(DescribeElement))}]",
            RowElement row => $"row({row.Spacing:0.###})[{string.Join("|", row.Items.Select(i => DescribeElement(i.Element)))}]",
            TableElement table => DescribeTable(table),
            AlignElement align => $"align:{align.Alignment}:{DescribeElement(align.Child)}",
            PaddingElement padding =>
                $"padding({padding.Top:0.###},{padding.Right:0.###},{padding.Bottom:0.###},{padding.Left:0.###}):{DescribeElement(padding.Child)}",
            BorderElement border =>
                $"border({border.Border.Width:0.###},{ToColorKey(border.Border.Color)}):{DescribeElement(border.Child)}",
            ListElement => "list",
            _ => unwrapped.GetType().Name,
        };
    }

    private static IElement? Unwrap(IElement? element)
    {
        if (element is FluentReport.Builders.LazyElement lazy)
            return Unwrap(lazy.Built);

        return element;
    }

    private static string DescribeText(TextElement text)
    {
        var sb = new StringBuilder("text[");
        for (var i = 0; i < text.Spans.Count; i++)
        {
            if (i > 0)
                sb.Append('+');

            var span = text.Spans[i];
            var token = span.IsCurrentPage
                ? "{currentPage}"
                : span.IsTotalPages
                    ? "{totalPages}"
                    : span.StaticText ?? string.Empty;

            sb.Append(token);
        }

        sb.Append($"]:align={text.Style.Alignment}:bold={text.Style.Bold}");
        return sb.ToString();
    }

    private static string DescribeTable(TableElement table)
    {
        var columns = string.Join(",", table.Columns.Select(c => c.FixedWidth.HasValue
            ? $"f:{c.FixedWidth.Value:0.###}"
            : $"r:{c.RelativeWidth:0.###}"));

        var header = string.Join(",", table.HeaderCells.Select(DescribeCell));
        var data = string.Join(",", table.DataCells.Select(DescribeCell));
        return $"table(cols={columns};bw={table.BorderWidth:0.###};bc={ToColorKey(table.BorderColor)};h=[{header}];d=[{data}])";
    }

    private static string DescribeCell(TableCell cell)
        => $"span{cell.ColumnSpan}/h{cell.IsHeader}:{DescribeElement(cell.Content)}";

    private static string ToColorKey(ReportColor color)
        => $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
}

public class SchemaKindValidationTests
{
    [Fact]
    public void FromSchemaYaml_UnsupportedKind_Throws()
    {
        const string yaml = """
            kind: SomeOtherTool
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        value: "Hello"
            """;

        var ex = Assert.Throws<NotSupportedException>(() => DocumentSchemaExtensions.FromSchemaYaml(yaml));
        Assert.Contains("SomeOtherTool", ex.Message);
    }

    [Fact]
    public void FromSchemaYaml_KindFluentReport_Succeeds()
    {
        const string yaml = """
            kind: FluentReport
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        value: "Hello"
            """;

        var doc = DocumentSchemaExtensions.FromSchemaYaml(yaml);
        Assert.Single(doc.Settings.Pages);
    }

    [Fact]
    public void FromSchemaYaml_MissingKind_Succeeds()
    {
        // kind is optional — hand-authored documents that omit it are accepted
        const string yaml = """
            schemaVersion: 1
            pages:
              - id: p1
                regions:
                  content:
                    nodes:
                      - id: t1
                        type: text
                        value: "Hello"
            """;

        var doc = DocumentSchemaExtensions.FromSchemaYaml(yaml);
        Assert.Single(doc.Settings.Pages);
    }
}
