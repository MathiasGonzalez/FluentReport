# FluentReport — AI Coding Agent Quickstart

This document is the canonical starting point for AI coding agents that need to generate PDF, Excel, or HTML reports using FluentReport. It provides a minimal but complete end-to-end flow and highlights the conventions that are most likely to cause errors.

---

## 1. Package installation

```shell
dotnet add package FluentReport           # PDF rendering (always required for PDF)
dotnet add package FluentReport.Excel     # Excel output
dotnet add package FluentReport.Html      # HTML / email output
dotnet add package FluentReport.Schema    # YAML/JSON schema importer
dotnet add package FluentReport.Rdlc      # RDLC/SSRS file importer
```

You only need the packages for the output formats you actually use. `FluentReport.Core` is the shared model and is pulled in automatically; do not install it directly unless you are implementing a custom renderer.

---

## 2. Two API paths

FluentReport has two independent paths for defining documents. Both produce the same `Document` object and can use the same output methods.

| Path | When to use | Entry point |
|------|-------------|-------------|
| **Fluent C# API** | Generating reports programmatically with full C# control | `Document.Create(...)` |
| **Schema (YAML/JSON)** | Declarative definitions, AI-generated schemas | `DocumentSchemaExtensions.FromSchema*(...)` |

---

## 3. Fluent API — minimal complete example

```csharp
using FluentReport;
using FluentReport.Core;

byte[] pdf = Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);   // 595.28 × 841.89 pt
        page.MarginAll(40);

        // Optional header — repeats on every page
        page.Header()
            .Text("Acme Corp — Sales Report")
            .FontSize(14).Bold().AlignCenter();

        // Required — main page content
        page.Content().Column(col =>
        {
            col.Spacing(8);

            col.Item().Text("Q1 2026 Summary").FontSize(12).Bold();
            col.Item().Line(1, "#CCCCCC");

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);   // proportional (3/4 of available width)
                    cols.RelativeColumn(1);   // proportional (1/4)
                });
                table.Header(h =>
                {
                    h.Cell().Background("#4472C4").Padding(5).Text("Region").Bold().Color("#FFFFFF");
                    h.Cell().Background("#4472C4").Padding(5).Text("Revenue").Bold().Color("#FFFFFF");
                });
                table.BorderEachCell(0.5f, "#D3D3D3");

                var rows = new[] {
                    new { Region = "North", Revenue = "$1,200" },
                    new { Region = "South", Revenue = "$980" },
                };
                foreach (var row in rows)
                {
                    table.Cell().Padding(4).Text(row.Region);
                    table.Cell().Padding(4).AlignRight().Text(row.Revenue);
                }
            });
        });

        // Optional footer — repeats on every page
        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    });
})
.GeneratePdf();   // returns byte[]
```

**Key conventions:**
- Every dimension (margins, font sizes, column widths, spacer heights) is in **points** (1 pt = 1/72 inch).  
  A4 = 595 × 842 pt. Letter = 612 × 792 pt. Do not use millimeters.
- At least one `container.Page(...)` is required; omitting it throws `ArgumentException`.
- Page numbers (`CurrentPageNumber`, `TotalPages`) must use the `Text(Action<DynamicTextBuilder>)` overload. Concatenating strings will not produce dynamic values.
- `ContainerBuilder` accepts one child element — calling `.Text(...)` and then `.Column(...)` on the same instance means only the last call takes effect.

---

## 4. Schema (YAML) — minimal complete example

### 4a. Schema file: `report.frpt.yaml`

