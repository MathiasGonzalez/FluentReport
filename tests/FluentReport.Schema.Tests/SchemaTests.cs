using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Schema;

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
}
