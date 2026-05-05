# Muestras de documentos fiscales y legales — Uruguay

Esta guía explica los tres documentos fiscales/legales uruguayos incluidos como muestras en `FluentReport.Samples`. Son los comprobantes más habituales en el día a día de empresas y trabajadores, y pueden usarse como punto de partida para generar documentos reales con datos propios.

---

## Índice

1. [Recibo de Sueldo (`10-recibo-sueldo.pdf`)](#1-recibo-de-sueldo)
2. [Remito de Entrega (`11-remito-entrega.pdf`)](#2-remito-de-entrega)
3. [Recibo de Pago (`12-recibo-pago.pdf`)](#3-recibo-de-pago)

---

## 1. Recibo de Sueldo

**Archivo generado:** `10-recibo-sueldo.pdf`

### ¿Qué es?

El **recibo de sueldo** (también llamado boleta de pago o liquidación de haberes) es el documento que el empleador debe entregar obligatoriamente a cada trabajador al liquidar su remuneración mensual. Su emisión está regulada por el **Ministerio de Trabajo y Seguridad Social (MTSS)** de Uruguay.

### Marco normativo

- Decreto-Ley 14.188 y modificaciones del MTSS.
- Resoluciones del **BPS (Banco de Previsión Social)** sobre aportes patronales y personales.
- Normativa del **IRPF** (Impuesto a la Renta de las Personas Físicas) — DGI.

### Campos incluidos en la muestra

| Sección | Datos |
|---------|-------|
| **Empleador** | Razón social, RUT, domicilio fiscal |
| **Trabajador** | Nombre completo, C.I., cargo, número de legajo, número BPS |
| **Período** | Descripción del mes liquidado, fechas de inicio/fin, fecha de pago |
| **Haberes** | Sueldo nominal, horas extras, viáticos |
| **Descuentos** | BPS Jubilación (15%), BPS FONASA (3%), BPS Seg. Desempleo (0,125%), IRPF (retención mensual estimada), FRL — Fondo de Reconversión Laboral (1%) |
| **Neto a cobrar** | Diferencia entre total de haberes y total de descuentos |
| **Firmas** | Bloque de firma del empleado y del empleador |

### Cálculos aplicados

```
Base de aportes = Sueldo nominal + Horas extras   (los viáticos están exonerados)

BPS Jubilación     = base × 15%
BPS FONASA         = base × 3%
BPS Seg. Desempleo = base × 0,125%
FRL                = base × 1%
IRPF               = retención mensual estimada (calculada fuera de la muestra)

Total descuentos   = BPS Jubilación + BPS FONASA + BPS Seg. Desempleo + IRPF + FRL
Neto a cobrar      = Total haberes − Total descuentos
```

> **Nota:** La tasa de IRPF varía según la escala de ingresos del trabajador. En producción debe obtenerse de las tablas publicadas anualmente por la DGI.

### Cómo adaptar la muestra

```csharp
// Reemplazá estas constantes con datos reales:
const string empNombre    = "Mi Empresa S.A.";
const string empRut       = "2XXXXXXX-X";
const string trabNombre   = "Nombre Apellido";
const string trabCi       = "X.XXX.XXX-X";
const string periodoDesc  = "Junio 2026";

decimal sueldoNominal = 60_000m;
decimal irpf          = CalcularIrpf(sueldoNominal);  // lógica propia
```

---

## 2. Remito de Entrega

**Archivo generado:** `11-remito-entrega.pdf`

### ¿Qué es?

El **remito de entrega** (o albarán) es el documento que acompaña a la mercadería cuando se traslada de un punto a otro. Acredita que los bienes fueron entregados al destinatario en el lugar, fecha y hora indicados. Es exigido por la **DGI** para el traslado de bienes y puede estar vinculado a una factura.

### Marco normativo

- Resolución DGI Nº 2.530/991 y modificativas (traslado de bienes).
- Decreto 597/988 y concordantes sobre documentación de mercaderías en tránsito.

### Campos incluidos en la muestra

| Sección | Datos |
|---------|-------|
| **Remitente** | Razón social, RUT, domicilio |
| **Número y fecha** | Número correlativo único, fecha y hora de emisión |
| **Destinatario** | Razón social, RUT, dirección |
| **Lugar de entrega** | Dirección física de entrega |
| **Transportista** | Nombre de la empresa/persona y matrícula del vehículo |
| **Detalle de ítems** | Cantidad, unidad de medida, descripción, observaciones por ítem |
| **Observaciones generales** | Estado del embalaje u otras notas |
| **Firmas** | Quien entrega y quien recibe |

### Cómo adaptar la muestra

```csharp
const string remitoNumero    = "R 00000001";  // correlativo único
const string remitentNombre  = "Mi Empresa S.A.";
const string remitentRut     = "2XXXXXXX-X";
const string destinNombre    = "Cliente Destino S.R.L.";
const string destinRut       = "2XXXXXXX-X";
const string lugarEntrega    = "Dirección de entrega";
const string transportista   = "Transportista S.A. – Matrícula ABC 1234";

var itemsRemito = new[]
{
    new { Cant = 5, Unidad = "unid.", Desc = "Producto A", Obs = "" },
    new { Cant = 2, Unidad = "caja",  Desc = "Producto B", Obs = "Frágil" },
};
```

---

## 3. Recibo de Pago

**Archivo generado:** `12-recibo-pago.pdf`

### ¿Qué es?

El **recibo de pago** es el comprobante que entrega quien **recibe** un pago a quien lo realiza, dejando constancia de que la obligación económica indicada ha quedado satisfecha. Es un documento de uso general en relaciones comerciales y de servicios, válido como prueba de pago.

### Marco normativo

- Código de Comercio de Uruguay (Ley 16.060 y concordantes).
- Se complementa con la **factura** o contrato que originó la deuda.

### Campos incluidos en la muestra

| Sección | Datos |
|---------|-------|
| **Beneficiario** | Razón social, RUT, domicilio |
| **Número y fecha** | Número correlativo único, fecha de cobro |
| **Pagador** | Razón social, RUT |
| **Monto** | Importe en números destacado + importe en letras + moneda |
| **Concepto** | Descripción de la obligación pagada (factura, servicio, etc.) |
| **Forma de pago** | Efectivo, transferencia, cheque, etc. |
| **Datos bancarios** | Número de cuenta y banco cuando aplica |
| **Firma** | Firma y aclaración del beneficiario |
| **Leyenda legal** | Texto que acredita la extinción de la obligación |

### Cómo adaptar la muestra

```csharp
const string reciboNumero    = "RP-2026-00001";
const string pagadorNombre   = "Empresa Pagadora S.A.";
const string pagadorRut      = "2XXXXXXX-X";
const string benefNombre     = "Mi Empresa S.A.";
const string benefRut        = "2XXXXXXX-X";
const string reciboConcepto  = "Pago factura N° A 00000001";
decimal      reciboMonto     = 44_000m;
const string reciboEnLetras  = "Cuarenta y cuatro mil pesos uruguayos";
const string reciboFormaPago = "Transferencia bancaria";
const string reciboCuenta    = "Cuenta: 001-XXXXXX-X – BROU";
```

---

## Estilos compartidos (`FacturaUY`)

Los tres documentos (y la e-Factura existente) reutilizan las constantes de estilo definidas en `FacturaUY.cs`:

| Constante | Valor | Uso |
|-----------|-------|-----|
| `FontPrimary` | `"Liberation Sans"` | Fuente principal (compatible Arial, funciona en Linux/CI) |
| `FontSizeTitle` | `13f` | Títulos de sección |
| `FontSizeSubtitle` | `11f` | Subtítulos y valores destacados |
| `FontSizeBody` | `9f` | Texto de cuerpo |
| `FontSizeSmall` | `8f` | Notas y campos secundarios |
| `FontSizeLegal` | `7f` | Pie de página y leyendas legales |
| `HeaderBackground` | `#003366` | Fondo de encabezados de tabla |
| `HeaderText` | `#FFFFFF` | Texto sobre fondo oscuro |
| `RowAlt` | `#F0F4F8` | Filas alternas de tabla |
| `DocBoxBackground` | `#E8EEF4` | Fondo del recuadro de tipo de documento |
| `DgiAccepted` | `#006400` | Verde "Aceptado por DGI" |

---

## Generación de los archivos

Ejecutar el proyecto de muestras para obtener todos los PDFs en la carpeta `output/`:

```shell
dotnet run --project samples/FluentReport.Samples
```

O especificar un directorio de salida:

```shell
dotnet run --project samples/FluentReport.Samples -- /ruta/salida
```

Los archivos generados serán:

```
output/
├── 10-recibo-sueldo.pdf
├── 11-remito-entrega.pdf
└── 12-recibo-pago.pdf
```

---

## Referencias

- [MTSS – Ministerio de Trabajo y Seguridad Social](https://www.mtss.gub.uy/)
- [BPS – Banco de Previsión Social](https://www.bps.gub.uy/)
- [DGI – Dirección General Impositiva](https://www.dgi.gub.uy/)
- [e-Factura DGI](https://www.efactura.dgi.gub.uy/)
