using FluentReport;
using FluentReport.Core;
using SkiaSharp;

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

// Sample 5: e-Factura (invoice) – based on UruFacturaSDK CfePdfGenerator
// ── Invoice data ────────────────────────────────────────────────────────────
const string emisorNombre          = "Empresa Demo S.A.";
const string emisorNombreComercial = "Demo Corp";
const string emisorRut             = "21234567-1";
const string emisorDomicilio       = "Av. 18 de Julio 1234";
const string emisorCiudad          = "Montevideo";

const string tipoDocumento = "e-Factura";
const string serieNumero   = "A 00000001";
const string fechaEmision  = "01/05/2026";

const string receptorNombre   = "Cliente de Ejemplo S.A.";
const string receptorRut      = "21234568-0";
const string receptorDireccion = "Rambla Rep. de México 6125, Montevideo";

var lineas = new[]
{
    new { Cant = 2m,  Desc = "Servicio de consultoría",    PUnit = 10000m, Iva = "22%", Total = 20000m },
    new { Cant = 1m,  Desc = "Licencia anual de software", PUnit = 15000m, Iva = "22%", Total = 15000m },
    new { Cant = 3m,  Desc = "Soporte técnico (horas)",    PUnit =  3000m, Iva = "10%", Total =  9000m },
};

decimal netoIva22 = Math.Round(35000m / 1.22m, 2);  // ≈ 28,688.52
decimal iva22     = 35000m - netoIva22;              // ≈  6,311.48
decimal netoIva10 = Math.Round(9000m  / 1.10m, 2);  // ≈  8,181.82
decimal iva10     = 9000m - netoIva10;               // ≈    818.18
decimal total     = 44000m;

// QR code placeholder (gray checkerboard PNG – replace with a real QR byte[] in production)
byte[] qrBytes = GenerarQrPlaceholder(80);