```yaml
kind: FluentReport
schemaVersion: 1
name: sales-summary
metadata:
  title: "{{ parameters.companyName }} — {{ parameters.period }}"

pageDefaults:
  size: A4
  orientation: portrait
  margin:
    top: 40
    right: 40
    bottom: 40
    left: 40

parameters:
  companyName:
    type: string
    required: true
  period:
    type: string
    required: true

dataSources:
  sales:
    type: array

styles:
  heading:
    fontSize: 14
    bold: true
    align: center
  tableHeader:
    fontSize: 11
    bold: true
    color: "#FFFFFF"
    background: "#4472C4"

definitions:
  groups: []
  repeatables:
    - id: sales-table
      type: table
      dataSource: sales
      columns:
        - field: region
          header: Region
          width: 3
        - field: revenue
          header: Revenue
          width: 1
          align: right

pages:
  - id: p1
    regions:
      header:
        nodes:
          - id: doc-title
            type: text
            value: "{{ parameters.companyName }} — {{ parameters.period }}"
            styleRef: heading
      content:
        nodes:
          - id: divider
            type: line
            thickness: 1
            color: "#CCCCCC"
          - id: data-table
            type: table
            dataSource: sales
            definitionRef: sales-table
      footer:
        nodes:
          - id: footer-note
            type: text
            value: "Confidential"
            align: center
```

### 4b. C# import and rendering

```csharp
using FluentReport;
using FluentReport.Schema;

string yaml = File.ReadAllText("report.frpt.yaml");

var dataSources = new Dictionary<string, IEnumerable<object>>
{
    ["sales"] = new[]
    {
        new Dictionary<string, object> { ["region"] = "North", ["revenue"] = "$1,200" },
        new Dictionary<string, object> { ["region"] = "South", ["revenue"] = "$980"  },
    }
};

var parameters = new Dictionary<string, object>
{
    ["companyName"] = "Acme Corp",
    ["period"]      = "Q1 2026",
};

Document doc = DocumentSchemaExtensions.FromSchemaYaml(yaml, dataSources, parameters);

doc.GeneratePdf("report.pdf");       // save to disk
byte[] xlsx = doc.GenerateExcel();   // return bytes
string html = doc.GenerateHtml();    // full HTML document
```

**Expected output:**
- PDF: A4 portrait document with header containing the title, one table with two rows and an alternating border, footer with "Confidential".
- Excel: One sheet; header renders as first row, table data follows, footer as last row.
- HTML: Self-contained HTML document using table-based inline-style layout.

---

## 5. Binding expression syntax

Template bindings use double-brace syntax. They are resolved at import time (before rendering).

### Syntax

```
{{ expression }}
{{ expression | pipe }}
{{ expression | pipe(argument) }}
```

### Contexts

| Context | Available bindings |
|---------|-------------------|
| Any text `value` in a page or header/footer | `parameters.name` |
| `table` column values | `row.fieldName` (current data row) |
| `repeat` `itemTemplate` | `row.fieldName` (current data row) |
| `metadata.title` | `parameters.name` |

### Pipe functions

| Pipe | Input type | Example | Output |
|------|-----------|---------|--------|
| `upper` | string | `{{ row.name \| upper }}` | `"north"` → `"NORTH"` |
| `lower` | string | `{{ row.name \| lower }}` | `"NORTH"` → `"north"` |
| `trim` | string | `{{ row.label \| trim }}` | `"  hi  "` → `"hi"` |
| `currency` | numeric | `{{ row.amount \| currency }}` | `1200` → `"$1,200.00"` |
| `number(fmt)` | numeric | `{{ row.rate \| number(P1) }}` | `0.145` → `"14.5%"` |
| `date(fmt)` | DateTime / string | `{{ row.date \| date(yyyy-MM) }}` | → `"2026-01"` |

`fmt` for `number` follows standard .NET numeric format strings. `fmt` for `date` follows standard .NET date format strings.

### Unresolved or null fields

If a field does not exist on the row object or is `null`, the expression resolves to an empty string. No exception is thrown.

---

## 6. Renderer output formats

### `GeneratePdf()` / `GeneratePdf(path)` / `GeneratePdf(stream)`

- Returns `byte[]`, writes to path, or writes to stream.
- Full layout fidelity: precise positioning, text wrapping, multi-page flow.
- All element types supported.

### `GenerateExcel()` / `GenerateExcel(path)` / `GenerateExcel(stream)`

- Each `container.Page(...)` produces one Excel sheet.
- A `PageBreak` inside a page also creates a new sheet.
- `Row` elements map to multiple horizontal columns; `Column` elements stack vertically.
- Relative item widths become proportional column widths; fixed item widths become fixed column widths.
- Images are **not** embedded in Excel output.
- Page size and margin settings are ignored (Excel has no physical page size at render time).

