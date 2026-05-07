# UY Fiscal Document Samples

Three Uruguayan fiscal/legal documents included in `FluentReport.Samples`, intended as starting points for generating real documents with live data.

**To generate all PDFs:**
```shell
dotnet run --project samples/FluentReport.Samples
# or with a custom output directory:
dotnet run --project samples/FluentReport.Samples -- /path/to/output
```

Output: `10-recibo-sueldo.pdf`, `11-remito-entrega.pdf`, `12-recibo-pago.pdf`

---

## Shared styles (`FacturaUY`)

All three documents (plus the e-invoice sample) share constants from `FacturaUY.cs`:

| Constant | Value | Usage |
|----------|-------|-------|
| `FontPrimary` | `"Liberation Sans"` | Main font (Arial-compatible, Linux/CI safe) |
| `FontSizeTitle` | `13f` | Section titles |
| `FontSizeSubtitle` | `11f` | Subtitles and highlighted values |
| `FontSizeBody` | `9f` | Body text |
| `FontSizeSmall` | `8f` | Secondary fields and notes |
| `FontSizeLegal` | `7f` | Footers and legal notices |
| `FontSizeAmount` | `22f` | Main amount (payment receipt) |
| `HeaderBackground` | `#003366` | Table header background |
| `HeaderText` | `#FFFFFF` | Text on dark backgrounds |
| `RowAlt` | `#F0F4F8` | Alternating table row |
| `DocBoxBackground` | `#E8EEF4` | Document type box background |
| `DgiAccepted` | `#006400` | "Accepted by DGI" green |

---

## 1. Salary Slip (`10-recibo-sueldo.pdf`)

Monthly payslip required by Uruguayan law (MTSS). Includes employer, employee, pay period, earnings, deductions, and net pay.

**Regulatory basis:** Decreto-Ley 14.188 (MTSS), BPS contribution rules, IRPF (DGI).

### Fields

| Section | Data |
|---------|------|
| Employer | Company name, RUT, fiscal address |
| Employee | Full name, ID, position, payroll number, BPS number |
| Period | Month description, start/end dates, payment date |
| Earnings | Base salary, overtime, per diems |
| Deductions | BPS Jubilación (15%), BPS FONASA (3%), BPS Seg. Desempleo (0.125%), IRPF, FRL (1%) |
| Net pay | Earnings − Deductions |
| Signatures | Employee and employer signature blocks |

> Deduction base = salary + overtime (per diems are exempt). IRPF rate depends on the employee's income bracket per annual DGI tables.

### Adapting the sample

```csharp
var data = new ReciboSueldoData(
    EmpNombre: "My Company S.A.",   EmpRut: "2XXXXXXX-X",
    TrabNombre: "John Doe",         TrabCargo: "Developer",
    PeriodoDesc: "June 2026",       FechaPago: "05/07/2026",
    SueldoNominal: 60_000m,         HorasExtra: 0m,   Viaticos: 0m,
    Irpf: CalculateIrpf(60_000m)    // your own logic
);
// BPS fields, totals, and net are calculated automatically.
```

---

## 2. Delivery Note (`11-remito-entrega.pdf`)

Required by DGI for goods in transit (Resolución DGI Nº 2.530/991).

### Fields

| Section | Data |
|---------|------|
| Sender | Company name, RUT, address |
| Number & date | Unique sequential number, date, time |
| Recipient | Company name, RUT, address |
| Delivery location | Physical delivery address |
| Carrier | Company/person name, vehicle plate |
| Line items | Quantity, unit, description, notes per item |
| Signatures | Sender and receiver |

### Adapting the sample

```csharp
var data = new RemitoData(
    Numero: "R 00000001",
    Fecha: "01/06/2026",   Hora: "09:00",
    RemitenteNombre: "My Company S.A.",   RemitenteRut: "2XXXXXXX-X",
    DestinatarioNombre: "Client S.R.L.", DestinatarioRut: "2XXXXXXX-X",
    Transportista: "Carrier S.A. – Plate ABC 1234",
    Items: new[]
    {
        new RemitoItem(5, "unit", "Product A", ""),
        new RemitoItem(2, "box",  "Product B", "Fragile"),
    }
);
```

---

## 3. Payment Receipt (`12-recibo-pago.pdf`)

Proof of payment issued by the payee (Código de Comercio, Ley 16.060).

### Fields

| Section | Data |
|---------|------|
| Beneficiary | Company name, RUT, address |
| Number & date | Unique sequential number, date |
| Payer | Company name, RUT |
| Amount | Figure + amount in words + currency |
| Concept | Description of the settled obligation |
| Payment method | Cash, transfer, check, etc. |
| Bank details | Account number and bank when applicable |
| Signature | Beneficiary signature and name |
| Legal notice | Text certifying the obligation is extinguished |

### Adapting the sample

```csharp
var data = new ReciboPagoData(
    Numero: "RP-2026-00001",
    Fecha: "01/06/2026",
    PagadorNombre: "Paying Company S.A.",   PagadorRut: "2XXXXXXX-X",
    BenefNombre: "My Company S.A.",         BenefRut: "2XXXXXXX-X",
    Concepto: "Payment for invoice A 00000001",
    Monto: 44_000m,
    Moneda: "Uruguayan Pesos (UYU)",
    EnLetras: "Forty-four thousand Uruguayan pesos",
    FormaPago: "Bank transfer",
    Cuenta: "Account: 001-XXXXXX-X – BROU"
);
```

---

## References

- [MTSS](https://www.mtss.gub.uy/) · [BPS](https://www.bps.gub.uy/) · [DGI](https://www.dgi.gub.uy/) · [e-Factura DGI](https://www.efactura.dgi.gub.uy/)
