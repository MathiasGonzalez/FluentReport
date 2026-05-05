using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>
/// Sample 05 – Full-page e-Factura (A4) based on the DGI CFE format (Uruguay).
/// Shares <see cref="InvoiceData"/> with <see cref="Sample06ThermalInvoice"/>.
/// </summary>
internal static class Sample05Invoice
{
    /// <summary>Returns the sample invoice data used by both samples 05 and 06.</summary>
    public static InvoiceData GetSampleData() => new(
        EmisorNombre:           "Empresa Demo S.A.",
        EmisorNombreComercial:  "Demo Corp",
        EmisorRut:              "21234567-1",
        EmisorDomicilio:        "Av. 18 de Julio 1234",
        EmisorCiudad:           "Montevideo",
        TipoDocumento:          "e-Factura",
        SerieNumero:            "A 00000001",
        FechaEmision:           "01/05/2026",
        ReceptorNombre:         "Cliente de Ejemplo S.A.",
        ReceptorRut:            "21234568-0",
        ReceptorDireccion:      "Rambla Rep. de México 6125, Montevideo",
        Lineas:
        [
            new(Cant: 2m,  Desc: "Servicio de consultoría",    PUnit: 10_000m, Iva: "22%", Total: 20_000m),
            new(Cant: 1m,  Desc: "Licencia anual de software", PUnit: 15_000m, Iva: "22%", Total: 15_000m),
            new(Cant: 3m,  Desc: "Soporte técnico (horas)",    PUnit:  3_000m, Iva: "10%", Total:  9_000m),
        ]
    );

    public static void Generate(string outputDir)
    {
        var invoice  = GetSampleData();
        var qrBytes  = SampleHelpers.GenerarQrPlaceholder(80);

        File.WriteAllBytes(Path.Combine(outputDir, "05-invoice.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginAll(40);

                    // ── Header ────────────────────────────────────────────
                    page.Header().Column(hdr =>
                    {
                        hdr.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(emisor =>
                            {
                                emisor.Item().Text(invoice.EmisorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                                emisor.Item().Text(invoice.EmisorNombreComercial).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextSecondary);
                                emisor.Item().Text($"RUT: {invoice.EmisorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                emisor.Item().Text(invoice.EmisorDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                emisor.Item().Text(invoice.EmisorCiudad).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                            {
                                box.Item().Text(invoice.TipoDocumento).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"N° {invoice.SerieNumero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"Fecha: {invoice.FechaEmision}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                        });

                        hdr.Item().Spacer(6);
                        hdr.Item().Line(1, FacturaUY.LineSeparator);
                    });

                    // ── Content ───────────────────────────────────────────
                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        // Receptor
                        col.Item().PaddingVertical(5).Column(rec =>
                        {
                            rec.Item().Text("Receptor:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                            rec.Item().Text(invoice.ReceptorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            rec.Item().Text($"RUT: {invoice.ReceptorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            rec.Item().Text(invoice.ReceptorDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
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
                            foreach (var l in invoice.Lineas)
                            {
                                string bg = alt ? FacturaUY.RowAlt : FacturaUY.RowBase;
                                table.Cell().Background(bg).Padding(4).Text(SampleHelpers.Fmt(l.Cant, "F2")).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                                table.Cell().Background(bg).Padding(4).Text(l.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                table.Cell().Background(bg).Padding(4).Text($"$ {SampleHelpers.Fmt(l.PUnit)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                                table.Cell().Background(bg).Padding(4).Text(l.Iva).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                                table.Cell().Background(bg).Padding(4).Text($"$ {SampleHelpers.Fmt(l.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                                alt = !alt;
                            }
                        });

                        // Totals
                        col.Item().PaddingTop(10).Column(tot =>
                        {
                            tot.Spacing(2);
                            tot.Item().Text($"Neto IVA 10%:   $ {SampleHelpers.Fmt(invoice.NetoIva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                            tot.Item().Text($"IVA 10%:         $ {SampleHelpers.Fmt(invoice.Iva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                            tot.Item().Text($"Neto IVA 22%:   $ {SampleHelpers.Fmt(invoice.NetoIva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                            tot.Item().Text($"IVA 22%:         $ {SampleHelpers.Fmt(invoice.Iva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                            tot.Item().Line(1, FacturaUY.LineSeparator);
                            tot.Item().Text($"TOTAL:   $ {SampleHelpers.Fmt(invoice.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignRight().Color(FacturaUY.TextPrimary);
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

                    // ── Footer ────────────────────────────────────────────
                    var footerStyle = SampleHelpers.UyFooterStyle();
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
    }
}
