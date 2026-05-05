using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

namespace FluentReport.Samples;

/// <summary>Sample 09 – Multi-sheet Excel workbook using PageBreak.</summary>
internal static class Sample09ExcelMultiSheet
{
    internal record ProductRow(string Name, string Price);

    private static readonly ProductRow[] Products =
    [
        new("Widget A", "$9.99"),
        new("Widget B", "$14.99"),
    ];

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "09-multi-sheet.xlsx"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);
                    page.Content().Column(col =>
                    {
                        col.Item().Text("Sheet 1 – Introduction").FontSize(14).Bold();
                        col.Item().Spacer(8);
                        col.Item().Text("This content is on the first worksheet.");
                        col.Item().PageBreak();
                        col.Item().Text("Sheet 2 – Data").FontSize(14).Bold();
                        col.Item().Spacer(8);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background("#CCCCCC").Padding(4).Text("Product").Bold();
                                h.Cell().Background("#CCCCCC").Padding(4).Text("Price").Bold();
                            });
                            table.BorderEachCell(1);

                            foreach (var p in Products)
                            {
                                table.Cell().Padding(4).Text(p.Name);
                                table.Cell().Padding(4).Text(p.Price);
                            }
                        });
                    });
                });
            }).GenerateExcel());

        Console.WriteLine("Generated 09-multi-sheet.xlsx");
    }
}
