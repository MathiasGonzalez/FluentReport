using ClosedXML.Excel;
using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

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
}
