using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

// ── Data ─────────────────────────────────────────────────────────────────────

/// <summary>Línea de ítem en un remito de entrega.</summary>
internal record RemitoItem(int Cant, string Unidad, string Desc, string Obs);

/// <summary>
/// Datos completos para generar un Remito de Entrega conforme a las normas DGI de Uruguay.
/// </summary>
internal record RemitoData(
    string Numero,
    string Fecha,
    string Hora,
    // Remitente
    string RemitenteNombre,
    string RemitenteRut,
    string RemitenteDireccion,
    // Destinatario
    string DestinatarioNombre,
    string DestinatarioRut,
    string DestinatarioDireccion,
    // Logística
    string LugarEntrega,
    string Transportista,
    // Ítems
    RemitoItem[] Items
);

// ── Sample ────────────────────────────────────────────────────────────────────

/// <summary>Sample 11 – Remito de Entrega (Uruguay – DGI delivery note).</summary>
internal static class Sample11RemitoEntrega
{
    private static readonly RemitoData Data = new(
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
        Items:
        [
            new(10, "unid.", "Monitor LED 24\"",         "Embalaje original"),
            new( 5, "unid.", "Teclado USB inalámbrico",  ""),
            new( 5, "unid.", "Mouse óptico inalámbrico", ""),
            new( 2, "caja",  "Cables HDMI 2 m (x10)",   "Sellado"),
        ]
    );

    public static void Generate(string outputDir)
    {
        var d = Data;

        File.WriteAllBytes(Path.Combine(outputDir, "11-remito-entrega.pdf"),
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
                            row.RelativeItem(3).Column(rem =>
                            {
                                rem.Item().Text(d.RemitenteNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                                rem.Item().Text($"RUT: {d.RemitenteRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                rem.Item().Text(d.RemitenteDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                            {
                                box.Item().Text("REMITO DE ENTREGA").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"N° {d.Numero}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"Fecha: {d.Fecha}  Hora: {d.Hora}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                        });

                        hdr.Item().Spacer(6);
                        hdr.Item().Line(1, FacturaUY.LineSeparator);
                    });

                    // ── Content ───────────────────────────────────────────
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
                                dest.Item().Text(d.DestinatarioNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                dest.Item().Text($"RUT: {d.DestinatarioRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                dest.Item().Text(d.DestinatarioDireccion).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.FixedItem(12);

                            row.RelativeItem().Background(FacturaUY.RowAlt).Padding(8).Column(entrega =>
                            {
                                entrega.Item().Text("Lugar de Entrega").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                                entrega.Item().Spacer(3);
                                entrega.Item().Text(d.LugarEntrega).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                entrega.Item().Spacer(6);
                                entrega.Item().Text("Transportista").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.HeaderBackground);
                                entrega.Item().Spacer(3);
                                entrega.Item().Text(d.Transportista).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
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

                            bool alt = false;
                            foreach (var it in d.Items)
                            {
                                string bg = alt ? FacturaUY.RowAlt : FacturaUY.RowBase;
                                t.Cell().Background(bg).Padding(4).Text(it.Cant.ToString()).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextPrimary);
                                t.Cell().Background(bg).Padding(4).Text(it.Unidad).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextPrimary);
                                t.Cell().Background(bg).Padding(4).Text(it.Desc).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                t.Cell().Background(bg).Padding(4).Text(it.Obs).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).Color(FacturaUY.TextSecondary);
                                alt = !alt;
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
                                sig.Item().Text(d.RemitenteNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                            });
                            r.FixedItem(40);
                            r.RelativeItem().Column(sig =>
                            {
                                sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                                sig.Item().Spacer(4);
                                sig.Item().Text("Recibido por").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                                sig.Item().Text(d.DestinatarioNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
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

        Console.WriteLine("Generated 11-remito-entrega.pdf");
    }
}
