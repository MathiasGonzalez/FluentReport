# FluentReport

[![NuGet](https://img.shields.io/nuget/v/FluentReport.svg?label=FluentReport)](https://www.nuget.org/packages/FluentReport)

**PDF** renderer for FluentReport: build reports with a fluent C# API and render them to PDF (or PNG) with SkiaSharp. Works on Windows, macOS and Linux.

> Using an AI coding assistant? Point it to [llms-full.txt](https://mathiasgonzalez.github.io/FluentReport/llms-full.txt): the whole API and usage guide in one file.

## Installation

```shell
dotnet add package FluentReport
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;

Document.Create(c => c.Page(page =>
{
    page.Size(PageSizes.A4);
    page.MarginAll(40);

    page.Header().Text("Sales report").FontSize(18).Bold();

    page.Content().Table(table =>
    {
        table.ColumnsDefinition(cols => { cols.RelativeColumn(3); cols.ConstantColumn(80); });
        table.Header(h =>
        {
            h.Cell().Background("#1F3864").Padding(4).Text("Product").Bold().Color("#FFFFFF");
            h.Cell().Background("#1F3864").Padding(4).Text("Price").Bold().Color("#FFFFFF");
        });
        foreach (var p in products)
        {
            table.Cell().Padding(3).Text(p.Name);
            table.Cell().Padding(3).Text(p.Price.ToString("N2")).AlignRight();
        }
        table.Footer(f =>
        {
            f.Cell().Padding(4).Text("Total").Bold();
            f.Cell().Padding(4).Text(total.ToString("N2")).Bold().AlignRight();
        });
    });

    page.Footer().AlignRight().Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); })
        .FontSize(8);
}))
.GeneratePdf("report.pdf");
```

## Pagination

Content flows across as many pages as it needs, with the header and footer repeated on each page:

- columns split between items, also when nested or wrapped in padding, background or border;
- text splits between lines;
- tables split between rows, repeat their header rows and keep footer rows on the last page;
- `PageBreak()` starts a new page at any depth (leading, repeated and trailing breaks add no blank pages).

Rows, images, charts and canvases are not split: they move to the next page, and one taller than a whole page is cut at the footer.

## API

| Method | Description |
|--------|-------------|
| `.GeneratePdf(path\|stream\|—, options?)` | PDF to a file, a stream or a `byte[]` |
| `.GenerateImages(scale = 1, options?)` | One PNG per page (`scale` = pixels per point) |
| `.GetPdfPageCount(options?)` | Number of pages |
| `container.Canvas(width, height, (SKCanvas canvas, SKRect rect) => …)` | Custom vector drawing (PDF only) |

`PdfOptions`: `FontResolver` (e.g. embedded fonts; returned typefaces are cached and never disposed), `Title`, `Author`, `MaxImagePixels` (largest image decoded, 100 MP by default; larger images throw instead of exhausting memory).

```csharp
var regular = SKTypeface.FromFile("fonts/Inter-Regular.ttf");
var bold = SKTypeface.FromFile("fonts/Inter-Bold.ttf");
doc.GeneratePdf("report.pdf", new PdfOptions
{
    FontResolver = style => style.EffectiveBold ? bold : regular,
    Title = "Sales report"
});
```

Fonts are embedded whole (SkiaSharp does not subset them), which adds a few hundred KB per font face to each PDF.

Without a `FontResolver`, text uses the requested system font. When the system has no usable font (common on Linux and in containers without fonts installed), the package falls back to its bundled **DejaVu Sans** (regular and bold; italic is synthesized), so text is never rendered empty. For consistent output on every server, pass your own fonts with `FontResolver`. DejaVu Sans is distributed under its own permissive license, included in the package as `LICENSE-DejaVu.txt`.

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| `FluentReport` | PDF renderer (this package) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Document model and fluent API |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
