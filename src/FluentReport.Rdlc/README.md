# FluentReport.Rdlc

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

**RDLC / SSRS** importer for FluentReport. Converts `.rdlc` files (Visual Studio Report Designer / SQL Server Reporting Services) into FluentReport documents that render to **PDF**, **HTML** or **Excel**, without SSRS or ReportViewer.

> Using an AI coding assistant? Point it to [llms-full.txt](https://mathiasgonzalez.github.io/FluentReport/llms-full.txt): the whole API and usage guide in one file.

## Installation

```shell
dotnet add package FluentReport.Rdlc
dotnet add package FluentReport         # PDF (and/or FluentReport.Html, FluentReport.Excel)
```

## Quick start

```csharp
using System.Globalization;
using FluentReport;
using FluentReport.Rdlc;

var doc = Document.FromRdlc(
    "reports/invoice.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Lines"] = lines.Cast<object>()          // POCOs or IDictionary<string, object>
    },
    parameters: new Dictionary<string, object> { ["Customer"] = "Cliente S.A." },
    options: new RdlcImportOptions { Culture = new CultureInfo("es-UY") });

doc.GeneratePdf("invoice.pdf");
```

## Entry points

| Method | Source |
|--------|--------|
| `Document.FromRdlc(path, datasets?, parameters?, globals?, options?)` | File (external images resolve from its folder) |
| `Document.FromRdlcStream(stream, …)` | `Stream` |
| `Document.FromRdlcXml(xml, …)` | XML `string` |

To see what could not be imported, use the factory and read its warnings:

```csharp
var factory = new RdlcDocumentFactory(datasets, parameters, options: options);
var doc = factory.ParseFromFile("reports/invoice.rdlc");
foreach (var warning in factory.Warnings)
    logger.LogWarning(warning);
```

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `Culture` | report `<Language>`, else invariant | Formats numbers/dates and reads numbers given as text. The machine's regional settings are never used. |
| `LoadExternalImages` | `true` | Load images with `Source = External`. |
| `ExternalImageDirectory` | the `.rdlc` folder (file import only) | External images are read only from inside this directory; streams and strings load none unless it is set. |
| `StrictExpressions` | `false` | Throw on unsupported expressions instead of rendering empty text. |

## What is supported

Textboxes (with paragraphs and runs, `Format`, borders), rectangles, lines, images (embedded, database, external), tablix tables and lists (row groups with group headers and subtotals, sorting, filters, hidden rows/columns, column spans), visibility, page header/footer with page numbers, page size and margins. Expressions support field/parameter/global references, VB operators, `IIF`/`Switch`, `Format`, common conversion and text functions, aggregates scoped to groups, `RowNumber`, `RunningValue` and `Previous`.

Aggregates follow SSRS scoping: without a scope argument, `Sum(...)` covers the current detail row, group or table; pass a group, table or dataset name (`Sum(Fields!Total.Value, "Lines")`) to aggregate over it. `Globals!PageNumber` and `TotalPages` render the real page numbers.

### Changes in 2.1

- Row groups render a header and subtotal per group instead of repeating them per row.
- An aggregate without a scope in a detail row now covers that row (it used to cover the whole dataset). Name the dataset to keep a grand total.
- `First(x, "Dataset")` reads the named dataset instead of the current row.
- `Rectangle`, `Format`, borders, `Hidden`, filters and `RowNumber` / `RunningValue` / `Previous` are imported; they used to be skipped or reported as unsupported.

Report definitions from users can be rendered safely: DTDs and `<Code>` are never processed, external images stay inside `ExternalImageDirectory`, and very deep or long expressions fail with a warning instead of crashing. See [SECURITY.md](https://github.com/MathiasGonzalez/FluentReport/blob/main/SECURITY.md).

Full list and limitations: [rdlc-limitations.md](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/rdlc-limitations.md).

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Document model and fluent API |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