File.WriteAllBytes(Path.Combine(outputDir, "05-invoice.pdf"),
    Document.Create(c =>
    {
        c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginAll(40);

            // ── Header ────────────────────────────────────────────────────
            page.Header().Column(hdr =>
            {
                hdr.Item().Row(row =>
                {
                    // Emisor info (left, 3 relative units)
                    row.RelativeItem(3).Column(emisor =>
                    {
                        emisor.Item().Text(emisorNombre).FontSize(14).Bold();
                        emisor.Item().Text(emisorNombreComercial).FontSize(10);
                        emisor.Item().Text($"RUT: {emisorRut}");
                        emisor.Item().Text(emisorDomicilio);
                        emisor.Item().Text(emisorCiudad);
                    });

                    // Document type box (right, 2 relative units)
                    row.RelativeItem(2).AlignRight().Border(1).Padding(8).Column(box =>
                    {
                        box.Item().Text(tipoDocumento).FontSize(12).Bold().AlignCenter();
                        box.Item().Text($"N° {serieNumero}").FontSize(11).Bold().AlignCenter();
                        box.Item().Text($"Fecha: {fechaEmision}").AlignCenter();
                    });
                });

                hdr.Item().Spacer(6);
                hdr.Item().Line(1, "#AAAAAA");
            });

            // ── Content ───────────────────────────────────────────────────
            page.Content().Column(col =>
            {
                col.Spacing(6);

                // Receptor
                col.Item().PaddingVertical(5).Column(rec =>
                {
                    rec.Item().Text("Receptor:").Bold();
                    rec.Item().Text(receptorNombre);
                    rec.Item().Text($"RUT: {receptorRut}");
                    rec.Item().Text(receptorDireccion);
                });

                col.Item().Line(0.5f, "#DDDDDD");

                // Line-item detail table
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45); // Cant.
                        cols.RelativeColumn(5);  // Descripción
                        cols.RelativeColumn(2);  // P.Unit.
                        cols.RelativeColumn(1);  // IVA
                        cols.RelativeColumn(2);  // Total
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background("#2E86C1").Padding(5).Text("Cant.").Bold().AlignCenter();
                        h.Cell().Background("#2E86C1").Padding(5).Text("Descripción").Bold();
                        h.Cell().Background("#2E86C1").Padding(5).Text("P.Unit.").Bold().AlignRight();
                        h.Cell().Background("#2E86C1").Padding(5).Text("IVA").Bold().AlignCenter();
                        h.Cell().Background("#2E86C1").Padding(5).Text("Total").Bold().AlignRight();
                    });

                    bool alt = false;
                    foreach (var l in lineas)
                    {
                        string bg = alt ? "#F5F5F5" : "#FFFFFF";
                        table.Cell().Background(bg).Padding(4).Text($"{l.Cant:F2}").AlignRight();
                        table.Cell().Background(bg).Padding(4).Text(l.Desc);
                        table.Cell().Background(bg).Padding(4).Text($"$ {l.PUnit:N2}").AlignRight();
                        table.Cell().Background(bg).Padding(4).Text(l.Iva).AlignCenter();
                        table.Cell().Background(bg).Padding(4).Text($"$ {l.Total:N2}").AlignRight();
                        alt = !alt;
                    }
                });

                // Totals (all right-aligned)
                col.Item().PaddingTop(10).Column(tot =>
                {
                    tot.Spacing(2);
                    tot.Item().Text($"Neto IVA 10%:   $ {netoIva10:N2}").AlignRight();
                    tot.Item().Text($"IVA 10%:         $ {iva10:N2}").AlignRight();
                    tot.Item().Text($"Neto IVA 22%:   $ {netoIva22:N2}").AlignRight();
                    tot.Item().Text($"IVA 22%:         $ {iva22:N2}").AlignRight();
                    tot.Item().Line(1, "#AAAAAA");
                    tot.Item().Text($"TOTAL:   $ {total:N2}").FontSize(12).Bold().AlignRight();
                });

                // QR placeholder + legal notice
                col.Item().PaddingTop(20).Row(row =>
                {
                    row.FixedItem(85).Image(qrBytes);

                    row.RelativeItem().PaddingLeft(10).Column(info =>
                    {
                        info.Item().Text("Representación impresa de CFE").Italic().FontSize(8);
                        info.Item().Text("Verifique la vigencia en: efactura.dgi.gub.uy").FontSize(8);
                        info.Item().Text("Ambiente: Producción").FontSize(8);
                        info.Item().Text("Aceptado por DGI").FontSize(8).Color("#1A7A1A");
                    });
                });
            });

            // ── Footer ────────────────────────────────────────────────────
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado con FluentReport  –  Página ");
                x.CurrentPageNumber();
                x.Span(" de ");
                x.TotalPages();
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 05-invoice.pdf");
Console.WriteLine($"\nAll sample PDFs written to: {Path.GetFullPath(outputDir)}");

// ── Helpers ─────────────────────────────────────────────────────────────────

/// <summary>
/// Creates a checkerboard PNG that mimics a QR code placeholder.
/// Replace this with an actual QR-code generator (e.g. ZXing.Net) in production.
/// </summary>
static byte[] GenerarQrPlaceholder(int size)
{
    using var bitmap = new SKBitmap(size, size);
    using var canvas = new SKCanvas(bitmap);

    canvas.Clear(new SKColor(240, 240, 240));

    int cellSize = Math.Max(1, size / 10);
    using var darkPaint = new SKPaint { Color = new SKColor(30, 30, 30), IsAntialias = false };
    for (int r = 0; r < 10; r++)
        for (int c = 0; c < 10; c++)
            if ((r + c) % 2 == 0)
                canvas.DrawRect(c * cellSize, r * cellSize, cellSize - 1, cellSize - 1, darkPaint);

    // Simulate QR finder-pattern corners
    using var borderPaint = new SKPaint
    {
        Color = SKColors.Black,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3,
        IsAntialias = false,
    };
    int p = 3 * cellSize;
    canvas.DrawRect(1,          1,          p - 2, p - 2, borderPaint);
    canvas.DrawRect(size - p,   1,          p - 2, p - 2, borderPaint);
    canvas.DrawRect(1,          size - p,   p - 2, p - 2, borderPaint);

    using var image = SKImage.FromBitmap(bitmap);
    using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}
