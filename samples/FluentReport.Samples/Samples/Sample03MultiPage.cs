using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>Sample 03 – Multi-page document with page breaks.</summary>
internal static class Sample03MultiPage
{
    private const int SectionCount  = 3;
    private const int ItemsPerSection = 10;

    public static void Generate(string outputDir)
    {
        File.WriteAllBytes(Path.Combine(outputDir, "03-multi-page.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);

                    page.Header().Text("Multi-Page Document").FontSize(18).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        for (int section = 1; section <= SectionCount; section++)
                        {
                            if (section > 1)
                                col.Item().PageBreak();

                            col.Item().Text($"Section {section}").FontSize(16).Bold();
                            col.Item().Line(1);
                            col.Item().Spacer(8);

                            for (int i = 1; i <= ItemsPerSection; i++)
                                col.Item().Text($"  • Item {i} of section {section}: Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
                        }
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

        Console.WriteLine("Generated 03-multi-page.pdf");
    }
}
