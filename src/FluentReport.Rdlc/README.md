# FluentReport.Rdlc

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

**RDLC / SSRS** importer for FluentReport. Converts `.rdlc` files (Visual Studio Report Designer / SQL Server Reporting Services) into FluentReport documents that render to **HTML** or **Excel**, without SSRS or ReportViewer.

## Installation

```shell
dotnet add package FluentReport.Rdlc
dotnet add package FluentReport.Html    # and/or FluentReport.Excel
```

## Quick start

```csharp
using System.Globalization;
using FluentReport;
using FluentReport.Html;
using FluentReport.Rdlc;

var doc = Document.FromRdlc(
    "reports/invoice.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Lines"] = lines.Cast<object>()          // POCOs or IDictionary<string, object>
    },
    parameters: new Dictionary<string, object> { ["Customer"] = "Cliente S.A." },
    options: new RdlcImportOptions { Culture = new CultureInfo("es-UY") });

doc.GenerateHtml("invoice.html");
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

Textboxes (with paragraphs and runs), lines, images (embedded, database, external), tablix tables (header, detail and footer rows, column spans), page header/footer, page size and margins. Expressions support field/parameter/global references, VB operators, `IIF`/`Switch`, `Format`, common conversion and text functions, and dataset aggregates.

Full list and limitations: [rdlc-limitations.md](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/rdlc-limitations.md).

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Document model and fluent API |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer |
| `FluentReport.Rdlc` | RDLC / SSRS importer (this package) |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
