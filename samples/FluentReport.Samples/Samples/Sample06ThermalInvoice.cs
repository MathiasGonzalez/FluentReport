using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

/// <summary>
/// Sample 06 – 80 mm thermal-printer e-Factura (based on UruFacturaSDK CfeDocumentoTermico).
/// Re-uses the same <see cref="InvoiceData"/> as <see cref="Sample05Invoice"/>.
/// </summary>
internal static class Sample06ThermalInvoice
{
    // 80 mm ≈ 227 points; height chosen generously to fit all content
    private const float PageWidth  = 227f;
    private const float PageHeight = 700f;

    public static void Generate(string outputDir)
    {
        var invoice = Sample05Invoice.GetSampleData();
        var qrBytes = SampleHelpers.GenerarQrPlaceholder(70);

        File.WriteAllBytes(Path.Combine(outputDir, "06-thermal-invoice.pdf"),
            Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageWidth, PageHeight);
                    page.MarginAll(5);

                    page.Content().Column(col =>
                    {
                        col.Spacing(3);

                        // Header
                        col.Item().Text(invoice.EmisorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(9).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                        col.Item().Text($"RUT: {invoice.EmisorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextPrimary);
                        col.Item().Text(invoice.EmisorDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextPrimary);
                        col.Item().Line(0.5f, FacturaUY.LineSeparator);

                        // Document type and number
                        col.Item().Text(invoice.TipoDocumento).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                        col.Item().Text($"N° {invoice.SerieNumero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignCenter().Color(FacturaUY.TextPrimary);
                        col.Item().Text($"Fecha: {invoice.FechaEmision}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignCenter().Color(FacturaUY.TextSecondary);
                        col.Item().Line(0.5f, FacturaUY.LineSeparator);

                        // Receptor
                        col.Item().Text($"Cliente: {invoice.ReceptorNombre}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                        col.Item().Text($"RUT: {invoice.ReceptorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                        col.Item().Line(0.5f, FacturaUY.LineSeparator);

                        // Line items
                        foreach (var l in invoice.Lineas)
                        {
                            col.Item().Text(l.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(
                                    $"  {SampleHelpers.Fmt(l.Cant, "F2")} x $ {SampleHelpers.Fmt(l.PUnit)} ({l.Iva})"
                                ).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextSecondary);
                                row.FixedItem(50).Text($"$ {SampleHelpers.Fmt(l.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                            });
                        }

                        col.Item().Line(0.5f, FacturaUY.LineSeparator);

                        // Totals
                        if (invoice.Bruto10 > 0)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("IVA 10%:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                                r.FixedItem(55).Text($"$ {SampleHelpers.Fmt(invoice.Iva10)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                            });
                        }
                        if (invoice.Bruto22 > 0)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("IVA 22%:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Color(FacturaUY.TextPrimary);
                                r.FixedItem(55).Text($"$ {SampleHelpers.Fmt(invoice.Iva22)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).AlignRight().Color(FacturaUY.TextPrimary);
                            });
                        }
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL:").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().Color(FacturaUY.TextPrimary);
                            r.FixedItem(55).Text($"$ {SampleHelpers.Fmt(invoice.Total)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                        });

                        col.Item().Line(0.5f, FacturaUY.LineSeparator);

                        // QR placeholder
                        col.Item().PaddingTop(4).AlignCenter().Image(qrBytes);
                        col.Item().Text("Verifique en efactura.dgi.gub.uy").FontFamily(FacturaUY.FontPrimary).FontSize(6).AlignCenter().Color(FacturaUY.TextSecondary);
                    });
                });
            }).GeneratePdf());

        Console.WriteLine("Generated 06-thermal-invoice.pdf");
    }
}
