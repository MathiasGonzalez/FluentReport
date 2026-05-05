using FluentReport;
using FluentReport.Core;

namespace FluentReport.Samples;

// ── Data ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Datos completos para generar un Recibo de Sueldo conforme al MTSS de Uruguay.
/// Las propiedades computadas calculan los descuentos BPS/FRL a partir de los haberes ingresados.
/// </summary>
internal record ReciboSueldoData(
    // Empleador
    string EmpNombre,
    string EmpRut,
    string EmpDomicilio,
    // Trabajador
    string TrabNombre,
    string TrabCi,
    string TrabCargo,
    string TrabLegajo,
    string TrabBps,
    // Período
    string PeriodoDesc,
    string PeriodoFrom,
    string PeriodoTo,
    string FechaPago,
    // Haberes
    decimal SueldoNominal,
    decimal HorasExtra,
    decimal Viaticos,
    // Descuentos variables
    decimal Irpf      // retención mensual estimada – calculada externamente
)
{
    // Base de aportes: viáticos están exonerados de BPS
    public decimal BaseAportes     => SueldoNominal + HorasExtra;

    // Descuentos legales (tasas vigentes BPS)
    public decimal BpsJubilacion   => Math.Round(BaseAportes * 0.15m,    2);
    public decimal BpsFonasa       => Math.Round(BaseAportes * 0.03m,    2);
    public decimal BpsSegDesempleo => Math.Round(BaseAportes * 0.00125m, 2);
    public decimal Frl             => Math.Round(BaseAportes * 0.01m,    2);  // Fondo de Reconversión Laboral

    public decimal TotalHaberes    => SueldoNominal + HorasExtra + Viaticos;
    public decimal TotalDescuentos => BpsJubilacion + BpsFonasa + BpsSegDesempleo + Irpf + Frl;
    public decimal NetoLiquidar    => TotalHaberes - TotalDescuentos;
}

// ── Sample ────────────────────────────────────────────────────────────────────

/// <summary>Sample 10 – Recibo de Sueldo (Uruguay – MTSS-compliant payslip).</summary>
internal static class Sample10ReciboSueldo
{
    private static readonly ReciboSueldoData Data = new(
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
        Irpf:          1_580m
    );

    public static void Generate(string outputDir)
    {
        var d = Data;

        File.WriteAllBytes(Path.Combine(outputDir, "10-recibo-sueldo.pdf"),
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
                            row.RelativeItem(3).Column(emp =>
                            {
                                emp.Item().Text(d.EmpNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeTitle).Bold().Color(FacturaUY.TextPrimary);
                                emp.Item().Text($"RUT: {d.EmpRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                emp.Item().Text(d.EmpDomicilio).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                            });

                            row.RelativeItem(2).AlignRight().Border(1, FacturaUY.DocBoxBorder).Background(FacturaUY.DocBoxBackground).Padding(8).Column(box =>
                            {
                                box.Item().Text("RECIBO DE SUELDO").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignCenter().Color(FacturaUY.HeaderBackground);
                                box.Item().Text($"Período: {d.PeriodoDesc}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignCenter().Color(FacturaUY.TextSecondary);
                                box.Item().Text($"{d.PeriodoFrom} – {d.PeriodoTo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                        });

                        hdr.Item().Spacer(6);
                        hdr.Item().Line(1, FacturaUY.LineSeparator);
                    });

                    // ── Content ───────────────────────────────────────────
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
                                    left.Item().Text($"Nombre:   {d.TrabNombre}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                    left.Item().Text($"C.I.:       {d.TrabCi}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                    left.Item().Text($"Cargo:     {d.TrabCargo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                });
                                r.RelativeItem().Column(right =>
                                {
                                    right.Item().Text($"Legajo:    {d.TrabLegajo}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                    right.Item().Text($"BPS:       {d.TrabBps}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                    right.Item().Text($"F. pago:  {d.FechaPago}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
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
                            bool alt = false;
                            foreach (var (concepto, valor) in new[]
                            {
                                ("Sueldo nominal", d.SueldoNominal),
                                ("Horas extras",   d.HorasExtra),
                                ("Viáticos",       d.Viaticos),
                            })
                            {
                                string bg = alt ? FacturaUY.RowAlt : FacturaUY.RowBase;
                                t.Cell().Background(bg).Padding(4).Text(concepto).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                t.Cell().Background(bg).Padding(4).Text($"$ {SampleHelpers.Fmt(valor)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                                alt = !alt;
                            }
                            t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text("TOTAL HABERES").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                            t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text($"$ {SampleHelpers.Fmt(d.TotalHaberes)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.TextPrimary);
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
                            bool alt = false;
                            foreach (var (concepto, valor) in new[]
                            {
                                ("BPS – Jubilación (15%)",        d.BpsJubilacion),
                                ("BPS – FONASA (3%)",             d.BpsFonasa),
                                ("BPS – Seg. Desempleo (0.125%)", d.BpsSegDesempleo),
                                ("IRPF (retención mensual)",      d.Irpf),
                                ("FRL – Fondo Reconversión (1%)", d.Frl),
                            })
                            {
                                string bg = alt ? FacturaUY.RowAlt : FacturaUY.RowBase;
                                t.Cell().Background(bg).Padding(4).Text(concepto).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Color(FacturaUY.TextPrimary);
                                t.Cell().Background(bg).Padding(4).Text($"$ {SampleHelpers.Fmt(valor)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).AlignRight().Color(FacturaUY.TextPrimary);
                                alt = !alt;
                            }
                            t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text("TOTAL DESCUENTOS").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().Color(FacturaUY.TextPrimary);
                            t.Cell().Background(FacturaUY.RowAlt).Padding(4).Text($"$ {SampleHelpers.Fmt(d.TotalDescuentos)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeBody).Bold().AlignRight().Color(FacturaUY.TextPrimary);
                        });

                        // Net pay
                        col.Item().Border(1, FacturaUY.HeaderBackground).Background(FacturaUY.DocBoxBackground).Padding(10).Row(r =>
                        {
                            r.RelativeItem().Text("NETO A COBRAR").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().Color(FacturaUY.HeaderBackground);
                            r.RelativeItem().Text($"$ {SampleHelpers.Fmt(d.NetoLiquidar)}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSubtitle).Bold().AlignRight().Color(FacturaUY.HeaderBackground);
                        });

                        // Signature area
                        col.Item().PaddingTop(30).Row(r =>
                        {
                            r.RelativeItem().Column(sig =>
                            {
                                sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                                sig.Item().Spacer(4);
                                sig.Item().Text("Firma del empleado").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                                sig.Item().Text(d.TrabNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                                sig.Item().Text($"C.I. {d.TrabCi}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                            r.FixedItem(40);
                            r.RelativeItem().Column(sig =>
                            {
                                sig.Item().Line(0.5f, FacturaUY.LineSeparator);
                                sig.Item().Spacer(4);
                                sig.Item().Text("Firma del empleador").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                                sig.Item().Text(d.EmpNombre).FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextPrimary);
                                sig.Item().Text($"RUT {d.EmpRut}").FontFamily(FacturaUY.FontPrimary).FontSize(FacturaUY.FontSizeSmall).AlignCenter().Color(FacturaUY.TextSecondary);
                            });
                        });

                        col.Item().PaddingTop(8).Text("El trabajador declara haber recibido conforme los haberes detallados.")
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

        Console.WriteLine("Generated 10-recibo-sueldo.pdf");
    }
}