### `GenerateHtml(path)` / `GenerateHtml(stream)` / `GenerateHtml()`

- Returns a complete HTML document (`<!DOCTYPE html>` … `</html>`).

### `GenerateHtmlFragment()`

- Returns an inline-style HTML table fragment suitable for embedding in email bodies.
- Has no `<html>`, `<head>`, or `<body>` tags.
- Use `HtmlRendererOptions` to configure `MaxWidth`, `FontFamily`, `OutlookCompatible`, and `PageDividerStyle`.

```csharp
using FluentReport.Html;

string fragment = doc.GenerateHtmlFragment(new HtmlRendererOptions
{
    MaxWidth = 600,
    FontFamily = "Arial, Helvetica, sans-serif",
    OutlookCompatible = true,   // adds MSO namespace and VML compat for Outlook
});
```

---

## 7. Page sizes and units

| Constant | Width × Height (pt) |
|----------|---------------------|
| `PageSizes.A4` | 595.28 × 841.89 |
| `PageSizes.A3` | 841.89 × 1190.55 |
| `PageSizes.A5` | 419.53 × 595.28 |
| `PageSizes.Letter` | 612 × 792 |
| `PageSizes.Legal` | 612 × 1008 |

Use `.Landscape()` to swap width and height, or `page.Size(width, height)` for a custom size.

**All dimensions are in points.** To convert:
- mm → pt: multiply by `2.835`
- cm → pt: multiply by `28.35`
- inches → pt: multiply by `72`

---

## 8. Common errors and how to fix them

| Error | Cause | Fix |
|-------|-------|-----|
| `ArgumentException` (no pages) | `Document.Create` with no `container.Page(...)`, or schema `pages: []` | Add at least one page |
| `NotSupportedException` (schema version) | `schemaVersion` value other than `1` | Set `schemaVersion: 1` |
| `InvalidOperationException` containing a style id | `styleRef` references a style not in `styles` | Add the style to `styles` or fix the `styleRef` |
| `InvalidOperationException` containing a group id | `groupRef` references a group not in `definitions.groups` | Add the group definition or fix the `groupRef` |
| `InvalidOperationException` containing a datasource name | `dataSource` referenced in a node but not provided at import | Pass the datasource in the `dataSources` dictionary |
| `InvalidOperationException` containing a definition id | `definitionRef` references an entry not in `definitions.repeatables` | Add the definition or fix the reference |
| `InvalidOperationException` containing `"unsupportedType"` | `type` value not recognized | Use only: `text`, `line`, `spacer`, `pageBreak`, `image`, `table`, `repeat`, `groupInstance` |
| `InvalidOperationException: Invalid text color` | Color hex format invalid | Use `#RRGGBB` format (six hex digits, no shorthand) |
| `InvalidOperationException: Unsupported image source mode` | `image.source.mode` is not `path`, `base64`, or `bytes` | Use one of the three supported modes |
| `InvalidOperationException` containing `"base64"` | Image value is not valid base64 | Validate and re-encode the image bytes |
| Page numbers show as static text | Used `Text("Page " + x)` instead of dynamic spans | Use `Text(x => { x.Span("Page "); x.CurrentPageNumber(); })` |
| Rotated text overlaps neighbors | `TextBuilder.Rotate()` is render-only; layout does not account for rotation | Apply rotation only to decorative text; do not rely on it for layout spacing |
| YAML parse error with tab indentation | Tabs in raw C# YAML strings become content | Use spaces for indentation in YAML, or use `FromSchemaJson` instead |

---

## 9. RDLC import

```csharp
using FluentReport.Rdlc;

// From file
Document doc = DocumentRdlcExtensions.FromRdlc(
    "reports/invoice.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["dsInvoiceDataSource"] = new[] { invoiceModel }
    }
);

// From XML string
Document doc = DocumentRdlcExtensions.FromRdlcXml(
    rdlcXmlContent,
    datasets: datasetsDict,
    parameters: new Dictionary<string, object> { ["ReportTitle"] = "Invoice" }
);

byte[] pdf = doc.GeneratePdf();
```

