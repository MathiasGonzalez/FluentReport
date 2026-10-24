using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>Sample 02 – Sales report with header, table, and footer.</summary>
internal static class Sample02ReportWithTable
{
    internal record SalesRow(string Region, string Jan, string Feb, string Mar);

    private static readonly SalesRow[] Data =
    [
        new("North",   "$45k", "$62k", "$38k"),
        new("South",   "$21k", "$85k", "$73k"),
        new("East",    "$56k", "$14k", "$91k"),
        new("West",    "$33k", "$47k", "$62k"),
        new("Central", "$78k", "$29k", "$54k"),
    ];

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "02-report-with-table.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Sales Report").FontSize(20).Bold().AlignCenter();
                        col.Item().Line(1, "#AAAAAA");
                        col.Item().Spacer(8);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text("Q1 2025 – Summary").FontSize(14).Bold();
                        col.Item().Text("The following table summarises sales figures for each region.");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#2E86C1").Padding(6).Text("Region").Bold();
                                h.Cell().Background("#2E86C1").Padding(6).Text("Jan").Bold();
                                h.Cell().Background("#2E86C1").Padding(6).Text("Feb").Bold();
                                h.Cell().Background("#2E86C1").Padding(6).Text("Mar").Bold();
                            });

                            for (int i = 0; i < Data.Length; i++)
                            {
                                string bg = i % 2 == 0 ? "#EBF5FB" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(5).Text(Data[i].Region);
                                table.Cell().Background(bg).Padding(5).Text(Data[i].Jan);
                                table.Cell().Background(bg).Padding(5).Text(Data[i].Feb);
                                table.Cell().Background(bg).Padding(5).Text(Data[i].Mar);
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
            }).GeneratePdf());

        Console.WriteLine("Generated 02-report-with-table.pdf");
    }
}
