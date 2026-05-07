# FluentReport

[![CI](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FluentReport.svg?label=FluentReport)](https://www.nuget.org/packages/FluentReport)

Renderer **PDF** para FluentReport. Usa **SkiaSharp** como motor de renderizado y genera archivos PDF multipágina con soporte completo del layout fluent. Funciona en Linux sin dependencias nativas adicionales.

## Instalación

```shell
dotnet add package FluentReport
```

## Uso rápido

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
            .Text("Mi Reporte").FontSize(20).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Introducción").FontSize(14).Bold();
            col.Item().Text("Contenido del reporte.");
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
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Producto").Bold();
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Precio").Bold();
                });
                table.Cell().Padding(5).Text("Producto A");
                table.Cell().Padding(5).Text("$100");
            });
        });

        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    });
})
.GeneratePdf("reporte.pdf");
```

## API de generación

| Método | Descripción |
|--------|-------------|
| `.GeneratePdf(filePath)` | Genera el PDF y lo guarda en disco |
| `.GeneratePdf(stream)` | Escribe el PDF en un `Stream` |
| `.GeneratePdf()` | Devuelve el PDF como `byte[]` |
| `.GenerateImages(scale)` | Renderiza cada página como PNG (`byte[][]`) |

## Personalizar fuentes

```csharp
using FluentReport.Rendering;
using SkiaSharp;

// Usar una fuente personalizada para todo el documento
SkiaFonts.TypefaceFactory = style =>
    SKTypeface.FromFile(style.Bold ? "fonts/MyFont-Bold.ttf" : "fonts/MyFont.ttf");
```

## Paquetes del ecosistema

| Paquete | Función |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Modelo y API fluent (sin deps de render) |
| `FluentReport` | Renderer PDF — este paquete |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Renderer Excel (ClosedXML) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | Renderer HTML / email |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | Importador RDLC / SSRS |

> 📖 Documentación completa y todos los ejemplos en el [repositorio](https://github.com/MathiasGonzalez/FluentReport).
