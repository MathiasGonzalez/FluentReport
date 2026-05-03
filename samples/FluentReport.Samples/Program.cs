using FluentReport;
using FluentReport.Core;

string outputDir = args.Length > 0 ? args[0] : "output";
Directory.CreateDirectory(outputDir);

// Sample 1: Simple hello world
File.WriteAllBytes(Path.Combine(outputDir, "01-hello-world.pdf"),
    Document.Create(c =>
    {
        c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginAll(40);
            page.Content().Column(col =>
            {
                col.Item().Text("Hello, FluentReport!").FontSize(28).Bold().AlignCenter();
                col.Item().Spacer(20);
                col.Item().Text("This is a simple PDF generated with FluentReport.");
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 01-hello-world.pdf");

// Sample 2: Report with header, table and footer
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

                    string[] regions = ["North", "South", "East", "West", "Central"];
                    var rng = new Random(42);
                    foreach (var region in regions)
                    {
                        string bg = Array.IndexOf(regions, region) % 2 == 0 ? "#EBF5FB" : "#FFFFFF";
                        table.Cell().Background(bg).Padding(5).Text(region);
                        table.Cell().Background(bg).Padding(5).Text($"${rng.Next(10, 99)}k");
                        table.Cell().Background(bg).Padding(5).Text($"${rng.Next(10, 99)}k");
                        table.Cell().Background(bg).Padding(5).Text($"${rng.Next(10, 99)}k");
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

// Sample 3: Multi-page document with page breaks
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

                for (int section = 1; section <= 3; section++)
                {
                    if (section > 1)
                        col.Item().PageBreak();

                    col.Item().Text($"Section {section}").FontSize(16).Bold();
                    col.Item().Line(1);
                    col.Item().Spacer(8);

                    for (int i = 1; i <= 10; i++)
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

// Sample 4: Layout showcase (rows, borders, padding)
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
                    row.Item().Border(1).Padding(10).Text("Column A").AlignCenter();
                    row.Item().Border(1).Padding(10).Text("Column B").AlignCenter();
                    row.Item().Border(1).Padding(10).Text("Column C").AlignCenter();
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
Console.WriteLine($"\nAll sample PDFs written to: {Path.GetFullPath(outputDir)}");
