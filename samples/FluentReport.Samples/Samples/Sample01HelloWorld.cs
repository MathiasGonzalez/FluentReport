using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>Sample 01 – Minimal "Hello World" PDF.</summary>
internal static class Sample01HelloWorld
{
    private static readonly string Title   = "Hello, FluentReport!";
    private static readonly string Subtitle = "This is a simple PDF generated with FluentReport.";

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "01-hello-world.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);
                    page.Content().Column(col =>
                    {
                        col.Item().Text(Title).FontSize(28).Bold().AlignCenter();
                        col.Item().Spacer(20);
                        col.Item().Text(Subtitle);
                    });
                });
            }).GeneratePdf());

        Console.WriteLine("Generated 01-hello-world.pdf");
    }
}
