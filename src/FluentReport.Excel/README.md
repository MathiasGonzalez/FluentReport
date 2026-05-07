# FluentReport.Excel

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Excel.svg?label=FluentReport.Excel)](https://www.nuget.org/packages/FluentReport.Excel)

Renderer **Excel (.xlsx)** para FluentReport. Genera planillas Excel directamente desde el mismo fluent API que se usa para PDF, usando **ClosedXML** como motor de escritura.

## Instalación

```shell
dotnet add package FluentReport.Excel
```

> Requiere también `FluentReport` (se instala automáticamente como dependencia transitiva).

## Uso rápido

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
            .Text("Resumen de Ventas").FontSize(18).Bold().AlignCenter();

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
                h.Cell().Background("#4472C4").Padding(4).Text("Región").Bold().Color("#FFFFFF");
                h.Cell().Background("#4472C4").Padding(4).Text("Unidades").Bold().Color("#FFFFFF");
                h.Cell().Background("#4472C4").Padding(4).Text("Ingresos").Bold().Color("#FFFFFF");
            });

            table.Cell().Padding(4).Text("Norte");
            table.Cell().Padding(4).Text("1.200");
            table.Cell().Padding(4).Text("$48.000");
        });
    });
})
.GenerateExcel("reporte.xlsx");
```

## API de generación

| Método | Descripción |
|--------|-------------|
| `.GenerateExcel(filePath)` | Guarda el `.xlsx` en disco |
| `.GenerateExcel(stream)` | Escribe el `.xlsx` en un `Stream` |
| `.GenerateExcel()` | Devuelve el `.xlsx` como `byte[]` |

## Comportamiento por elemento

| Elemento | Comportamiento en Excel |
|----------|------------------------|
| `Text(...)` | Celda con formato (negrita, color, tamaño, alineación) |
| `Column(...)` | Apila elementos en filas consecutivas |
| `Row(...)` | Coloca elementos en columnas del mismo rango de filas |
| `Table(...)` | Filas y columnas proporcionales a la definición |
| `Header` / `Footer` | Al inicio y al final del worksheet |
| `Line(...)` | Borde inferior en la fila actual |
| `PageBreak()` | Crea un nuevo worksheet en el workbook |
| `Background(...)` | Color de fondo de celda |
| `Border(...)` | Borde de celda |
| `AlignCenter()` / `AlignRight()` | Alineación horizontal |
| `Padding(...)` | Ignorado (Excel no tiene padding por celda) |
| `Spacer(...)` | Fila vacía |
| `Image(...)` | No soportado |

## Paquetes del ecosistema

| Paquete | Función |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Modelo y API fluent |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | Renderer PDF |
| `FluentReport.Excel` | Renderer Excel — este paquete |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | Renderer HTML / email |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | Importador RDLC / SSRS |

> 📖 Documentación completa en el [repositorio](https://github.com/MathiasGonzalez/FluentReport).
