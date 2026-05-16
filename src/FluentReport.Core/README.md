# FluentReport.Core

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Core.svg?label=FluentReport.Core)](https://www.nuget.org/packages/FluentReport.Core)

Models, elements, and builders for **FluentReport** without rendering dependencies. Designed for projects that want to share the same document definition across multiple renderers (PDF, Excel, HTML) or build custom renderers.

## Installation

```shell
dotnet add package FluentReport.Core
```

## What is included?

- `Document` / `DocumentSettings` — central document model
- `PageBuilder`, `ColumnBuilder`, `RowBuilder`, `TableBuilder`, `TextBuilder`, `ChartBuilder` — fluent API
- Elements: `TextElement`, `TableElement`, `ImageElement`, `ChartElement`, `SubreportElement`, `ColumnElement`, `RowElement`, `BorderElement`, `LineElement`, `PaddingElement`, `AlignElement`, `ListElement`, `SpacerElement`, `PageBreakElement`, `CanvasElement`
- `TextStyle`, `ReportColor`, `BorderStyle` — style types
- `MeasureContext` / `RenderContext` — layout contexts
- `ITextMeasurer` / `IDrawingCanvas` — renderer abstraction interfaces

## Implement a custom renderer

```csharp
public class MyCanvas : IDrawingCanvas
{
    public float MeasureText(string text, TextStyle style) => /* ... */ 0;
    public List<string> WrapText(string text, TextStyle style, float maxWidth) => new() { text };
    public float MeasureText(string text, float fontSize, string? fontFamily = null) => 0;
    public void DrawText(string text, float x, float y, DrawTextAlign align, TextStyle style) { /* ... */ }
    public void DrawFilledRect(float x, float y, float w, float h, ReportColor color) { /* ... */ }
    // ... rest of the IDrawingCanvas methods
}

var doc = Document.Create(c => { c.Page(p => { p.Size(PageSizes.A4); p.Content().Text("Hello"); }); });

foreach (var page in doc.Settings.Pages)
{
    var measurer = new MyCanvas();
    var ctx = new RenderContext
    {
        Canvas = measurer,
        AvailableWidth  = page.ContentWidth,
        AvailableHeight = page.ContentHeight,
        CurrentPage = 1, TotalPages = 1
    };
    page.ContentElement?.Render(ctx,
        new Position(page.MarginLeft, page.MarginTop),
        new Size(page.ContentWidth, page.ContentHeight));
}
```

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| `FluentReport.Core` | Model and fluent API (this package) |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer (SkiaSharp) |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel renderer (ClosedXML) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
