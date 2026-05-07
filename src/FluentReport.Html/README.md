# FluentReport.Html

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Html.svg?label=FluentReport.Html)](https://www.nuget.org/packages/FluentReport.Html)

Renderer **HTML / email** para FluentReport. Genera HTML estático o fragmentos listos para embeber en emails transaccionales, usando el mismo fluent API que PDF y Excel. No requiere SkiaSharp.

## Instalación

```shell
dotnet add package FluentReport.Html
```

## Uso rápido

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
            .Text("Factura #001").FontSize(18).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Cliente: Empresa S.A.").FontSize(12);
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
                    h.Cell().Background("#EEEEEE").Padding(6).Text("Descripción").Bold();
                    h.Cell().Background("#EEEEEE").Padding(6).Text("Total").Bold();
                });
                table.Cell().Padding(6).Text("Servicio de consultoría");
                table.Cell().Padding(6).Text("$5.000");
            });
        });
    });
});

// HTML completo (<html>…</html>)
doc.GenerateHtml("factura.html");

// Fragmento para embeber en un email
string fragment = doc.GenerateHtmlFragment();
await emailService.SendAsync(to, subject, htmlBody: fragment);
```

## API de generación

| Método | Descripción |
|--------|-------------|
| `.GenerateHtml(filePath)` | Guarda el HTML completo en disco |
| `.GenerateHtml(stream)` | Escribe el HTML en un `Stream` |
| `.GenerateHtml()` | Devuelve el HTML completo como `string` |
| `.GenerateHtmlFragment()` | Devuelve solo la tabla exterior, sin `<html>`/`<body>` |

## Opciones

```csharp
var options = new HtmlRendererOptions
{
    InlineStyles = true,   // por defecto: true  — estilos inline para máxima compatibilidad con clientes de email
    MaxWidthPx   = 800     // ancho máximo del contenedor en píxeles
};
doc.GenerateHtml("reporte.html", options);
```

## Paquetes del ecosistema

| Paquete | Función |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Modelo y API fluent |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | Renderer PDF |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Renderer Excel |
| `FluentReport.Html` | Renderer HTML / email — este paquete |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | Importador RDLC / SSRS |

> 📖 Documentación completa en el [repositorio](https://github.com/MathiasGonzalez/FluentReport).
