namespace FluentReport.Samples;

// ── Sample 10: Recibo de Sueldo ───────────────────────────────────────────────

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

// ── Sample 11: Remito de Entrega ──────────────────────────────────────────────

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

// ── Sample 12: Recibo de Pago ─────────────────────────────────────────────────

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
