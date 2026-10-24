# FluentReport

![FluentReport Logo](logo.png)

[![CI](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml)
[![Publish to NuGet](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml)
[![NuGet FluentReport.Core](https://img.shields.io/nuget/v/FluentReport.Core.svg?label=FluentReport.Core)](https://www.nuget.org/packages/FluentReport.Core)
[![NuGet FluentReport.Html](https://img.shields.io/nuget/v/FluentReport.Html.svg?label=FluentReport.Html)](https://www.nuget.org/packages/FluentReport.Html)
[![NuGet FluentReport.Excel](https://img.shields.io/nuget/v/FluentReport.Excel.svg?label=FluentReport.Excel)](https://www.nuget.org/packages/FluentReport.Excel)
[![NuGet FluentReport.Rdlc](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

.NET 10 library for building reports with a fluent C# API and rendering them as **HTML / email** or **Excel**, plus an importer for **RDLC / SSRS** report definitions. No native dependencies.

> **2.0** removed the PDF renderer, the YAML schema importer and the MCP server. See [the migration guide](docs/migration-2.0.md).

## Packages

| Package | Description |
|---------|-------------|
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer (table layout, inline styles) |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer (ClosedXML) |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Document model and fluent API, brought in by the packages above |

```shell
dotnet add package FluentReport.Html
dotnet add package FluentReport.Excel
dotnet add package FluentReport.Rdlc
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;
using FluentReport.Html;

var doc = Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header().Text("My Report").FontSize(20).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Summary").FontSize(14).Bold();
            col.Item().Line(1);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(1); });
                table.Header(h =>
                {
                    h.Cell().Background("#4472C4").Padding(5).Text("Product").Bold().Color("#FFFFFF");
                    h.Cell().Background("#4472C4").Padding(5).Text("Price").Bold().Color("#FFFFFF");
                });
                table.Cell().Padding(5).Text("Widget A");
                table.Cell().Padding(5).Text("9.99").AlignRight();
                table.Footer(f =>
                {
                    f.Cell().Padding(5).Text("Total").Bold();
                    f.Cell().Padding(5).Text("9.99").Bold().AlignRight();
                });
            });
        });

        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages();
        }).FontSize(8);
    });
});

doc.GenerateHtml("report.html");            // full HTML document
string emailBody = doc.GenerateHtmlFragment(); // fragment for an email
doc.GenerateExcel("report.xlsx");
```

### RDLC

```csharp
using System.Globalization;
using FluentReport.Rdlc;

var invoice = Document.FromRdlc(
    "reports/invoice.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>> { ["Lines"] = lines.Cast<object>() },
    options: new RdlcImportOptions { Culture = new CultureInfo("es-UY") });

invoice.GenerateHtml("invoice.html");
```

## Renderer support

| Feature | HTML | Excel |
|---------|:----:|:-----:|
| Text, fonts, colors, alignment | ✓ | ✓ |
| Rich text (spans) and page numbers | ✓ | ✓ |
| Column / Row layout | ✓ | ✓ (merged cells) |
| Tables: header, footer, column spans, borders | ✓ | ✓ |
| Lists | ✓ | ✓ |
| Images (PNG, JPEG, GIF, WebP, BMP) | ✓ (data URI) | — |
| Backgrounds, borders, padding | ✓ | ✓ (no padding) |
| Lines and spacers | ✓ | ✓ |
| Multiple pages / page breaks | ✓ (divider) | ✓ (worksheets) |

Output does not depend on the machine's regional settings: CSS numbers always use `.`, and the RDLC importer formats values with an explicit culture.

## Documentation

- [API reference](docs/api.md) — builders, elements, renderer options, RDLC options
- [RDLC support and limitations](docs/rdlc-limitations.md)
- [Migration to 2.0](docs/migration-2.0.md)
- [UY fiscal document samples](docs/uy-fiscal-samples.md) — salary slip, delivery note, payment receipt
- Package READMEs: [Core](src/FluentReport.Core/README.md) · [Html](src/FluentReport.Html/README.md) · [Excel](src/FluentReport.Excel/README.md) · [Rdlc](src/FluentReport.Rdlc/README.md)

## Project structure

```
src/
├── FluentReport.Core/     # Document model and fluent API
├── FluentReport.Html/     # HTML / email renderer
├── FluentReport.Excel/    # Excel renderer (ClosedXML)
└── FluentReport.Rdlc/     # RDLC importer
tests/                     # xUnit v3 test projects, one per package
samples/FluentReport.Samples/
docs/
```

## Releasing

Push a tag such as `v2.0.0`; the [NuGet workflow](.github/workflows/nuget.yml) builds, tests and publishes all packages with that version.

## Dependencies

- [ClosedXML](https://www.nuget.org/packages/ClosedXML) 0.105.1 — Excel rendering
