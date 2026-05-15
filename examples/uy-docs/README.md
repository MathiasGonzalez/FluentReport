# examples/uy-docs — Documentos Uruguayos (YAML Schemas)

Schemas listos para usar con `FluentReport.Schema` para los documentos legales/fiscales más comunes en Uruguay.
Cada archivo `.frpt.yaml` es un template completo con parámetros y fuentes de datos declarados.

## Documentos incluidos

| Archivo | Documento | Base legal |
|---------|-----------|------------|
| `factura-electronica.frpt.yaml` | e-Factura / CFE (A4) | Resolución DGI / normativa CFE |
| `recibo-sueldo.frpt.yaml` | Recibo de Sueldo | Decreto-Ley 14.188 (MTSS) |
| `remito-entrega.frpt.yaml` | Remito de Entrega | Resolución DGI Nº 2.530/991 |
| `recibo-pago.frpt.yaml` | Recibo de Pago | Código de Comercio, Ley 16.060 |

---

## Uso rápido

```csharp
using FluentReport.Schema;

// 1. Carga el schema desde archivo
string yaml = File.ReadAllText("examples/uy-docs/factura-electronica.frpt.yaml");

// 2. Prepara los datos
var dataSources = new Dictionary<string, IEnumerable<object>>
{
    ["lineas"] = new[]
    {
        new Dictionary<string, object>
        {
            ["cant"] = "2", ["descripcion"] = "Servicio de consultoría",
            ["precio_unitario"] = "$ 10.000", ["iva"] = "22 %", ["total"] = "$ 20.000"
        }
    }
};

var parameters = new Dictionary<string, object>
{
    ["emisor_nombre"]         = "Empresa Demo S.A.",
    ["emisor_rut"]            = "21234567-1",
    ["emisor_domicilio"]      = "Av. 18 de Julio 1234, Montevideo",
    ["tipo_documento"]        = "e-Factura",
    ["serie_numero"]          = "A 00000001",
    ["fecha_emision"]         = "01/06/2026",
    ["receptor_nombre"]       = "Cliente S.A.",
    ["receptor_rut"]          = "21234568-0",
    ["receptor_direccion"]    = "Rambla Rep. de México 6125, Montevideo",
    ["subtotal"]              = "$ 36.000",
    ["iva_10"]                = "$ 900",
    ["iva_22"]                = "$ 4.400",
    ["total"]                 = "$ 41.300",
};

// 3. Renderiza
var doc = DocumentSchemaExtensions.FromSchemaYaml(yaml, dataSources, parameters);
doc.GeneratePdf("factura.pdf");
```

---

## Uso desde el MCP (AI agent)

Si estás usando el servidor MCP (`FluentReport.Mcp`), llama a `get_schema_template` con el `useCase` correspondiente para obtener el template directamente sin necesidad de leer archivos:

```
get_schema_template("factura_uy")          → e-Factura / CFE
get_schema_template("recibo_sueldo_uy")    → Recibo de Sueldo
get_schema_template("remito_uy")           → Remito de Entrega
get_schema_template("recibo_pago_uy")      → Recibo de Pago
```

Una vez que tengas el schema, puedes convertirlo a código C# con:

```
schema_to_csharp(schema)                   → C# Document.Create() equivalente
```

---

## Campos por documento

### e-Factura / CFE (`factura-electronica.frpt.yaml`)

| Parámetro | Descripción |
|-----------|-------------|
| `emisor_nombre` | Razón social del emisor (requerido) |
| `emisor_rut` | RUT del emisor (requerido) |
| `emisor_domicilio` | Domicilio fiscal del emisor |
| `tipo_documento` | `e-Factura`, `e-Ticket`, etc. (requerido) |
| `serie_numero` | Número de serie (ej. `A 00000001`) (requerido) |
| `fecha_emision` | Fecha de emisión (requerido) |
| `receptor_nombre` | Razón social del receptor (requerido) |
| `receptor_rut` | RUT del receptor |
| `receptor_direccion` | Domicilio del receptor |
| `subtotal` | Subtotal sin IVA |
| `iva_10` | IVA al 10 % |
| `iva_22` | IVA al 22 % |
| `total` | Monto total |

**Data source** `lineas`: `{ cant, descripcion, precio_unitario, iva, total }`

---

### Recibo de Sueldo (`recibo-sueldo.frpt.yaml`)

| Parámetro | Descripción |
|-----------|-------------|
| `emp_nombre` | Nombre del empleador (requerido) |
| `emp_rut` | RUT del empleador (requerido) |
| `trab_nombre` | Nombre del trabajador (requerido) |
| `trab_ci` / `trab_cargo` / `trab_legajo` / `trab_bps` | Datos del trabajador |
| `periodo_desc` | Descripción del período (requerido) |
| `fecha_pago` | Fecha de pago (requerido) |
| `total_haberes` / `total_descuentos` / `neto_liquidar` | Totales (requeridos) |

**Data sources**: `haberes` `{ concepto, monto }` · `descuentos` `{ concepto, tasa, monto }`

> Tasas vigentes BPS: Jubilación 15 %, FONASA 3 %, Seg. Desempleo 0.125 %, FRL 1 %.

---

### Remito de Entrega (`remito-entrega.frpt.yaml`)

| Parámetro | Descripción |
|-----------|-------------|
| `numero` | Número único correlativo (requerido) |
| `fecha` / `hora` | Fecha y hora de emisión |
| `remitente_nombre` / `remitente_rut` | Datos del remitente (requeridos) |
| `destinatario_nombre` | Razón social del destinatario (requerido) |
| `lugar_entrega` | Domicilio de entrega |
| `transportista` | Nombre del transportista y patente |

**Data source** `items`: `{ cantidad, unidad, descripcion, observaciones }`

---

### Recibo de Pago (`recibo-pago.frpt.yaml`)

| Parámetro | Descripción |
|-----------|-------------|
| `numero` | Número de recibo (requerido) |
| `fecha` | Fecha de emisión (requerido) |
| `benef_nombre` / `benef_rut` | Datos del beneficiario (requeridos) |
| `pagador_nombre` | Razón social del pagador (requerido) |
| `concepto` | Descripción de la obligación cancelada (requerido) |
| `monto_cifra` | Monto en números (requerido) |
| `monto_letras` | Monto en letras (requerido) |
| `moneda` | Ej. `Pesos Uruguayos (UYU)` |
| `forma_pago` | Efectivo, transferencia, cheque, etc. |
| `cuenta` | Cuenta bancaria cuando aplique |

---

## Referencias

- [DGI Uruguay — e-Factura](https://www.efactura.dgi.gub.uy/)
- [MTSS — Recibo de Sueldo](https://www.mtss.gub.uy/)
- [BPS — Aportes patronales y personales](https://www.bps.gub.uy/)
