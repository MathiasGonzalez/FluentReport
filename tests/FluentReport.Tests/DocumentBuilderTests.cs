using FluentReport;
using FluentReport.Core;

namespace FluentReport.Tests;

public class DocumentBuilderTests
{
    [Fact]
    public void CreateSimplePdf_ShouldNotThrow()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Text("Hello World").FontSize(14);
                    col.Item().Spacer(10);
                    col.Item().Text("Second line");
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void CreatePdf_WithHeaderAndFooter_ShouldNotThrow()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Header().Text("My Report").FontSize(20).Bold().AlignCenter();
                page.Content().Column(col =>
                {
                    col.Item().Text("Content here");
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
    }

    [Fact]
    public void CreatePdf_WithTable_ShouldNotThrow()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background("#EEEEEE").Padding(5).Text("Name").Bold();
                            header.Cell().Background("#EEEEEE").Padding(5).Text("Value").Bold();
                        });
                        for (int i = 1; i <= 3; i++)
                        {
                            table.Cell().Padding(5).Text($"Item {i}");
                            table.Cell().Padding(5).Text($"{i * 100}");
                        }
                    });
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void CreatePdf_WithMultiplePages_ShouldNotThrow()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        col.Item().Text($"Line {i + 1}: This is some content that might span multiple pages.");
                    }
                });
            });
        }).GeneratePdf();

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void CreatePdf_OutputsValidPdfHeader()
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Content().Text("Test");
            });
        }).GeneratePdf();

        // PDF files start with %PDF-
        Assert.True(bytes.Length > 5);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
