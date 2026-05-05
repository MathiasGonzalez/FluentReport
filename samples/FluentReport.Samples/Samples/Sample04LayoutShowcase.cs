using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>Sample 04 – Layout showcase: rows, borders, padding, and text styles.</summary>
internal static class Sample04LayoutShowcase
{
    private static readonly string[] Columns = ["Column A", "Column B", "Column C"];

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "04-layout-showcase.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Layout Showcase").FontSize(20).Bold().AlignCenter();
                        col.Item().Spacer(4);
                        col.Item().Line(2, "#333333");
                        col.Item().Spacer(8);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Text("Bordered sections").FontSize(14).Bold();

                        col.Item().Row(row =>
                        {
                            foreach (var label in Columns)
                                row.Item().Border(1).Padding(10).Text(label).AlignCenter();
                        });

                        col.Item().Text("Text styles").FontSize(14).Bold();

                        col.Item().Column(inner =>
                        {
                            inner.Item().Text("Normal text");
                            inner.Item().Text("Bold text").Bold();
                            inner.Item().Text("Large text").FontSize(20);
                            inner.Item().Text("Small text").FontSize(9);
                            inner.Item().Text("Coloured text").Color("#E74C3C");
                            inner.Item().Text("Right-aligned text").AlignRight();
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

        Console.WriteLine("Generated 04-layout-showcase.pdf");
    }
}
