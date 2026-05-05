namespace FluentReport.Samples;

// ── Shared data structures for e-Factura samples (05 and 06) ─────────────────

/// <summary>Línea de ítem en una e-Factura (CFE, DGI Uruguay).</summary>
internal record InvoiceLineItem(decimal Cant, string Desc, decimal PUnit, string Iva, decimal Total);

/// <summary>
/// Datos completos para generar una e-Factura (CFE) conforme a las normas DGI de Uruguay.
/// Las propiedades computadas derivan los netos e IVA a partir de las líneas de detalle.
/// </summary>
internal record InvoiceData(
    // Emisor
    string EmisorNombre,
    string EmisorNombreComercial,
    string EmisorRut,
    string EmisorDomicilio,
    string EmisorCiudad,
    // Tipo de documento
    string TipoDocumento,
    string SerieNumero,
    string FechaEmision,
    // Receptor
    string ReceptorNombre,
    string ReceptorRut,
    string ReceptorDireccion,
    // Líneas de detalle
    InvoiceLineItem[] Lineas
)
{
    public decimal Bruto22   => Lineas.Where(l => l.Iva == "22%").Sum(l => l.Total);
    public decimal Bruto10   => Lineas.Where(l => l.Iva == "10%").Sum(l => l.Total);
    public decimal NetoIva22 => Math.Round(Bruto22 / 1.22m, 2);
    public decimal Iva22     => Bruto22 - NetoIva22;
    public decimal NetoIva10 => Math.Round(Bruto10 / 1.10m, 2);
    public decimal Iva10     => Bruto10 - NetoIva10;
    public decimal Total     => Lineas.Sum(l => l.Total);
}
