# FluentReport

[![CI](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FluentReport.svg?label=FluentReport)](https://www.nuget.org/packages/FluentReport)

**PDF** renderer for FluentReport. It uses **SkiaSharp** as the rendering engine and generates multi-page PDF files with full fluent layout support. It works on Linux without extra native dependencies.

## Installation

```shell
dotnet add package FluentReport
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;

Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("My Report").FontSize(20).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Introduction").FontSize(14).Bold();
            col.Item().Text("Report content.");
            col.Item().Line(1);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.ConstantColumn(80);
                });
                table.Header(h =>
                {
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Product").Bold();
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Price").Bold();
                });
                table.Cell().Padding(5).Text("Product A");
                table.Cell().Padding(5).Text("$100");
            });
        });

        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    });
})
.GeneratePdf("report.pdf");
```

## Generation API

| Method | Description |
|--------|-------------|
| `.GeneratePdf(filePath)` | Generates the PDF and saves it to disk |
| `.GeneratePdf(stream)` | Writes the PDF to a `Stream` |
| `.GeneratePdf()` | Returns the PDF as `byte[]` |
| `.GenerateImages(scale)` | Renders each page as PNG (`IReadOnlyList<byte[]>`) |

## Customize fonts

```csharp
using FluentReport.Rendering;
using SkiaSharp;

// Use a custom font for the whole document
SkiaFonts.TypefaceFactory = style =>
    SKTypeface.FromFile(style.Bold ? "fonts/MyFont-Bold.ttf" : "fonts/MyFont.ttf");
```

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Model and fluent API (without rendering deps) |
| `FluentReport` | PDF renderer - this package |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel renderer (ClosedXML) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |

> Full documentation and all examples are available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
