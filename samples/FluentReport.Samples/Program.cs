using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;
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
                col.Item().Line(0.5f, FacturaUY.LineSeparator);

                // Document type and number
                col.Item().Text(tipoDocumento).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text($"N° {serieNumero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                col.Item().Text($"Fecha: {fechaEmision}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextSecondary);
                col.Item().Line(0.5f, FacturaUY.LineSeparator);

                // Receptor
                col.Item().Text($"Cliente: {receptorNombre}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                col.Item().Text($"RUT: {receptorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                col.Item().Line(0.5f, FacturaUY.LineSeparator);

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

                col.Item().Line(0.5f, FacturaUY.LineSeparator);

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

                col.Item().Line(0.5f, FacturaUY.LineSeparator);

                // QR placeholder
                col.Item().PaddingTop(4).AlignCenter().Image(GenerarQrPlaceholder(70));
                col.Item().Text("Verifique en efactura.dgi.gub.uy").FontFamily(FacturaUY.FontPrimary).FontSize(6).AlignCenter().Color(FacturaUY.TextSecondary);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 06-thermal-invoice.pdf");

// ── Excel samples ────────────────────────────────────────────────────────────

// Excel Sample 1: simple hello world
File.WriteAllBytes(Path.Combine(outputDir, "07-hello-world.xlsx"),
    Document.Create(c =>
    {
        c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginAll(40);
            page.Content().Column(col =>
            {
                col.Item().Text("Hello, FluentReport Excel!").FontSize(18).Bold().AlignCenter();
                col.Item().Spacer(10);
                col.Item().Text("This spreadsheet was generated with FluentReport.Excel.");
            });
        });
    }).GenerateExcel());

Console.WriteLine("Generated 07-hello-world.xlsx");

// Excel Sample 2: report with table
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

                    var rows = new[] {
                        ("North", "1 200", "$48 000"),
                        ("South", "850",   "$34 000"),
                        ("East",  "1 050", "$42 000"),
                        ("West",  "920",   "$36 800"),
                    };
                    foreach (var (region, units, revenue) in rows)
                    {
                        table.Cell().Padding(4).Text(region);
                        table.Cell().Padding(4).Text(units);
                        table.Cell().Padding(4).Text(revenue);
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

// Excel Sample 3: multi-sheet via PageBreak
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
                    table.Cell().Padding(4).Text("Widget A");
                    table.Cell().Padding(4).Text("$9.99");
                    table.Cell().Padding(4).Text("Widget B");
                    table.Cell().Padding(4).Text("$14.99");
                });
            });
        });
    }).GenerateExcel());

Console.WriteLine("Generated 09-multi-sheet.xlsx");

// ── Uruguayan fiscal / legal documents ──────────────────────────────────────

// Sample 10: Recibo de Sueldo (Uruguay – MTSS compliant payslip)
var reciboSueldo = new ReciboSueldoData(
    EmpNombre:     "Empresa Demo S.A.",
    EmpRut:        "21234567-1",
    EmpDomicilio:  "Av. 18 de Julio 1234, Montevideo",
    TrabNombre:    "Juan Carlos Pérez López",
    TrabCi:        "1.234.567-8",
    TrabCargo:     "Analista de Sistemas",
    TrabLegajo:    "00042",
    TrabBps:       "1234567-1",
    PeriodoDesc:   "Mayo 2026",
    PeriodoFrom:   "01/05/2026",
    PeriodoTo:     "31/05/2026",
    FechaPago:     "05/06/2026",
    SueldoNominal: 50_000m,
    HorasExtra:    2_500m,
    Viaticos:      1_200m,
    Irpf:          1_580m   // retención mensual estimada
);

