using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

namespace FluentReport.Samples;

/// <summary>Sample 08 – Sales report with table exported to Excel.</summary>
internal static class Sample08ExcelReportWithTable
{
    internal record SalesRow(string Region, string Units, string Revenue);

    private static readonly SalesRow[] Data =
    [
        new("North", "1 200", "$48 000"),
        new("South", "850",   "$34 000"),
        new("East",  "1 050", "$42 000"),
        new("West",  "920",   "$36 800"),
    ];

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "08-report-with-table.xlsx"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Sales Report").FontSize(16).Bold().AlignCenter();
                        col.Item().Line(1, "#AAAAAA");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().Text("Q1 2025 – Summary").FontSize(12).Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background("#4472C4").Padding(4).Text("Region").Bold().Color("#FFFFFF");
                                h.Cell().Background("#4472C4").Padding(4).Text("Units").Bold().Color("#FFFFFF");
                                h.Cell().Background("#4472C4").Padding(4).Text("Revenue").Bold().Color("#FFFFFF");
                            });
                            table.BorderEachCell(1);

                            foreach (var row in Data)
                            {
                                table.Cell().Padding(4).Text(row.Region);
                                table.Cell().Padding(4).Text(row.Units);
                                table.Cell().Padding(4).Text(row.Revenue);
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
            }).GenerateExcel());

        Console.WriteLine("Generated 08-report-with-table.xlsx");
    }
}
