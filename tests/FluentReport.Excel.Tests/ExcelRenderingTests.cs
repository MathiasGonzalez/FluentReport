using ClosedXML.Excel;
using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;
using FluentReport.Schema;

namespace FluentReport.Excel.Tests;

public class ExcelRenderingTests
{
    // ── Basic smoke tests ─────────────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_WithSimpleText_ProducesValidXlsx()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Hello, Excel!");
                });
            });
        }).GenerateExcel();

        Assert.NotEmpty(bytes);
        // .xlsx files start with PK (zip header)
        Assert.Equal(0x50, bytes[0]); // 'P'
        Assert.Equal(0x4B, bytes[1]); // 'K'
    }

    [Fact]
    public void GenerateExcel_WithSimpleText_CellContainsExpectedValue()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Hello, Excel!");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("Hello, Excel!", sheet.Cell(1, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithTable_WritesHeaderAndDataRows()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Name");
                            h.Cell().Text("Value");
                        });
                        table.Cell().Text("Alpha");
                        table.Cell().Text("100");
                        table.Cell().Text("Beta");
                        table.Cell().Text("200");
                    });
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);

        // Header row (row 1)
        Assert.Equal("Name", sheet.Cell(1, 1).GetString());
        Assert.Equal("Value", sheet.Cell(1, 6).GetString()); // col 6 with 10 total cols, 2 equal cols

        // Data rows
        Assert.Equal("Alpha", sheet.Cell(2, 1).GetString());
        Assert.Equal("Beta", sheet.Cell(3, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithBoldText_AppliesBoldStyle()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Bold Title").Bold();
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
    }

    [Fact]
    public void GenerateExcel_WithBackground_AppliesFillColor()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Background("#CCCCCC").Padding(5).Text("Shaded");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        var fill = sheet.Cell(1, 1).Style.Fill.BackgroundColor;
        Assert.NotEqual(XLColor.NoColor, fill);
    }

    [Fact]
    public void GenerateExcel_WithPageBreak_CreatesMultipleSheets()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Page one content");
                    col.Item().PageBreak();
                    col.Item().Text("Page two content");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("Page one content", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("Page two content", workbook.Worksheet(2).Cell(1, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithMultipleDocumentPages_CreatesOneSheetPerPage()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col => col.Item().Text("Sheet A"));
            });
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col => col.Item().Text("Sheet B"));
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("Sheet A", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("Sheet B", workbook.Worksheet(2).Cell(1, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithRowElement_WritesItemsSideBySide()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Row(row =>
                {
                    row.Item().Text("Left");
                    row.Item().Text("Right");
                });
            });
        }).GenerateExcel();

        Assert.NotEmpty(bytes);
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        // The two items are in columns 1 and 6 (with 10 cols, split 5 each)
        Assert.Equal("Left", sheet.Cell(1, 1).GetString());
        Assert.Equal("Right", sheet.Cell(1, 6).GetString());
    }

    [Fact]
    public void GenerateExcel_WithHeaderAndFooter_WritesHeaderFirstAndFooterLast()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Header().Text("Report Header");
                page.Content().Column(col => col.Item().Text("Body text"));
                page.Footer().Text("Report Footer");
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("Report Header", sheet.Cell(1, 1).GetString());
        Assert.Equal("Body text", sheet.Cell(2, 1).GetString());
        Assert.Equal("Report Footer", sheet.Cell(3, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithLine_DoesNotThrow()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Above");
                    col.Item().Line(1, "#000000");
                    col.Item().Text("Below");
                });
            });
        }).GenerateExcel();

        Assert.NotEmpty(bytes);
    }

        // ── Schema integration ───────────────────────────────────────────────────

        [Fact]
        public void GenerateExcel_FromSchemaJson_WritesExpectedCellValue()
        {
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
                                                                            "id": "t1",
                                                                            "type": "text",
                                                                            "value": "Hello from schema"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    ]
                                                }
                                                """;

                                var bytes = DocumentSchemaExtensions.FromSchemaJson(json).GenerateExcel();

                using var ms = new MemoryStream(bytes);
                using var workbook = new XLWorkbook(ms);
                var sheet = workbook.Worksheet(1);
                Assert.Equal("Hello from schema", sheet.Cell(1, 1).GetString());
        }

        [Fact]
        public void GenerateExcel_FromSchemaJson_WithMissingDataSource_Throws()
        {
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
                                                                            "columns": [
                                                                                {
                                                                                    "field": "region",
                                                                                    "header": "Region"
                                                                                }
                                                                            ]
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    ]
                                                }
                                                """;

                                Assert.Throws<InvalidOperationException>(() => DocumentSchemaExtensions.FromSchemaJson(json).GenerateExcel());
        }

    [Fact]
    public void GenerateExcel_WritesToStream()
    {
        using var stream = new MemoryStream();
        Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col => col.Item().Text("Stream test"));
            });
        }).GenerateExcel(stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void GenerateExcel_WritesToFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fr_test_{Guid.NewGuid():N}.xlsx");
        try
        {
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);
                    page.Content().Column(col => col.Item().Text("File test"));
                });
            }).GenerateExcel(path);

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── Tests for reviewer-identified fixes ──────────────────────────────────

    [Fact]
    public void GenerateExcel_WithColumnSpacing_InsertsBlankRowsBetweenItems()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Spacing(10); // non-zero spacing
                    col.Item().Text("First");
                    col.Item().Text("Second");
                    col.Item().Text("Third");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        // With spacing, blank rows are inserted between items
        Assert.Equal("First", sheet.Cell(1, 1).GetString());
        Assert.Equal("", sheet.Cell(2, 1).GetString()); // blank spacer row
        Assert.Equal("Second", sheet.Cell(3, 1).GetString());
        Assert.Equal("", sheet.Cell(4, 1).GetString()); // blank spacer row
        Assert.Equal("Third", sheet.Cell(5, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithNoColumnSpacing_NoBlankRowsBetweenItems()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    // default spacing = 0
                    col.Item().Text("First");
                    col.Item().Text("Second");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        Assert.Equal("First", sheet.Cell(1, 1).GetString());
        Assert.Equal("Second", sheet.Cell(2, 1).GetString());
    }

    [Fact]
    public void GenerateExcel_WithRowSpacing_InsertsGapColumnBetweenItems()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Row(row =>
                {
                    row.Spacing(10); // non-zero spacing
                    row.Item().Text("Left");
                    row.Item().Text("Right");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        // With spacing, a gap column is reserved so "Right" is pushed further right
        string left = sheet.Cell(1, 1).GetString();
        Assert.Equal("Left", left);
        // "Right" should be somewhere after col 1 (not adjacent)
        bool rightFound = false;
        for (int c = 2; c <= 11; c++)
        {
            if (sheet.Cell(1, c).GetString() == "Right") { rightFound = true; break; }
        }
        Assert.True(rightFound);
    }

    [Fact]
    public void GenerateExcel_WithMixedFixedAndRelativeTableColumns_DoesNotGiveFixedColumnAllColumns()
    {
        // Previously ConstantColumn(80) weight=80 would dominate RelativeColumn(1) weight=1
        // causing the fixed column to get ~89% of virtual columns
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(80); // fixed
                            cols.RelativeColumn(1);  // relative
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Fixed");
                            h.Cell().Text("Relative");
                        });
                    });
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);

        // "Fixed" should start at col 1 and "Relative" at some column > 1 and < 10
        // (not at col 10 which would mean fixed got 9/10 columns)
        string fixedHeader = sheet.Cell(1, 1).GetString();
        Assert.Equal("Fixed", fixedHeader);

        // Find where "Relative" header starts
        int relativeCol = -1;
        for (int c = 2; c <= 11; c++)
        {
            if (sheet.Cell(1, c).GetString() == "Relative") { relativeCol = c; break; }
        }
        Assert.True(relativeCol > 1, "Relative column should be after col 1");
        Assert.True(relativeCol <= 8, "Relative column should not be at the very end (fixed column should not dominate)");
    }

    [Fact]
    public void GenerateExcel_WithTableCellAlignment_AppliesAlignment()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Left");
                            h.Cell().AlignRight().Text("Right-aligned");
                        });
                    });
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        // Second header cell should be right-aligned
        Assert.Equal(XLAlignmentHorizontalValues.Right, sheet.Cell(1, 6).Style.Alignment.Horizontal);
    }

    [Fact]
    public void GenerateExcel_WithMultiSpanText_PreservesPerSpanFormatting()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("Normal ");
                        t.Span("Bold", s => s.Bold = true);
                    });
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        var cell = sheet.Cell(1, 1);
        // The cell should contain rich text
        Assert.True(cell.HasRichText);
        // Combined text should include both spans
        Assert.Contains("Normal", cell.GetString());
        Assert.Contains("Bold", cell.GetString());
    }

    [Fact]
    public void GenerateExcel_WithColoredLine_AppliesLineColor()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Above");
                    col.Item().Line(2, "#FF0000"); // red, thick
                    col.Item().Text("Below");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        // Line is at row 2; check that the bottom border color of a cell in that row is red
        var lineCell = sheet.Cell(2, 1);
        Assert.NotEqual(XLBorderStyleValues.None, lineCell.Style.Border.BottomBorder);
        // The color should be red-ish (R=255, G=0, B=0)
        var color = lineCell.Style.Border.BottomBorderColor;
        Assert.Equal(255, color.Color.R);
        Assert.Equal(0, color.Color.G);
        Assert.Equal(0, color.Color.B);
    }

    [Fact]
    public void GenerateExcel_WithThickLine_UsesMediumBorderStyle()
    {
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Line(3, "#000000"); // thickness >= 2 → medium border
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);
        Assert.Equal(XLBorderStyleValues.Medium, sheet.Cell(1, 1).Style.Border.BottomBorder);
    }

    [Fact]
    public void GenerateExcel_DistributeColumns_SumNeverExceedsTotalCols()
    {
        // Regression test: many items with small relative weights should always sum to TotalColumns
        var renderer = new ExcelDocumentRenderer(new DocumentSettings());
        // Test via the public interface by generating a document with many row items
        var bytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Row(row =>
                {
                    // 7 equal items — with rounding they could previously exceed TotalColumns
                    for (int i = 0; i < 7; i++)
                        row.Item().Text($"Col{i + 1}");
                });
            });
        }).GenerateExcel();

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheet(1);

        // Find the last non-empty column — it must be <= TotalColumns (10) + 1
        // (EndCol is exclusive so content is spread within 10 cols)
        int lastCol = 0;
        for (int c = 1; c <= 11; c++)
            if (sheet.Cell(1, c).GetString().Length > 0) lastCol = c;
        Assert.True(lastCol <= 10, $"Content spilled past column 10 (lastCol={lastCol})");
    }
}

