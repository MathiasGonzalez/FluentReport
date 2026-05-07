using FluentReport;
using FluentReport.Core;
using FluentReport.Html;

namespace FluentReport.Samples;

/// <summary>
/// Sample 13 – Email invoice rendered as HTML using <see cref="FluentReport.Html"/>.
/// Demonstrates GenerateHtml() (full document) and GenerateHtmlFragment() (body fragment).
/// </summary>
internal static class Sample13HtmlEmail
{
    public static void Generate(string outputDir)
    {
        var invoice = Sample05Invoice.GetSampleData();

        var doc = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(0);

                // ── Header ────────────────────────────────────────────────
                page.Header()
                    .Background("#003366")
                    .Padding(20)
                    .Column(hdr =>
                    {
                        hdr.Spacing(4);
                        hdr.Item().Text(invoice.EmisorNombre)
                            .FontFamily("Arial").FontSize(18).Bold().Color("#FFFFFF");
                        hdr.Item().Text(invoice.EmisorNombreComercial)
                            .FontFamily("Arial").FontSize(11).Color("#AACCEE");
                        hdr.Item().Text($"RUT: {invoice.EmisorRut}")
                            .FontFamily("Arial").FontSize(9).Color("#FFFFFF");
                        hdr.Item().Text($"{invoice.EmisorDomicilio} — {invoice.EmisorCiudad}")
                            .FontFamily("Arial").FontSize(9).Color("#FFFFFF");
                    });

                // ── Content ───────────────────────────────────────────────
                page.Content().Padding(20).Column(col =>
                {
                    col.Spacing(12);

                    // Document type box
                    col.Item()
                        .Border(1, "#003366")
                        .Background("#EEF3FA")
                        .Padding(10)
                        .Column(box =>
                        {
                            box.Spacing(4);
                            box.Item().Row(r =>
                            {
                                r.RelativeItem(2).Text($"{invoice.TipoDocumento}  N.° {invoice.SerieNumero}")
                                    .FontFamily("Arial").FontSize(13).Bold().Color("#003366");
                                r.RelativeItem(1).AlignRight().Text($"Fecha: {invoice.FechaEmision}")
                                    .FontFamily("Arial").FontSize(9).Color("#333333");
                            });
                            box.Item().Text($"Cliente: {invoice.ReceptorNombre}  |  RUT: {invoice.ReceptorRut}")
                                .FontFamily("Arial").FontSize(9).Color("#555555");
                            box.Item().Text(invoice.ReceptorDireccion)
                                .FontFamily("Arial").FontSize(9).Color("#555555");
                        });

                    // Line items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(50);
                            cols.RelativeColumn(5);
                            cols.ConstantColumn(45);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(90);
                        });

                        // Header
                        table.Header(h =>
                        {
                            h.Cell().Background("#003366").Padding(4).AlignCenter()
                                .Text("Cant.").FontFamily("Arial").FontSize(8).Bold().Color("#FFFFFF");
                            h.Cell().Background("#003366").Padding(4)
                                .Text("Descripción").FontFamily("Arial").FontSize(8).Bold().Color("#FFFFFF");
                            h.Cell().Background("#003366").Padding(4).AlignCenter()
                                .Text("IVA").FontFamily("Arial").FontSize(8).Bold().Color("#FFFFFF");
                            h.Cell().Background("#003366").Padding(4).AlignRight()
                                .Text("P. Unit.").FontFamily("Arial").FontSize(8).Bold().Color("#FFFFFF");
                            h.Cell().Background("#003366").Padding(4).AlignRight()
                                .Text("Total").FontFamily("Arial").FontSize(8).Bold().Color("#FFFFFF");
                        });

                        bool alt = false;
                        foreach (var linea in invoice.Lineas)
                        {
                            string bg = alt ? "#F7F7F7" : "#FFFFFF";
                            alt = !alt;

                            table.Cell().Background(bg).Padding(4).AlignCenter()
                                .Text(linea.Cant.ToString("N0")).FontFamily("Arial").FontSize(9);
                            table.Cell().Background(bg).Padding(4)
                                .Text(linea.Desc).FontFamily("Arial").FontSize(9);
                            table.Cell().Background(bg).Padding(4).AlignCenter()
                                .Text(linea.Iva).FontFamily("Arial").FontSize(9);
                            table.Cell().Background(bg).Padding(4).AlignRight()
                                .Text($"$ {linea.PUnit:N0}").FontFamily("Arial").FontSize(9);
                            table.Cell().Background(bg).Padding(4).AlignRight()
                                .Text($"$ {linea.Total:N0}").FontFamily("Arial").FontSize(9).Bold();
                        }
                    });

                    // Total row
                    col.Item().AlignRight().Column(totals =>
                    {
                        totals.Spacing(4);
                        decimal total = invoice.Lineas.Sum(l => l.Total);
                        totals.Item().Border(1, "#003366").Padding(8).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").FontFamily("Arial").FontSize(12).Bold().Color("#003366");
                            r.FixedItem(120).AlignRight().Text($"$ {total:N0}").FontFamily("Arial").FontSize(12).Bold().Color("#003366");
                        });
                    });

                    // Separator
                    col.Item().Line(1, "#CCCCCC");

                    // Legal notice
                    col.Item().Text("Este documento es una representación impresa de un Comprobante Fiscal Electrónico.")
                        .FontFamily("Arial").FontSize(7).Color("#888888");
                });

                // ── Footer ─────────────────────────────────────────────────
                page.Footer()
                    .Background("#F0F0F0")
                    .Padding(12)
                    .AlignCenter()
                    .Text("FluentReport · HTML Email Sample · https://github.com/MathiasGonzalez/FluentReport")
                    .FontFamily("Arial").FontSize(8).Color("#888888");
            });
        });

        // Full HTML document
        doc.GenerateHtml(
            Path.Combine(outputDir, "13-email-invoice.html"),
            new HtmlRendererOptions { MaxWidth = 650 });

        // Fragment only (for embedding in external email templates)
        string fragment = doc.GenerateHtmlFragment(new HtmlRendererOptions { MaxWidth = 650 });
        File.WriteAllText(Path.Combine(outputDir, "13-email-invoice-fragment.html"), fragment, System.Text.Encoding.UTF8);

        Console.WriteLine("  ✓ 13-email-invoice.html");
        Console.WriteLine("  ✓ 13-email-invoice-fragment.html");
    }
}
