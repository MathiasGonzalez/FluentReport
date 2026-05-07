# FluentReport.Html

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Html.svg?label=FluentReport.Html)](https://www.nuget.org/packages/FluentReport.Html)

**HTML / email** renderer for FluentReport. Generates static HTML or fragments ready to embed in transactional emails, using the same fluent API as PDF and Excel. It does not require SkiaSharp.

## Installation

```shell
dotnet add package FluentReport.Html
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;
using FluentReport.Html;

var doc = Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("Invoice #001").FontSize(18).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Customer: Company Inc.").FontSize(12);
            col.Item().Line(1);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.ConstantColumn(80);
                });
                table.BorderEachCell(1);
                table.Header(h =>
                {
                    h.Cell().Background("#EEEEEE").Padding(6).Text("Description").Bold();
                    h.Cell().Background("#EEEEEE").Padding(6).Text("Total").Bold();
                });
                table.Cell().Padding(6).Text("Consulting service");
                table.Cell().Padding(6).Text("$5.000");
            });
        });
    });
});

// Full HTML (<html>...</html>)
doc.GenerateHtml("invoice.html");

// Fragment to embed in an email
string fragment = doc.GenerateHtmlFragment();
await emailService.SendAsync(to, subject, htmlBody: fragment);
```

## Generation API

| Method | Description |
|--------|-------------|
| `.GenerateHtml(filePath)` | Saves the full HTML to disk |
| `.GenerateHtml(stream)` | Writes the HTML to a `Stream` |
| `.GenerateHtml()` | Returns the full HTML as `string` |
| `.GenerateHtmlFragment()` | Returns only the outer table, without `<html>`/`<body>` |

## Options

```csharp
var options = new HtmlRendererOptions
{
    InlineStyles = true,   // default: true - inline styles for maximum email client compatibility
    MaxWidthPx   = 800     // maximum container width in pixels
};
doc.GenerateHtml("report.html", options);
```

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Model and fluent API |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel renderer |
| `FluentReport.Html` | HTML / email renderer - this package |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
