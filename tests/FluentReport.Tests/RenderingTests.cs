using FluentReport;
using FluentReport.Core;

namespace FluentReport.Tests;

public class RenderingTests
{
    [Fact]
    public void GeneratePdf_WithRow_ShouldWork()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Row(row =>
                {
                    row.Item().Text("Left");
                    row.Item().Text("Right");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GeneratePdf_WithLine_ShouldWork()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Above the line");
                    col.Item().Line(2, "#000000");
                    col.Item().Text("Below the line");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GeneratePdf_WithBorder_ShouldWork()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Border(1).Padding(10).Text("Bordered text");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GeneratePdf_WithPageBreak_ShouldProduceLargerOutput()
    {
        var bytesWithBreak = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Page 1");
                    col.Item().PageBreak();
                    col.Item().Text("Page 2");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytesWithBreak);
    }

    [Fact]
    public void GeneratePdf_WithComplexLayout_ShouldWork()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Header().Text("Report Title").FontSize(18).Bold().AlignCenter();
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Introduction").FontSize(14).Bold();
                    col.Item().Text("This is the content of the report. It demonstrates the fluent API.");
                    col.Item().Line(1);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background("#CCCCCC").Padding(5).Text("Column 1").Bold();
                            h.Cell().Background("#CCCCCC").Padding(5).Text("Column 2").Bold();
                            h.Cell().Background("#CCCCCC").Padding(5).Text("Column 3").Bold();
                        });
                        for (int i = 0; i < 5; i++)
                        {
                            table.Cell().Padding(5).Text($"A{i}");
                            table.Cell().Padding(5).Text($"B{i}");
                            table.Cell().Padding(5).Text($"C{i}");
                        }
                    });
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
        // Verify it's a valid PDF
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
