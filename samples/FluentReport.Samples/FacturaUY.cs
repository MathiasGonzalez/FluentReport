namespace FluentReport.Samples;

/// <summary>
/// Colores y fuentes estándar para facturas electrónicas (e-Factura / CFE) en Uruguay,
/// siguiendo las pautas visuales habituales de los comprobantes emitidos a través del
/// sistema de Factura Electrónica de la DGI.
/// </summary>
internal static class FacturaUY
{
    // ── Fuentes ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Fuente principal utilizada en documentos comerciales en Uruguay.
    /// Liberation Sans es métricamente compatible con Arial y está disponible
    /// en sistemas Linux (usada por CI). En Windows y macOS el sistema resuelve
    /// "Arial" directamente; en Linux se mapea a Liberation Sans vía fontconfig.
    /// </summary>
    public const string FontPrimary = "Liberation Sans";

    // ── Tamaños de texto ─────────────────────────────────────────────────────
    public const float FontSizeTitle    = 13f;
    public const float FontSizeSubtitle = 11f;
    public const float FontSizeBody     =  9f;
    public const float FontSizeSmall    =  8f;
    public const float FontSizeLegal    =  7f;

    // ── Paleta de colores ────────────────────────────────────────────────────
    /// <summary>Azul marino oscuro: encabezado de tabla y títulos de sección.</summary>
    public const string HeaderBackground  = "#003366";

    /// <summary>Texto blanco sobre encabezado oscuro.</summary>
    public const string HeaderText        = "#FFFFFF";

    /// <summary>Fila alternada (impar) en tablas de ítems.</summary>
    public const string RowAlt            = "#F0F4F8";

    /// <summary>Fila base (par) en tablas de ítems.</summary>
    public const string RowBase           = "#FFFFFF";

    /// <summary>Color de líneas separadoras y bordes.</summary>
    public const string LineSeparator     = "#AAAAAA";

    /// <summary>Texto principal negro.</summary>
    public const string TextPrimary       = "#1A1A1A";

    /// <summary>Texto secundario / etiquetas.</summary>
    public const string TextSecondary     = "#444444";

    /// <summary>Verde oscuro DGI – indica comprobante aceptado.</summary>
    public const string DgiAccepted       = "#006400";

    /// <summary>Fondo del recuadro del tipo de documento (usado en la esquina superior derecha).</summary>
    public const string DocBoxBackground  = "#E8EEF4";

    /// <summary>Borde del recuadro del tipo de documento.</summary>
    public const string DocBoxBorder      = "#003366";
}
