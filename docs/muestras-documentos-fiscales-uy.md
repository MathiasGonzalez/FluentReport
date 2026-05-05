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
var reciboSueldo = new ReciboSueldoData(
    EmpNombre:     "Mi Empresa S.A.",
    EmpRut:        "2XXXXXXX-X",
    EmpDomicilio:  "Dirección fiscal, Montevideo",
    TrabNombre:    "Nombre Apellido",
    TrabCi:        "X.XXX.XXX-X",
    TrabCargo:     "Cargo",
    TrabLegajo:    "00001",
    TrabBps:       "XXXXXXX-X",
    PeriodoDesc:   "Junio 2026",
    PeriodoFrom:   "01/06/2026",
    PeriodoTo:     "30/06/2026",
    FechaPago:     "05/07/2026",
    SueldoNominal: 60_000m,
    HorasExtra:    0m,
    Viaticos:      0m,
    Irpf:          CalcularIrpf(60_000m)  // lógica propia
);
// BpsJubilacion, BpsFonasa, BpsSegDesempleo, Frl, TotalHaberes,
// TotalDescuentos y NetoLiquidar se calculan automáticamente.
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
var remito = new RemitoData(
    Numero:                "R 00000001",  // correlativo único
    Fecha:                 "01/06/2026",
    Hora:                  "09:00",
    RemitenteNombre:       "Mi Empresa S.A.",
    RemitenteRut:          "2XXXXXXX-X",
    RemitenteDireccion:    "Dirección remitente",
    DestinatarioNombre:    "Cliente Destino S.R.L.",
    DestinatarioRut:       "2XXXXXXX-X",
    DestinatarioDireccion: "Dirección destino",
    LugarEntrega:          "Dirección de entrega",
    Transportista:         "Transportista S.A. – Matrícula ABC 1234",
    Items: new[]
    {
        new RemitoItem(5, "unid.", "Producto A", ""),
        new RemitoItem(2, "caja",  "Producto B", "Frágil"),
    }
);
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
var reciboPago = new ReciboPagoData(
    Numero:        "RP-2026-00001",
    Fecha:         "01/06/2026",
    PagadorNombre: "Empresa Pagadora S.A.",
    PagadorRut:    "2XXXXXXX-X",
    BenefNombre:   "Mi Empresa S.A.",
    BenefRut:      "2XXXXXXX-X",
    BenefDomicilio: "Dirección fiscal, Montevideo",
    Concepto:      "Pago factura N° A 00000001",
    Monto:         44_000m,
    Moneda:        "Pesos Uruguayos (UYU)",
    EnLetras:      "Cuarenta y cuatro mil pesos uruguayos",
    FormaPago:     "Transferencia bancaria",
    Cuenta:        "Cuenta: 001-XXXXXX-X – BROU"
);
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
| `FontSizeAmount` | `22f` | Monto principal destacado (recibo de pago) |
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