**RDLC conventions:**
- Dataset names must match the `DataSetName` attribute in the RDLC XML exactly.
- Parameters are resolved at parse time, not render time.
- Supported expressions: `=Fields!Name.Value`, `=Parameters!Name.Value`, `IIF(cond, a, b)`, `Switch(...)`.
- Other RDLC expressions (aggregates, functions) resolve to empty string.
- An RDLC table without a matching dataset renders as an empty table (no exception).

---

## 10. Additional schema patterns

### Repeat / list with row template

```yaml
definitions:
  repeatables:
    - id: item-list
      type: repeat
      dataSource: items
      itemTemplate: "{{ row.name }} — {{ row.price | currency }}"
      itemGap: 6
      growthMode: grow
      overflowMode: nextPage

pages:
  - id: p1
    regions:
      content:
        nodes:
          - id: list
            type: repeat
            dataSource: items
            definitionRef: item-list
```

```csharp
var dataSources = new Dictionary<string, IEnumerable<object>>
{
    ["items"] = new[]
    {
        new { name = "Widget A", price = 9.99m },
        new { name = "Widget B", price = 14.99m },
    }
};
var doc = DocumentSchemaExtensions.FromSchemaYaml(yaml, dataSources);
```

### Group definition and instance

Groups let you reuse a set of nodes in multiple locations. The group is defined once, then placed by reference.

```yaml
definitions:
  groups:
    - id: branding
      nodes:
        - id: logo-text
          type: text
          value: "Acme Corp"
          bold: true
          fontSize: 16
          frame: { x: 0, y: 0, width: 200, height: 30 }
          zIndex: 0
        - id: tagline
          type: text
          value: "Quality Products Since 1990"
          fontSize: 9
          frame: { x: 0, y: 34, width: 200, height: 20 }
          zIndex: 1

pages:
  - id: p1
    regions:
      header:
        nodes:
          - id: brand
            type: groupInstance
            groupRef: branding
            frame: { x: 40, y: 10, width: 200, height: 60 }
            zIndex: 0
```

### Multi-page document

```yaml
pages:
  - id: p1
    regions:
      content:
        nodes:
          - id: cover-title
            type: text
            value: "Annual Report 2026"
            fontSize: 24
            bold: true
            align: center
          - id: break
            type: pageBreak

  - id: p2
    regions:
      content:
        nodes:
          - id: data-section
            type: text
            value: "Financial Data"
            fontSize: 14
            bold: true
```

Each entry in `pages[]` maps to a separate page section in PDF and HTML; in Excel, each entry produces a separate sheet.

### HTML fragment with email options

```csharp
using FluentReport.Html;

string fragment = doc.GenerateHtmlFragment(new HtmlRendererOptions
{
    MaxWidth = 600,                                         // outer table max-width in px
    FontFamily = "Arial, Helvetica, sans-serif",
    OutlookCompatible = true,                               // adds MSO namespace VML compat
    PageDividerStyle = "border-top: 1px solid #CCCCCC; padding-top: 12px;"
});
```

The fragment is a `<table>` element with inline styles — no `<html>`, `<head>`, or `<body>` wrapper. Embed it directly in the `<body>` of a transactional email.

### Fluent API — List with inline template

```csharp
var items = new[] { "First item", "Second item", "Third item" };

col.Item().List(items, (cb, item) =>
{
    cb.Text($"• {item}").FontSize(11);
}, spacing: 4f);
```

### Fluent API — Subreport embedding

```csharp
Document header = Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(0);
        page.Content().Text("Company Letterhead").FontSize(14).Bold();
    });
});

Document main = Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);
        page.Content().Column(col =>
        {
            col.Item().Subreport(header);   // embed header document inline
            col.Item().Spacer(12);
            col.Item().Text("Report body goes here");
        });
    });
});
```

---

## 11. Further reading

- [API reference](api.md) — all builders and methods with signatures
- [YAML schema reference](schema/report-schema.md) — full normative schema specification
- [JSON Schema validator](schema/report-schema.schema.json) — machine-readable validation rules
- [FluentReport.Schema README](../src/FluentReport.Schema/README.md) — schema import API details
- [RDLC limitations](rdlc-limitations.md) — known RDLC constraints and expression support