File.WriteAllBytes(Path.Combine(outputDir, "10-recibo-sueldo.pdf"),
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
                    row.RelativeItem(3).Column(emp =>
                    {
                        emp.Item().Text(reciboSueldo.EmpNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                        emp.Item().Text($"RUT: {reciboSueldo.EmpRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        emp.Item().Text(reciboSueldo.EmpDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                    {
                        box.Item().Text("RECIBO DE SUELDO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"Período: {reciboSueldo.PeriodoDesc}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                        box.Item().Text($"{reciboSueldo.PeriodoFrom} – {reciboSueldo.PeriodoTo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });

                hdr.Item().Spacer(6);
                hdr.Item().Line(1, FacturaUY.LineSeparator);
            });

            // ── Content ───────────────────────────────────────────────────
            page.Content().Column(col =>
            {
                col.Spacing(8);

                // Worker data
                col.Item().PaddingTop(6).Background(FacturaUY.RowAlt).Padding(8).Column(trab =>
                {
                    trab.Item().Text("Datos del Trabajador").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                    trab.Item().Spacer(4);
                    trab.Item().Row(r =>
                    {
                        r.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Nombre:   {reciboSueldo.TrabNombre}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            left.Item().Text($"C.I.:       {reciboSueldo.TrabCi}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            left.Item().Text($"Cargo:     {reciboSueldo.TrabCargo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        });
                        r.RelativeItem().Column(right =>
                        {
                            right.Item().Text($"Legajo:    {reciboSueldo.TrabLegajo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            right.Item().Text($"BPS:       {reciboSueldo.TrabBps}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            right.Item().Text($"F. pago:  {reciboSueldo.FechaPago}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        });
                    });
                });

                // Haberes table
                col.Item().Text("Haberes").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Concepto").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Importe ($)").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.HeaderText);
                    });
                    bool alt2 = false;
                    foreach (var (concepto, valor) in new[]
                    {
                        ("Sueldo nominal",    reciboSueldo.SueldoNominal),
                        ("Horas extras",      reciboSueldo.HorasExtra),
                        ("Viáticos",          reciboSueldo.Viaticos),
                    })
                    {
                        string bg = alt2 ? FacturaUY.RowAlt : FacturaUY.RowBase;
                        t.Cell().Background(bg).Padding(4).Text(concepto).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        t.Cell().Background(bg).Padding(4).Text($"$ {Fmt(valor)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                        alt2 = !alt2;
                    }
                    t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text("TOTAL HABERES").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                    t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text($"$ {Fmt(reciboSueldo.TotalHaberes)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                });

                // Descuentos table
                col.Item().Text("Descuentos").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Concepto").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Importe ($)").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.HeaderText);
                    });
                    bool alt3 = false;
                    foreach (var (concepto, valor) in new[]
                    {
                        ("BPS – Jubilación (15%)",          reciboSueldo.BpsJubilacion),
                        ("BPS – FONASA (3%)",               reciboSueldo.BpsFonasa),
                        ("BPS – Seg. Desempleo (0.125%)",   reciboSueldo.BpsSegDesempleo),
                        ("IRPF (retención mensual)",        reciboSueldo.Irpf),
                        ("FRL – Fondo Reconversión (1%)",   reciboSueldo.Frl),
                    })
                    {
                        string bg = alt3 ? FacturaUY.RowAlt : FacturaUY.RowBase;
                        t.Cell().Background(bg).Padding(4).Text(concepto).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        t.Cell().Background(bg).Padding(4).Text($"$ {Fmt(valor)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                        alt3 = !alt3;
                    }
                    t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text("TOTAL DESCUENTOS").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                    t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text($"$ {Fmt(reciboSueldo.TotalDescuentos)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                });

                // Net pay
                col.Item().Border(1, FacturaUY.HeaderBackground).Background(FacturaUY.DocBoxBackground).Padding(10).Row(r =>
                {
                    r.RelativeItem().Text("NETO A COBRAR").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().Color(FacturaUY.HeaderBackground);
                    r.RelativeItem().Text($"$ {Fmt(reciboSueldo.NetoLiquidar)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignRight().Color(FacturaUY.HeaderBackground);
                });

                // Signature area
                col.Item().PaddingTop(30).Row(r =>
                {
                    r.RelativeItem().Column(sig =>
                    {
                        sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                        sig.Item().Spacer(4);
                        sig.Item().Text("Firma del empleado").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                        sig.Item().Text(reciboSueldo.TrabNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                        sig.Item().Text($"C.I. {reciboSueldo.TrabCi}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                    r.FixedItem(40);
                    r.RelativeItem().Column(sig =>
                    {
                        sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                        sig.Item().Spacer(4);
                        sig.Item().Text("Firma del empleador").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                        sig.Item().Text(reciboSueldo.EmpNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                        sig.Item().Text($"RUT {reciboSueldo.EmpRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });

                col.Item().PaddingTop(8).Text("El trabajador declara haber recibido conforme los haberes detallados.")
                    .FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Italic().AlignCenter().Color(FacturaUY.TextSecondary);
            });

            // ── Footer ────────────────────────────────────────────────────
            var fs10 = UyFooterStyle();
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado con FluentReport  –  Página ", fs10);
                x.CurrentPageNumber(fs10);
                x.Span(" de ", fs10);
                x.TotalPages(fs10);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 10-recibo-sueldo.pdf");

// Sample 11: Remito de Entrega (Uruguay – DGI delivery note)
var remito = new RemitoData(
    Numero:                "R 00000123",
    Fecha:                 "05/05/2026",
    Hora:                  "10:30",
    RemitenteNombre:       "Empresa Demo S.A.",
    RemitenteRut:          "21234567-1",
    RemitenteDireccion:    "Av. 18 de Julio 1234, Montevideo",
    DestinatarioNombre:    "Distribuidora Norte S.R.L.",
    DestinatarioRut:       "21987654-3",
    DestinatarioDireccion: "Gral. Flores 3456, Montevideo",
    LugarEntrega:          "Gral. Flores 3456 – Depósito Central",
    Transportista:         "Transporte Demo – Matrícula SBJ 4321",
    Items: new[]
    {
        new RemitoItem(10, "unid.", "Monitor LED 24\"",        "Embalaje original"),
        new RemitoItem( 5, "unid.", "Teclado USB inalámbrico", ""),
        new RemitoItem( 5, "unid.", "Mouse óptico inalámbrico",""),
        new RemitoItem( 2, "caja",  "Cables HDMI 2 m (x10)",  "Sellado"),
    }
);

File.WriteAllBytes(Path.Combine(outputDir, "11-remito-entrega.pdf"),
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
                    row.RelativeItem(3).Column(rem =>
                    {
                        rem.Item().Text(remito.RemitenteNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                        rem.Item().Text($"RUT: {remito.RemitenteRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        rem.Item().Text(remito.RemitenteDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                    {
                        box.Item().Text("REMITO DE ENTREGA").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"N° {remito.Numero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"Fecha: {remito.Fecha}  Hora: {remito.Hora}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });

                hdr.Item().Spacer(6);
                hdr.Item().Line(1, FacturaUY.LineSeparator);
            });

            // ── Content ───────────────────────────────────────────────────
            page.Content().Column(col =>
            {
                col.Spacing(10);

                // Recipient block
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(dest =>
                    {
                        dest.Item().Text("Destinatario").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                        dest.Item().Spacer(3);
                        dest.Item().Text(remito.DestinatarioNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        dest.Item().Text($"RUT: {remito.DestinatarioRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        dest.Item().Text(remito.DestinatarioDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    row.FixedItem(12);

                    row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(entrega =>
                    {
                        entrega.Item().Text("Lugar de Entrega").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                        entrega.Item().Spacer(3);
                        entrega.Item().Text(remito.LugarEntrega).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        entrega.Item().Spacer(6);
                        entrega.Item().Text("Transportista").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                        entrega.Item().Spacer(3);
                        entrega.Item().Text(remito.Transportista).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });
                });

                // Items table
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45);  // Cantidad
                        cols.ConstantColumn(50);  // Unidad
                        cols.RelativeColumn(4);   // Descripción
                        cols.RelativeColumn(2);   // Observaciones
                    });

                    t.Header(h =>
                    {
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Cant.").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Unidad").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Descripción").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderText);
                        h.Cell().Background(FacturaUY.HeaderBackground).Padding(5).Text("Observaciones").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderText);
                    });

                    bool alt4 = false;
                    foreach (var it in remito.Items)
                    {
                        string bg = alt4 ? FacturaUY.RowAlt : FacturaUY.RowBase;
                        t.Cell().Background(bg).Padding(4).Text(it.Cant.ToString()).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextPrimary);
                        t.Cell().Background(bg).Padding(4).Text(it.Unidad).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextPrimary);
                        t.Cell().Background(bg).Padding(4).Text(it.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        t.Cell().Background(bg).Padding(4).Text(it.Obs).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Color(FacturaUY.TextSecondary);
                        alt4 = !alt4;
                    }
                });

                // Observations
                col.Item().Border(0.5f, FacturaUY.LineSeparator).Padding(8).Column(obs =>
                {
                    obs.Item().Text("Observaciones generales:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                    obs.Item().Spacer(4);
                    obs.Item().Text("Mercadería entregada en buen estado y embalaje sin daños visibles.").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextSecondary);
                });

                // Signatures
                col.Item().PaddingTop(30).Row(r =>
                {
                    r.RelativeItem().Column(sig =>
                    {
                        sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                        sig.Item().Spacer(4);
                        sig.Item().Text("Entregado por").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                        sig.Item().Text(remito.RemitenteNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                    });
                    r.FixedItem(40);
                    r.RelativeItem().Column(sig =>
                    {
                        sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                        sig.Item().Spacer(4);
                        sig.Item().Text("Recibido por").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                        sig.Item().Text(remito.DestinatarioNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                    });
                });
            });

            // ── Footer ────────────────────────────────────────────────────
            var fs11 = UyFooterStyle();
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado con FluentReport  –  Página ", fs11);
                x.CurrentPageNumber(fs11);
                x.Span(" de ", fs11);
                x.TotalPages(fs11);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 11-remito-entrega.pdf");

// Sample 12: Recibo de Pago (payment receipt)
var reciboPago = new ReciboPagoData(
    Numero:        "RP-2026-00456",
    Fecha:         "05/05/2026",
    PagadorNombre: "Cliente de Ejemplo S.A.",
    PagadorRut:    "21234568-0",
    BenefNombre:   "Empresa Demo S.A.",
    BenefRut:      "21234567-1",
    BenefDomicilio: "Av. 18 de Julio 1234, Montevideo",
    Concepto:      "Pago de factura N° A 00000001 – Servicios de consultoría y licencias – Mayo 2026",
    Monto:         44_000m,
    Moneda:        "Pesos Uruguayos (UYU)",
    EnLetras:      "Cuarenta y cuatro mil pesos uruguayos",
    FormaPago:     "Transferencia bancaria",
    Cuenta:        "Cuenta: 001-123456-7 – BROU"
);

File.WriteAllBytes(Path.Combine(outputDir, "12-recibo-pago.pdf"),
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
                    row.RelativeItem(3).Column(ben =>
                    {
                        ben.Item().Text(reciboPago.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                        ben.Item().Text($"RUT: {reciboPago.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        ben.Item().Text(reciboPago.BenefDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                    {
                        box.Item().Text("RECIBO DE PAGO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"N° {reciboPago.Numero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                        box.Item().Text($"Fecha: {reciboPago.Fecha}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });

                hdr.Item().Spacer(6);
                hdr.Item().Line(1, FacturaUY.LineSeparator);
            });

            // ── Content ───────────────────────────────────────────────────
            page.Content().Column(col =>
            {
                col.Spacing(10);

                // Parties
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(pag =>
                    {
                        pag.Item().Text("Pagador").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                        pag.Item().Spacer(3);
                        pag.Item().Text(reciboPago.PagadorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        pag.Item().Text($"RUT: {reciboPago.PagadorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });

                    row.FixedItem(12);

                    row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(ben =>
                    {
                        ben.Item().Text("Beneficiario").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                        ben.Item().Spacer(3);
                        ben.Item().Text(reciboPago.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        ben.Item().Text($"RUT: {reciboPago.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                    });
                });

                // Amount box
                col.Item().Border(1.5f, FacturaUY.HeaderBackground).Background(FacturaUY.DocBoxBackground).Padding(12).Column(amt =>
                {
                    amt.Item().Text("MONTO RECIBIDO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                    amt.Item().Spacer(6);
                    amt.Item().Text($"$ {Fmt(reciboPago.Monto)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeAmount).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                    amt.Item().Spacer(4);
                    amt.Item().Text($"({reciboPago.EnLetras})").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Italic().AlignCenter().Color(FacturaUY.TextSecondary);
                    amt.Item().Text($"Moneda: {reciboPago.Moneda}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                });

                // Payment details
                col.Item().Column(det =>
                {
                    det.Item().Text("Detalle del pago").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                    det.Item().Spacer(4);
                    det.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                        });
                        foreach (var (label, value) in new[]
                        {
                            ("Concepto",      reciboPago.Concepto),
                            ("Forma de pago", reciboPago.FormaPago),
                            ("Cuenta",        reciboPago.Cuenta),
                            ("Fecha",         reciboPago.Fecha),
                        })
                        {
                            t.Cell().Padding(4).Text(label + ":").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextSecondary);
                            t.Cell().Padding(4).Text(value).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                        }
                    });
                });

                // Signature
                col.Item().PaddingTop(40).Row(r =>
                {
                    r.RelativeItem();
                    r.RelativeItem(2).Column(sig =>
                    {
                        sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                        sig.Item().Spacer(4);
                        sig.Item().Text("Firma y aclaración del beneficiario").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                        sig.Item().Text(reciboPago.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                        sig.Item().Text($"RUT {reciboPago.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                    r.RelativeItem();
                });

                col.Item().PaddingTop(8)
                    .Text("Este recibo es válido como comprobante de pago de la obligación indicada en el concepto.")
                    .FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Italic().AlignCenter().Color(FacturaUY.TextSecondary);
            });

            // ── Footer ────────────────────────────────────────────────────
            var fs12 = UyFooterStyle();
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado con FluentReport  –  Página ", fs12);
                x.CurrentPageNumber(fs12);
                x.Span(" de ", fs12);
                x.TotalPages(fs12);
            });
        });
    }).GeneratePdf());

Console.WriteLine("Generated 12-recibo-pago.pdf");

Console.WriteLine($"\nAll sample files written to: {Path.GetFullPath(outputDir)}");

// ── Helpers ─────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the shared TextStyle action for the standard "Generado con FluentReport – Página N de M" footer
/// used across all Uruguayan fiscal document samples.
/// </summary>
static Action<FluentReport.Styling.TextStyle> UyFooterStyle() =>
    s => { s.FontFamily = FacturaUY.FontPrimary; s.FontSize = FacturaUY.FontSizeLegal; };

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
