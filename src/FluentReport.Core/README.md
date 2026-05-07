# FluentReport.Core

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Core.svg?label=FluentReport.Core)](https://www.nuget.org/packages/FluentReport.Core)

Modelos, elementos y builders de **FluentReport** sin ninguna dependencia de renderizado. Diseñado para proyectos que quieren compartir la misma definición de documento entre múltiples renderers (PDF, Excel, HTML) o construir renderers propios.

## Instalación

```shell
dotnet add package FluentReport.Core
```

## ¿Qué incluye?

- `Document` / `DocumentSettings` — modelo central del documento
- `PageBuilder`, `ColumnBuilder`, `RowBuilder`, `TableBuilder`, `TextBuilder`, `ChartBuilder` — API fluent
- Elementos: `TextElement`, `TableElement`, `ImageElement`, `ChartElement`, `SubreportElement`, `ColumnElement`, `RowElement`, `BorderElement`, `LineElement`, `PaddingElement`, `AlignElement`, `ListElement`, `SpacerElement`, `PageBreakElement`
- `TextStyle`, `ReportColor`, `BorderStyle` — tipos de estilo
- `MeasureContext` / `RenderContext` — contextos de layout
- `ITextMeasurer` / `IDrawingCanvas` — interfaces de abstracción del renderer

## Implementar un renderer propio

```csharp
public class MyCanvas : IDrawingCanvas
{
    public float MeasureText(string text, TextStyle style) => /* ... */ 0;
    public List<string> WrapText(string text, TextStyle style, float maxWidth) => new() { text };
    public float MeasureText(string text, float fontSize, string? fontFamily = null) => 0;
    public void DrawText(string text, float x, float y, DrawTextAlign align, TextStyle style) { /* ... */ }
    public void DrawFilledRect(float x, float y, float w, float h, ReportColor color) { /* ... */ }
    // ... resto de métodos de IDrawingCanvas
}

var doc = Document.Create(c => { c.Page(p => { p.Size(PageSizes.A4); p.Content().Text("Hola"); }); });

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

## Paquetes del ecosistema

| Paquete | Función |
|---------|---------|
| `FluentReport.Core` | Modelo y API fluent (este paquete) |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | Renderer PDF (SkiaSharp) |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Renderer Excel (ClosedXML) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | Renderer HTML / email |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | Importador RDLC / SSRS |

> 📖 Documentación completa en el [repositorio](https://github.com/MathiasGonzalez/FluentReport).
