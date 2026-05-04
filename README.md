# FluentReport

Librería .NET 10 para generar PDFs y Excel usando una API fluent en C#. Usa SkiaSharp como motor de renderizado PDF y ClosedXML para Excel. Funciona en Linux sin dependencias nativas adicionales.

## Características

- API fluent para construir documentos PDF **y Excel (.xlsx)**
- Motor de layout con cálculo de tamaños (measure/arrange)
- Elementos: texto, columnas, filas, tablas, imágenes, espaciadores, líneas, bordes, padding, salto de página
- Soporte de cabecera y pie de página
- Numeración de páginas dinámica
- Salto de página automático cuando el contenido supera la página
- **Renderer Excel** (`FluentReport.Excel`): genera `.xlsx` directamente desde el mismo fluent API
- Compatible con Linux (SkiaSharp.NativeAssets.Linux.NoDependencies)

## Instalación

Agrega la referencia al proyecto principal:

```xml
<ProjectReference Include="src/FluentReport/FluentReport.csproj" />
```

Para generar Excel, agrega además el renderer de Excel:

```xml
<ProjectReference Include="src/FluentReport.Excel/FluentReport.Excel.csproj" />
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
```

## API Reference

### Document

| Método | Descripción |
|--------|-------------|
| `Document.Create(configure)` | Crea un nuevo documento |
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
| `.Cell()` | Agrega una celda de datos |
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
└── FluentReport.Excel/         # Renderer Excel (opcional)
    ├── ExcelDocumentRenderer.cs # Motor de renderizado Excel con ClosedXML
    └── DocumentExcelExtensions.cs # Métodos de extensión GenerateExcel
tests/
├── FluentReport.Tests/         # Tests PDF con xUnit (19 tests)
└── FluentReport.Excel.Tests/   # Tests Excel con xUnit (12 tests)
samples/
└── FluentReport.Samples/       # Samples PDF (01-06) y Excel (07-09)
```

## Dependencias

- [SkiaSharp](https://github.com/mono/SkiaSharp) 3.116.1 — renderizado PDF
- SkiaSharp.NativeAssets.Linux.NoDependencies — soporte Linux sin libfontconfig
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.102.2 — renderizado Excel (solo FluentReport.Excel)
