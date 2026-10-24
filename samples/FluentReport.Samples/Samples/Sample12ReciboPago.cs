using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

// ── Data ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Datos completos para generar un Recibo de Pago (Código de Comercio, Uruguay).
/// </summary>
internal record ReciboPagoData(
    string Numero,
    string Fecha,
    // Pagador
    string PagadorNombre,
    string PagadorRut,
    // Beneficiario
    string BenefNombre,
    string BenefRut,
    string BenefDomicilio,
    // Monto
    string Concepto,
    decimal Monto,
    string Moneda,
    string EnLetras,
    // Forma de pago
    string FormaPago,
    string Cuenta
);

// ── Sample ────────────────────────────────────────────────────────────────────

/// <summary>Sample 12 – Recibo de Pago (payment receipt, Uruguay).</summary>
internal static class Sample12ReciboPago
{
    private static readonly ReciboPagoData Data = new(
        Numero:         "RP-2026-00456",
        Fecha:          "05/05/2026",
        PagadorNombre:  "Cliente de Ejemplo S.A.",
        PagadorRut:     "21234568-0",
        BenefNombre:    "Empresa Demo S.A.",
        BenefRut:       "21234567-1",
        BenefDomicilio: "Av. 18 de Julio 1234, Montevideo",
        Concepto:       "Pago de factura N° A 00000001 – Servicios de consultoría y licencias – Mayo 2026",
        Monto:          44_000m,
        Moneda:         "Pesos Uruguayos (UYU)",
        EnLetras:       "Cuarenta y cuatro mil pesos uruguayos",
        FormaPago:      "Transferencia bancaria",
        Cuenta:         "Cuenta: 001-123456-7 – BROU"
    );

    public static void Generate(string outputDir)
    {
        var d = Data;

        File.WriteAllBytes(Path.Combine(outputDir, "12-recibo-pago.pdf"),
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
                            row.RelativeItem(3).Column(ben =>
                            {
                                ben.Item().Text(d.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                                ben.Item().Text($"RUT: {d.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                ben.Item().Text(d.BenefDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                            {
                                box.Item().Text("RECIBO DE PAGO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"N° {d.Numero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"Fecha: {d.Fecha}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                        });

                        hdr.Item().Spacer(6);
                        hdr.Item().Line(1, FacturaUY.LineSeparator);
                    });

                    // ── Content ───────────────────────────────────────────
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
                                pag.Item().Text(d.PagadorNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                pag.Item().Text($"RUT: {d.PagadorRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.FixedItem(12);

                            row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(ben =>
                            {
                                ben.Item().Text("Beneficiario").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                                ben.Item().Spacer(3);
                                ben.Item().Text(d.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                ben.Item().Text($"RUT: {d.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });
                        });

                        // Amount box
                        col.Item().Border(1.5f, FacturaUY.HeaderBackground).Background(FacturaUY.DocBoxBackground).Padding(12).Column(amt =>
                        {
                            amt.Item().Text("MONTO RECIBIDO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                            amt.Item().Spacer(6);
                            amt.Item().Text($"$ {SampleHelpers.Fmt(d.Monto)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeAmount).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                            amt.Item().Spacer(4);
                            amt.Item().Text($"({d.EnLetras})").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Italic().AlignCenter().Color(FacturaUY.TextSecondary);
                            amt.Item().Text($"Moneda: {d.Moneda}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
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
                                    ("Concepto",      d.Concepto),
                                    ("Forma de pago", d.FormaPago),
                                    ("Cuenta",        d.Cuenta),
                                    ("Fecha",         d.Fecha),
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
                                sig.Item().Text(d.BenefNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                                sig.Item().Text($"RUT {d.BenefRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                            r.RelativeItem();
                        });

                        col.Item().PaddingTop(8)
                            .Text("Este recibo es válido como comprobante de pago de la obligación indicada en el concepto.")
                            .FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeLegal).Italic().AlignCenter().Color(FacturaUY.TextSecondary);
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

        Console.WriteLine("Generated 12-recibo-pago.pdf");
    }
}
