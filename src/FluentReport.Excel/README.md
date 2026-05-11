# FluentReport.Excel

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Excel.svg?label=FluentReport.Excel)](https://www.nuget.org/packages/FluentReport.Excel)

**Excel (.xlsx)** renderer for FluentReport. Generates Excel spreadsheets directly from the same fluent API used for PDF, using **ClosedXML** as the writing engine.

## Installation

```shell
dotnet add package FluentReport.Excel
```

> Also requires `FluentReport` (installed automatically as a transitive dependency).

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("Sales Summary").FontSize(18).Bold().AlignCenter();

        page.Content().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(1);
                cols.RelativeColumn(1);
            });
            table.BorderEachCell(1);

            table.Header(h =>
            {
                h.Cell().Background("#4472C4").Padding(4).Text("Region").Bold().Color("#FFFFFF");
                h.Cell().Background("#4472C4").Padding(4).Text("Units").Bold().Color("#FFFFFF");
                h.Cell().Background("#4472C4").Padding(4).Text("Revenue").Bold().Color("#FFFFFF");
            });

            table.Cell().Padding(4).Text("North");
            table.Cell().Padding(4).Text("1.200");
            table.Cell().Padding(4).Text("$48.000");
        });
    });
})
.GenerateExcel("report.xlsx");
```

## Generation API

| Method | Description |
|--------|-------------|
| `.GenerateExcel(filePath)` | Saves the `.xlsx` to disk |
| `.GenerateExcel(stream)` | Writes the `.xlsx` to a `Stream` |
| `.GenerateExcel()` | Returns the `.xlsx` as `byte[]` |

## Element behavior

| Element | Behavior in Excel |
|----------|------------------------|
| `Text(...)` | Formatted cell (bold, color, size, alignment) |
| `Column(...)` | Stacks elements in consecutive rows |
| `Row(...)` | Places elements in columns within the same row range |
| `Table(...)` | Rows and columns proportional to definition |
| `Header` / `Footer` | At the start and end of the worksheet |
| `Line(...)` | Bottom border on the current row |
| `PageBreak()` | Creates a new worksheet in the workbook |
| `Background(...)` | Cell background color |
| `Border(...)` | Cell border |
| `AlignCenter()` / `AlignRight()` | Horizontal alignment |
| `Padding(...)` | Ignored (Excel has no per-cell padding) |
| `Spacer(...)` | Empty row |
| `Image(...)` | Not supported |

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Model and fluent API |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer |
| `FluentReport.Excel` | Excel renderer - this package |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
