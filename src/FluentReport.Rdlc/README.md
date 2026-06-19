# FluentReport.Rdlc

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

**RDLC / SSRS** importer for FluentReport. Converts `.rdlc` files (Visual Studio Report Designer / SQL Server Reporting Services) into FluentReport documents, allowing them to be rendered as **PDF**, **Excel**, or **HTML** without SSRS dependencies.

## Installation

```shell
dotnet add package FluentReport.Rdlc
```

## Quick start

```csharp
using FluentReport.Rdlc;

// From a file on disk
var doc = DocumentRdlcExtensions.FromRdlc(
    "reports/catalog.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Products"] = products.Cast<object>()
    },
    parameters: new Dictionary<string, object>
    {
        ["Company"] = "Acme Corp."
    });

doc.GeneratePdf("catalog.pdf");

// From a Stream (useful with embedded resources)
using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.Reports.catalog.rdlc")!;
var doc2 = DocumentRdlcExtensions.FromRdlcStream(stream, datasets, parameters);
doc2.GenerateExcel("catalog.xlsx");

// From an XML string
var doc3 = DocumentRdlcExtensions.FromRdlcXml(xmlString, datasets, parameters);
```

## API

| Method | Description |
|--------|-------------|
| `FromRdlc(path, datasets, parameters, globals)` | Imports from file |
| `FromRdlcStream(stream, datasets, parameters, globals)` | Imports from `Stream` |
| `FromRdlcXml(xml, datasets, parameters, globals)` | Imports from XML `string` |

## Supported RDLC elements

| RDLC element | FluentReport equivalent |
|---------------|--------------------------|
| `Textbox` | `TextElement` |
| `Line` | `LineElement` |
| `Image` | `ImageElement` |
| `Tablix` | `TableElement` (with data and `ColSpan`) |
| `PageHeader` / `PageFooter` | Page header / footer |
| Margins and page size | `PageSettings` |

## Supported expressions

| Expression | Result |
|-----------|-----------|
| `=Fields!FieldName.Value` | Field value from the dataset |
| `=First(Fields!FieldName.Value, "DataSetName")` | Field value from the first row of the named dataset |
| `=Parameters!ParamName.Value` | Parameter value |
| `=Globals!Name.Value` | Global variable (supply via `globals` parameter) |
| `=IIF(condition, trueValue, falseValue)` | Conditional expression |
| `=Switch(cond1, val1, cond2, val2, ...)` | Multi-branch conditional expression |
| `=Format(expr, "format")` | Value formatted with a .NET / VB.NET format string |
| `=Sum(Fields!X.Value, "DataSet")` | Sum of a numeric field over a dataset |
| `=Count(Fields!X.Value, "DataSet")` | Count of non-empty values |
| `=Avg(Fields!X.Value, "DataSet")` | Average of a numeric field |
| `=Min(Fields!X.Value, "DataSet")` | Minimum value |
| `=Max(Fields!X.Value, "DataSet")` | Maximum value |
| `=CountRows("DataSet")` | Total row count of a dataset |
| `=expr1 & expr2` | String concatenation |
| Condition operators `=`, `<>`, `>`, `<`, `>=`, `<=` | Comparisons in `IIF` / `Switch` conditions |
| Literal (without `=`) | Static text |

Unsupported/unknown expressions still resolve to an empty string.

> Full documentation and limitations: [`docs/api.md#rdlc-import`](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/api.md#rdlc-import) · [`docs/rdlc-limitations.md`](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/rdlc-limitations.md)

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Model and fluent API |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel renderer |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| `FluentReport.Rdlc` | RDLC importer - this package |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
