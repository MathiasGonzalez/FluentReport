using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

namespace FluentReport.Samples;

/// <summary>Sample 07 – Minimal "Hello World" Excel workbook.</summary>
internal static class Sample07ExcelHelloWorld
{
    private static readonly string Title    = "Hello, FluentReport Excel!";
    private static readonly string Subtitle = "This spreadsheet was generated with FluentReport.Excel.";

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "07-hello-world.xlsx"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);
                    page.Content().Column(col =>
                    {
                        col.Item().Text(Title).FontSize(18).Bold().AlignCenter();
                        col.Item().Spacer(10);
                        col.Item().Text(Subtitle);
                    });
                });
            }).GenerateExcel());

        Console.WriteLine("Generated 07-hello-world.xlsx");
    }
}
