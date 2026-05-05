# FluentReport

[![CI](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml)
[![Publish to NuGet](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml)
[![NuGet FluentReport](https://img.shields.io/nuget/v/FluentReport.svg?label=FluentReport)](https://www.nuget.org/packages/FluentReport)
[![NuGet FluentReport.Excel](https://img.shields.io/nuget/v/FluentReport.Excel.svg?label=FluentReport.Excel)](https://www.nuget.org/packages/FluentReport.Excel)
[![NuGet FluentReport.Rdlc](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

Librería .NET 10 para generar PDFs y Excel usando una API fluent en C#. Usa SkiaSharp como motor de renderizado PDF y ClosedXML para Excel. Funciona en Linux sin dependencias nativas adicionales.

## Características

- API fluent para construir documentos PDF **y Excel (.xlsx)**
- Motor de layout con cálculo de tamaños (measure/arrange)
- Elementos: texto, columnas, filas, tablas, imágenes, espaciadores, líneas, bordes, padding, salto de página
- Soporte de cabecera y pie de página
- Numeración de páginas dinámica
- Salto de página automático cuando el contenido supera la página
- **Renderer Excel** (`FluentReport.Excel`): genera `.xlsx` directamente desde el mismo fluent API
- **Importador RDLC** (`FluentReport.Rdlc`): convierte archivos `.rdlc` (SSRS) en documentos PDF/Excel
- Compatible con Linux (SkiaSharp.NativeAssets.Linux.NoDependencies)

## Instalación

Instala el paquete principal desde NuGet ([FluentReport](https://www.nuget.org/packages/FluentReport)):

```shell
dotnet add package FluentReport
```

Para generar Excel, instala además el renderer ([FluentReport.Excel](https://www.nuget.org/packages/FluentReport.Excel)):

```shell
dotnet add package FluentReport.Excel
```

Para importar reportes `.rdlc` existentes ([FluentReport.Rdlc](https://www.nuget.org/packages/FluentReport.Rdlc)):

```shell
dotnet add package FluentReport.Rdlc
```

## Uso rápido

```csharp
using FluentReport;
using FluentReport.Core;

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("Mi Reporte")
            .FontSize(20)
            .Bold()
            .AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);

            col.Item().Text("Introducción").FontSize(14).Bold();
            col.Item().Text("Este es el contenido del reporte.");
            col.Item().Line(1);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                    cols.ConstantColumn(80);
                });

                table.Header(h =>
                {
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Nombre").Bold();
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Descripción").Bold();
                    h.Cell().Background("#CCCCCC").Padding(5).Text("Valor").Bold();
                });

                table.Cell().Padding(5).Text("Producto A");
                table.Cell().Padding(5).Text("Descripción del producto A");
                table.Cell().Padding(5).Text("$100");

                table.Cell().Padding(5).Text("Producto B");
                table.Cell().Padding(5).Text("Descripción del producto B");
                table.Cell().Padding(5).Text("$200");
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

## Excel – Uso rápido

Agrega `using FluentReport.Excel;` y usa el mismo fluent API con `GenerateExcel`:

```csharp
using FluentReport;
using FluentReport.Core;
using FluentReport.Excel;

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("Mi Reporte Excel")
            .FontSize(20)
            .Bold()
            .AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);

            col.Item().Text("Resumen de Ventas").FontSize(14).Bold();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                });

                table.Header(h =>
                {
                    h.Cell().Background("#4472C4").Padding(4).Text("Región").Bold().Color("#FFFFFF");
                    h.Cell().Background("#4472C4").Padding(4).Text("Unidades").Bold().Color("#FFFFFF");
                    h.Cell().Background("#4472C4").Padding(4).Text("Ingresos").Bold().Color("#FFFFFF");
                });
                table.BorderEachCell(1);

                table.Cell().Padding(4).Text("Norte");
                table.Cell().Padding(4).Text("1 200");
                table.Cell().Padding(4).Text("$48 000");
            });
        });
    });
})
.GenerateExcel("reporte.xlsx");
```

### Comportamiento del renderer Excel

| Elemento | Comportamiento en Excel |
|----------|------------------------|
| `Text(...)` | Escribe el texto en la celda con formato (negrita, color, tamaño, alineación) |
| `Column(...)` | Apila los elementos verticalmente (filas consecutivas) |
| `Row(...)` | Coloca los elementos en columnas del mismo rango de filas |
| `Table(...)` | Genera filas y columnas de Excel proporcionales a la definición de columnas |
| `Header` / `Footer` | Se escriben al inicio y al final del worksheet respectivamente |
| `Line(...)` | Agrega un borde inferior en la fila actual |
| `PageBreak()` | Crea un nuevo worksheet en el mismo workbook |
| `Background(...)` | Aplica color de fondo a la celda |
| `Border(...)` | Aplica borde a la celda |
| `AlignCenter()` / `AlignRight()` | Alineación horizontal en la celda |
| `Padding(...)` | Transparente (se ignora; Excel no tiene padding por celda) |
| `Spacer(...)` | Inserta una fila vacía |
| `Image(...)` | Ignorado (no soportado en el renderer Excel) |
| Numeración de páginas | Escribe "1" para página actual y "?" para total de páginas |

## RDLC – Importar reportes existentes

`FluentReport.Rdlc` permite convertir archivos `.rdlc` (Visual Studio / SSRS) en documentos FluentReport listos para renderizar como PDF o Excel.

```csharp
using FluentReport.Rdlc;

var doc = DocumentRdlcExtensions.FromRdlc(
    "reportes/catalogo.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Productos"] = productos.Cast<object>()
    },
    parameters: new Dictionary<string, object>
    {
        ["Empresa"] = "Acme Corp."
    });

doc.GeneratePdf("catalogo.pdf");
```

Elementos RDLC soportados: `Textbox`, `Line`, `Image`, `Tablix` (con datos y `ColSpan`), `PageHeader`, `PageFooter`, márgenes y tamaño de página.

Expresiones soportadas: `=Fields!X.Value`, `=Parameters!X.Value` y literales.

> 📄 **Documentación completa, limitaciones y ejemplos:** [`docs/rdlc-import.md`](docs/rdlc-import.md)

## Nuevos elementos (API fluent)

### Lista de datos — `List<T>()`

Repite una plantilla por cada elemento de una colección:

```csharp
page.Content().List(pedidos, (container, pedido) =>
{
    container.Column(col =>
    {
        col.Item().Text(pedido.Descripcion).Bold();
        col.Item().Text($"Total: {pedido.Total:C}");
    });
}, spacing: 8f);
```

### Gráficos — `Chart()`

Genera gráficos de barras o líneas con ejes, leyenda y múltiples series:

```csharp
page.Content().Chart()
    .Type(ChartType.Bar)
    .Title("Ventas por Trimestre")
    .Categories(new[] { "Q1", "Q2", "Q3", "Q4" })
    .AddSeries("Ingresos", new double[] { 100_000, 145_000, 132_000, 198_000 })
    .AddSeries("Costos",   new double[] { 78_000, 91_000, 85_000, 110_000 }, "#FF6666")
    .Height(220);
```

### Documento anidado — `Subreport()`

Incrusta un `Document` completo dentro de otro:

```csharp
var anexo = Document.Create(c => { /* ... */ });
page.Content().Subreport(anexo);
```

### `ColSpan` en tablas

Las celdas de encabezado y de datos pueden abarcar múltiples columnas:

```csharp
table.Header(h =>
{
    h.Cell(3).Background("#4472C4").Text("Encabezado extendido").Color("#FFFFFF");
});
```

### Estilos condicionales en texto

`BoldResolver`, `ItalicResolver` y `ColorResolver` permiten evaluar el estilo en tiempo de render:

```csharp
t.Span(item.Estado, s =>
{
    s.ColorResolver = () => item.Activo
        ? new ReportColor(0, 150, 0)
        : new ReportColor(200, 0, 0);
});
```

## API Reference

### Document

| Método | Descripción |
|--------|-------------|
| `Document.Create(configure)` | Crea un nuevo documento |
| `DocumentRdlcExtensions.FromRdlc(path, ...)` | Importa un archivo `.rdlc` *(requiere FluentReport.Rdlc)* |
| `DocumentRdlcExtensions.FromRdlcStream(stream, ...)` | Importa RDLC desde un `Stream` *(requiere FluentReport.Rdlc)* |
| `DocumentRdlcExtensions.FromRdlcXml(xml, ...)` | Importa RDLC desde una cadena XML *(requiere FluentReport.Rdlc)* |
| `.GeneratePdf(filePath)` | Genera el PDF y lo guarda en disco |
| `.GeneratePdf(stream)` | Genera el PDF y lo escribe en un stream |
| `.GeneratePdf()` | Genera el PDF y devuelve `byte[]` |
| `.GenerateExcel(filePath)` | Genera el Excel (.xlsx) y lo guarda en disco *(requiere FluentReport.Excel)* |
| `.GenerateExcel(stream)` | Genera el Excel y lo escribe en un stream *(requiere FluentReport.Excel)* |
| `.GenerateExcel()` | Genera el Excel y devuelve `byte[]` *(requiere FluentReport.Excel)* |

### PageBuilder

| Método | Descripción |
|--------|-------------|
| `.Size(PageSizes.A4)` | Tamaño de página (A3, A4, A5, Letter, Legal) |
| `.Size(width, height)` | Tamaño personalizado en puntos (1pt = 1/72 in) |
| `.Landscape()` | Orientación horizontal |
| `.MarginAll(40)` | Márgenes iguales en todos los lados |
| `.Margin(top, right, bottom, left)` | Márgenes individuales |
| `.MarginHorizontal(h)` | Márgenes horizontales |
| `.MarginVertical(v)` | Márgenes verticales |
| `.Header()` | Devuelve un `ContainerBuilder` para la cabecera |
| `.Content()` | Devuelve un `ContainerBuilder` para el contenido |
| `.Footer()` | Devuelve un `ContainerBuilder` para el pie de página |

### ContainerBuilder

| Método | Descripción |
|--------|-------------|
| `.Text("texto")` | Agrega texto estático (devuelve `TextBuilder`) |
| `.Text(x => { ... })` | Texto dinámico con spans (números de página) |
| `.Column(configure)` | Contenedor vertical |
| `.Row(configure)` | Contenedor horizontal |
| `.Table(configure)` | Tabla |
| `.Image(path)` | Imagen desde archivo |
| `.Image(bytes)` | Imagen desde bytes |
| `.Spacer(size)` | Espacio vacío |
| `.Line(thickness, color)` | Línea horizontal |
| `.Padding(all)` | Padding uniforme |
| `.PaddingHorizontal(h)` | Padding horizontal |
| `.PaddingVertical(v)` | Padding vertical |
| `.Background("#RRGGBB")` | Color de fondo |
| `.Border(width, color)` | Borde |
| `.AlignCenter()` | Alineación centrada |
| `.AlignRight()` | Alineación derecha |
| `.PageBreak()` | Salto de página explícito |
| `.List<T>(items, template, spacing)` | Repite una plantilla por cada elemento de la colección |
| `.Chart()` | Gráfico de barras o líneas (devuelve `ChartBuilder`) |
| `.Subreport(document)` | Incrusta un `Document` anidado |

### TextBuilder

| Método | Descripción |
|--------|-------------|
| `.FontSize(14)` | Tamaño de fuente |
| `.FontFamily("Arial")` | Familia tipográfica |
| `.Bold()` | Negrita |
| `.Italic()` | Cursiva |
| `.Underline()` | Subrayado |
| `.Color("#RRGGBB")` | Color del texto |
| `.AlignCenter()` | Centrado |
| `.AlignRight()` | Derecha |
| `.AlignLeft()` | Izquierda |
| `.AlignJustify()` | Justificado |
| `.LineSpacing(1.5f)` | Interlineado |

### ColumnBuilder

| Método | Descripción |
|--------|-------------|
| `.Spacing(8)` | Espaciado entre elementos |
| `.Item()` | Agrega un elemento (devuelve `ContainerBuilder`) |

### RowBuilder

| Método | Descripción |
|--------|-------------|
| `.Spacing(8)` | Espaciado entre elementos |
| `.Item()` | Elemento con ancho relativo |
| `.RelativeItem(weight)` | Elemento relativo con peso |
| `.FixedItem(width)` | Elemento con ancho fijo en puntos |

### TableBuilder

| Método | Descripción |
|--------|-------------|
| `.ColumnsDefinition(configure)` | Define columnas |
| `.Header(configure)` | Define fila de encabezado |
| `.Cell(colSpan)` | Agrega una celda de datos con span de columnas opcional |
| `.BorderEachCell(width, color)` | Borde en cada celda |

### TableColumnDefinitionBuilder

| Método | Descripción |
|--------|-------------|
| `.RelativeColumn(weight)` | Columna con ancho relativo |
| `.ConstantColumn(width)` | Columna con ancho fijo |

### DynamicTextBuilder (para pie de página)

| Método | Descripción |
|--------|-------------|
| `.Span("texto")` | Texto estático |
| `.CurrentPageNumber()` | Número de página actual |
| `.TotalPages()` | Total de páginas |

## Tamaños de página predefinidos

| Constante | Dimensiones (puntos) |
|-----------|---------------------|
| `PageSizes.A4` | 595 × 842 |
| `PageSizes.A3` | 842 × 1191 |
| `PageSizes.A5` | 420 × 595 |
| `PageSizes.Letter` | 612 × 792 |
| `PageSizes.Legal` | 612 × 1008 |

## Estructura del proyecto

```
src/
├── FluentReport/               # Librería principal (PDF)
│   ├── Document.cs             # Punto de entrada
│   ├── Core/                   # Tipos base (Size, Position, Rect, PageSize...)
│   ├── Styling/                # Estilos (TextStyle, BorderStyle, Color)
│   ├── Elements/               # Elementos renderizables
│   ├── Builders/               # Fluent API builders
│   └── Rendering/              # Motor de renderizado con SkiaSharp
├── FluentReport.Excel/         # Renderer Excel (opcional)
│   ├── ExcelDocumentRenderer.cs # Motor de renderizado Excel con ClosedXML
│   └── DocumentExcelExtensions.cs # Métodos de extensión GenerateExcel
└── FluentReport.Rdlc/          # Importador RDLC (opcional)
    ├── RdlcDocumentFactory.cs  # Parser XML .rdlc → DocumentSettings
    ├── RdlcExpressionEvaluator.cs # Evaluador de expresiones =Fields!/=Parameters!
    └── DocumentRdlcExtensions.cs # Métodos de extensión FromRdlc
tests/
├── FluentReport.Tests/         # Tests PDF con xUnit
├── FluentReport.Excel.Tests/   # Tests Excel con xUnit
└── FluentReport.Rdlc.Tests/    # Tests RDLC con xUnit
docs/
└── rdlc-import.md              # Guía completa del importador RDLC
samples/
└── FluentReport.Samples/       # Samples PDF (01-06) y Excel (07-09)
```

## Dependencias

- [SkiaSharp](https://www.nuget.org/packages/SkiaSharp) 3.116.1 — renderizado PDF
- SkiaSharp.NativeAssets.Linux.NoDependencies — soporte Linux sin libfontconfig
- [ClosedXML](https://www.nuget.org/packages/ClosedXML) 0.102.2 — renderizado Excel (solo FluentReport.Excel)
- System.Xml.Linq (BCL) — parser RDLC (solo FluentReport.Rdlc, sin dependencias adicionales)
