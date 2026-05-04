using FluentReport;
using FluentReport.Core;
using FluentReport.Samples;
using FluentReport.Styling;
using SkiaSharp;
using System.Globalization;

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

decimal bruto22  = lineas.Where(l => l.Iva == "22%").Sum(l => l.Total);
decimal bruto10  = lineas.Where(l => l.Iva == "10%").Sum(l => l.Total);
decimal netoIva22 = Math.Round(bruto22 / 1.22m, 2);
decimal iva22     = bruto22 - netoIva22;
decimal netoIva10 = Math.Round(bruto10 / 1.10m, 2);
decimal iva10     = bruto10 - netoIva10;
decimal total     = lineas.Sum(l => l.Total);

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
                        emisor.Item().Text(emisorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                        emisor.Item().Text(emisorNombreComercial).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextSecondary);
                        emisor.Item().Text($"RUT: {emisorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        emisor.Item().Text(emisorDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        emisor.Item().Text(emisorCiudad).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    // Document type box (right, 2 relative units)
                    row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                    {
                        box.Item().Text(tipoDocumento).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"N° {serieNumero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"Fecha: {fechaEmision}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });

                hdr.Item().Spacer(6);
                hdr.Item().Line(1, FacturaUY.LineSeparator);
            });

            // ── Content ───────────────────────────────────────────────────
            page.Content().Column(col =>
            {
                col.Spacing(6);

                // Receptor
                col.Item().PaddingVertical(5).Column(rec =>
                {
                    rec.Item().Text("Receptor:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                    rec.Item().Text(receptorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    rec.Item().Text($"RUT: {receptorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    rec.Item().Text(receptorDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                });

                col.Item().Line(0.5f, FacturaUY.LineSeparator);

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
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Cant.").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Descripción").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("P.Unit.").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("IVA").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Total").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.HeaderText);
                    });

                    bool alt = false;
                    foreach (var l in lineas)
                    {
                        string bg = alt ? FacturaUY.RowAlt : FacturaUY.RowBase;
                        table.Cell().Background(bg).Padding(4).Text(Fmt(l.Cant, "F2")).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                        table.Cell().Background(bg).Padding(4).Text(l.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        table.Cell().Background(bg).Padding(4).Text($"$ {Fmt(l.PUnit)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                        table.Cell().Background(bg).Padding(4).Text(l.Iva).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                        table.Cell().Background(bg).Padding(4).Text($"$ {Fmt(l.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                        alt = !alt;
                    }
                });

                // Totals (all right-aligned)
                col.Item().PaddingTop(10).Column(tot =>
                {
                    tot.Spacing(2);
                    tot.Item().Text($"Neto IVA 10%:   $ {Fmt(netoIva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                    tot.Item().Text($"IVA 10%:         $ {Fmt(iva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                    tot.Item().Text($"Neto IVA 22%:   $ {Fmt(netoIva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                    tot.Item().Text($"IVA 22%:         $ {Fmt(iva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                    tot.Item().Line(1, FacturaUY.LineSeparator);
                    tot.Item().Text($"TOTAL:   $ {Fmt(total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                });

                // QR placeholder + legal notice
                col.Item().PaddingTop(20).Row(row =>
                {
                    row.FixedItem(85).Image(qrBytes);

                    row.RelativeItem().PaddingLeft(10).Column(info =>
                    {
                        info.Item().Text("Representación impresa de CFE").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Italic().Color(FacturaUY.TextSecondary);
                        info.Item().Text("Verifique la vigencia en: efactura.dgi.gub.uy").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextSecondary);
                        info.Item().Text("Ambiente: Producción").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextSecondary);
                        info.Item().Text("Aceptado por DGI").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Bold().Color(FacturaUY.DgiAccepted);
                    });
                });
            });

            // ── Footer ────────────────────────────────────────────────────
            Action<TextStyle> footerStyle = s => { s.FontFamily = FacturaUY.FontPrimary; s.FontSize = FacturaUY.FontSizeLegal; };
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado con FluentReport  –  Página ", footerStyle);
                x.CurrentPageNumber(footerStyle);
                x.Span(" de ", footerStyle);
                x.TotalPages(footerStyle);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 05-invoice.pdf");

// Sample 6: Thermal printer (80 mm) invoice – based on UruFacturaSDK CfeDocumentoTermico
// 80 mm ≈ 227 points; height chosen generously to fit all content
File.WriteAllBytes(Path.Combine(outputDir, "06-thermal-invoice.pdf"),
    Document.Create(c =>
    {
        c.Page(page =>
        {
            page.Size(227, 700);
            page.MarginAll(5);

            page.Content().Column(col =>
            {
                col.Spacing(3);

                // Header
                col.Item().Text(emisorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(9).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text($"RUT: {emisorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text(emisorDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Line(0.5f);

                // Document type and number
                col.Item().Text(tipoDocumento).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text($"N° {serieNumero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text($"Fecha: {fechaEmision}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextSecondary);
                col.Item().Line(0.5f);

                // Receptor
                col.Item().Text($"Cliente: {receptorNombre}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                col.Item().Text($"RUT: {receptorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                col.Item().Line(0.5f);

                // Line items
                foreach (var l in lineas)
                {
                    col.Item().Text(l.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(
                            $"  {Fmt(l.Cant, "F2")} x $ {Fmt(l.PUnit)} ({l.Iva})"
                        ).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextSecondary);
                        row.FixedItem(50).Text($"$ {Fmt(l.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                    });
                }

                col.Item().Line(0.5f);

                // Totals
                if (bruto10 > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("IVA 10%:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                        r.FixedItem(55).Text($"$ {Fmt(iva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                    });
                }
                if (bruto22 > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("IVA 22%:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                        r.FixedItem(55).Text($"$ {Fmt(iva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                    });
                }
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("TOTAL:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().Color(FacturaUY.TextPrimary);
                    r.FixedItem(55).Text($"$ {Fmt(total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                });

                col.Item().Line(0.5f);

                // QR placeholder
                col.Item().PaddingTop(4).AlignCenter().Image(GenerarQrPlaceholder(70));
                col.Item().Text("Verifique en efactura.dgi.gub.uy").FontFamily(FacturaUY.FontPrimary).FontSize(6).AlignCenter().Color(FacturaUY.TextSecondary);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 06-thermal-invoice.pdf");
Console.WriteLine($"\nAll sample PDFs written to: {Path.GetFullPath(outputDir)}");

// ── Helpers ─────────────────────────────────────────────────────────────────

/// <summary>
/// Formats a decimal value using InvariantCulture for deterministic output across locales.
/// </summary>
static string Fmt(decimal value, string format = "N2") =>
    value.ToString(format, CultureInfo.InvariantCulture);

/// <summary>
/// Creates a checkerboard PNG that mimics a QR code placeholder.
/// Replace this with an actual QR-code generator (e.g. ZXing.Net) in production.
/// </summary>
static byte[] GenerarQrPlaceholder(int size)
{
    if (size <= 0)
        throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

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
