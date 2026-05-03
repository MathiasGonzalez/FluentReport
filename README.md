# FluentReport

Librería .NET 10 para generar PDFs usando una API fluent en C#. Usa SkiaSharp como motor de renderizado y funciona en Linux sin dependencias nativas adicionales.

## Características

- API fluent para construir documentos PDF
- Motor de layout con cálculo de tamaños (measure/arrange)
- Elementos: texto, columnas, filas, tablas, imágenes, espaciadores, líneas, bordes, padding, salto de página
- Soporte de cabecera y pie de página
- Numeración de páginas dinámica
- Salto de página automático cuando el contenido supera la página
- Compatible con Linux (SkiaSharp.NativeAssets.Linux.NoDependencies)

## Instalación

Agrega la referencia al proyecto:

```xml
<ProjectReference Include="src/FluentReport/FluentReport.csproj" />
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

## API Reference

### Document

| Método | Descripción |
|--------|-------------|
| `Document.Create(configure)` | Crea un nuevo documento |
| `.GeneratePdf(filePath)` | Genera el PDF y lo guarda en disco |
| `.GeneratePdf(stream)` | Genera el PDF y lo escribe en un stream |
| `.GeneratePdf()` | Genera el PDF y devuelve `byte[]` |

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
└── FluentReport/           # Librería principal
    ├── Document.cs         # Punto de entrada
    ├── Core/               # Tipos base (Size, Position, Rect, PageSize...)
    ├── Styling/            # Estilos (TextStyle, BorderStyle, Color)
    ├── Elements/           # Elementos renderizables
    ├── Builders/           # Fluent API builders
    └── Rendering/          # Motor de renderizado con SkiaSharp
tests/
└── FluentReport.Tests/     # Tests con xUnit (19 tests)
```

## Dependencias

- [SkiaSharp](https://github.com/mono/SkiaSharp) 3.116.1
- SkiaSharp.NativeAssets.Linux.NoDependencies (soporte Linux sin libfontconfig)
