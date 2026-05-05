# FluentReport — Referencia de la API Fluent

Esta guía documenta **todas las clases y métodos** disponibles para construir documentos mediante la API fluent de FluentReport.

> **Namespaces de uso frecuente**
>
> ```csharp
> using FluentReport;            // Document, PageSizes
> using FluentReport.Core;       // PageSize, PageSizes
> using FluentReport.Elements;   // ChartType (para gráficos)
> using FluentReport.Styling;    // ReportColor, TextStyle (para estilos condicionales)
> ```

---

## Tabla de contenidos

1. [Document](#1-document)
2. [DocumentBuilder](#2-documentbuilder)
3. [PageBuilder](#3-pagebuilder)
4. [ContainerBuilder](#4-containerbuilder)
5. [TextBuilder](#5-textbuilder)
6. [DynamicTextBuilder](#6-dynamictextbuilder)
7. [ColumnBuilder](#7-columnbuilder)
8. [RowBuilder](#8-rowbuilder)
9. [TableBuilder](#9-tablebuilder)
   - [TableHeaderBuilder](#tableheaderbuilder)
   - [TableColumnDefinitionBuilder](#tablecolumndefinitionbuilder)
10. [ChartBuilder](#10-chartbuilder)
11. [Elementos especiales](#11-elementos-especiales)
    - [List\<T\>()](#listt)
    - [Subreport()](#subreport)
12. [ReportColor](#12-reportcolor)
13. [TextStyle y estilos condicionales](#13-textstyle-y-estilos-condicionales)
14. [PageSizes — tamaños predefinidos](#14-pagesizes--tamaños-predefinidos)
15. [Ejemplo completo](#15-ejemplo-completo)

---

## 1. Document

Punto de entrada principal. Crea y renderiza documentos.

### Métodos de fábrica

| Método | Descripción |
|--------|-------------|
| `Document.Create(Action<DocumentBuilder>)` | Crea un documento a partir de la API fluent |
| `Document.FromSettings(DocumentSettings)` | Crea un documento desde un `DocumentSettings` pre-construido *(uso avanzado / capas de traducción como RDLC)* |

### Métodos de renderizado — PDF

| Método | Descripción |
|--------|-------------|
| `.GeneratePdf(string filePath)` | Genera el PDF y lo guarda en disco |
| `.GeneratePdf(Stream stream)` | Genera el PDF y lo escribe en el stream |
| `.GeneratePdf()` | Genera el PDF y devuelve `byte[]` |

### Métodos de renderizado — Imágenes PNG

| Método | Descripción |
|--------|-------------|
| `.GenerateImages(float scale = 1f)` | Renderiza cada página a PNG; devuelve `IReadOnlyList<byte[]>`. El parámetro `scale` controla los píxeles por punto (ej.: `2f` para hi-DPI) |

### Métodos de renderizado — Excel *(requiere `FluentReport.Excel`)*

| Método | Descripción |
|--------|-------------|
| `.GenerateExcel(string filePath)` | Genera el `.xlsx` y lo guarda en disco |
| `.GenerateExcel(Stream stream)` | Genera el `.xlsx` y lo escribe en el stream |
| `.GenerateExcel()` | Genera el `.xlsx` y devuelve `byte[]` |

```csharp
// PDF en disco
Document.Create(container => { ... }).GeneratePdf("reporte.pdf");

// PDF como bytes (ej.: para enviar por HTTP)
byte[] bytes = Document.Create(container => { ... }).GeneratePdf();

// PNG de cada página (escala 2× hi-DPI)
IReadOnlyList<byte[]> pages = Document.Create(container => { ... }).GenerateImages(scale: 2f);

// Excel (requiere FluentReport.Excel)
using FluentReport.Excel;
Document.Create(container => { ... }).GenerateExcel("reporte.xlsx");
```

---

## 2. DocumentBuilder

Recibido en el callback de `Document.Create(...)`. Permite agregar una o más páginas al documento.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Page(Action<PageBuilder>)` | `DocumentBuilder` | Agrega una página al documento (encadenable para múltiples páginas) |

```csharp
Document.Create(container =>
{
    container.Page(page => { /* página 1 */ });
    container.Page(page => { /* página 2 */ });
});
```

> **Nota:** Cada llamada a `.Page()` configura una *sección* independiente con su propio tamaño, márgenes y contenido. Para saltos de página dentro de una misma sección, usa `ContainerBuilder.PageBreak()`.

---

## 3. PageBuilder

Configura el tamaño, los márgenes y el contenido de una página.

### Tamaño y orientación

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Size(PageSize size)` | `PageBuilder` | Aplica un tamaño de página predefinido (ej.: `PageSizes.A4`) |
| `.Size(float width, float height)` | `PageBuilder` | Tamaño personalizado en puntos (1 pt = 1/72 in) |
| `.Landscape()` | `PageBuilder` | Invierte ancho y alto para orientación horizontal |

### Márgenes

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.MarginAll(float margin)` | `PageBuilder` | Mismo margen en los cuatro lados |
| `.Margin(float top, float right, float bottom, float left)` | `PageBuilder` | Márgenes individuales |
| `.MarginHorizontal(float h)` | `PageBuilder` | Márgenes izquierdo y derecho iguales |
| `.MarginVertical(float v)` | `PageBuilder` | Márgenes superior e inferior iguales |

### Zonas de contenido

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Header()` | `ContainerBuilder` | Cabecera de página (se repite en cada página renderizada) |
| `.Content()` | `ContainerBuilder` | Cuerpo principal del documento |
| `.Footer()` | `ContainerBuilder` | Pie de página (se repite en cada página renderizada) |

```csharp
container.Page(page =>
{
    page.Size(PageSizes.A4);
    page.MarginAll(40);

    page.Header().Text("Mi Reporte").FontSize(16).Bold().AlignCenter();

    page.Content().Column(col =>
    {
        col.Spacing(8);
        col.Item().Text("Contenido del reporte...");
    });

    page.Footer().AlignCenter().Text(x =>
    {
        x.Span("Página ");
        x.CurrentPageNumber();
        x.Span(" de ");
        x.TotalPages();
    });
});
```

---

## 4. ContainerBuilder

Es el bloque fundamental de composición. Cada celda de tabla, ítem de columna/fila y zona de página devuelve un `ContainerBuilder`.

Un `ContainerBuilder` admite **exactamente un elemento hijo**; la última llamada al método de contenido (ej.: `.Text()`, `.Column()`, `.Table()`) reemplaza a la anterior.

### Contenido

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Text(string text)` | `TextBuilder` | Texto estático |
| `.Text(Action<DynamicTextBuilder>)` | `TextBuilder` | Texto con múltiples spans (número de página, estilos mixtos) |
| `.Column(Action<ColumnBuilder>)` | `ContainerBuilder` | Contenedor vertical |
| `.Row(Action<RowBuilder>)` | `ContainerBuilder` | Contenedor horizontal |
| `.Table(Action<TableBuilder>)` | `ContainerBuilder` | Tabla de datos |
| `.Image(string path)` | `ContainerBuilder` | Imagen desde ruta de archivo |
| `.Image(byte[] bytes)` | `ContainerBuilder` | Imagen desde bytes |
| `.Spacer(float size = 0)` | `ContainerBuilder` | Espacio vacío (flexible si `size = 0`, fijo si `size > 0`) |
| `.Line(float thickness = 1, string? colorHex = null)` | `ContainerBuilder` | Línea horizontal |
| `.PageBreak()` | `ContainerBuilder` | Salto de página explícito |
| `.List<T>(items, template, spacing)` | `ContainerBuilder` | Repite una plantilla por cada elemento de la colección |
| `.Chart()` | `ChartBuilder` | Gráfico de barras o líneas |
| `.Subreport(Document nested)` | `ContainerBuilder` | Incrusta un `Document` completo como elemento inline |

### Decoradores visuales

Estos métodos se pueden encadenar antes o después del método de contenido:

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Padding(float all)` | `ContainerBuilder` | Padding uniforme en los cuatro lados |
| `.PaddingHorizontal(float h)` | `ContainerBuilder` | Padding izquierdo y derecho |
| `.PaddingVertical(float v)` | `ContainerBuilder` | Padding superior e inferior |
| `.PaddingTop(float v)` | `ContainerBuilder` | Padding superior |
| `.PaddingBottom(float v)` | `ContainerBuilder` | Padding inferior |
| `.PaddingLeft(float v)` | `ContainerBuilder` | Padding izquierdo |
| `.PaddingRight(float v)` | `ContainerBuilder` | Padding derecho |
| `.Background(string hex)` | `ContainerBuilder` | Color de fondo hexadecimal `"#RRGGBB"` |
| `.Background(ReportColor color)` | `ContainerBuilder` | Color de fondo como `ReportColor` |
| `.Border(float width = 1, string? colorHex = null)` | `ContainerBuilder` | Borde rectangular |
| `.AlignCenter()` | `ContainerBuilder` | Alineación horizontal centrada |
| `.AlignRight()` | `ContainerBuilder` | Alineación horizontal a la derecha |
| `.AlignLeft()` | `ContainerBuilder` | Alineación horizontal a la izquierda |

> **Orden de aplicación interno:** padding → border/background → alignment. Los decoradores se pueden encadenar en cualquier orden desde la API.

```csharp
// Celda con fondo, borde y texto centrado
col.Item()
   .Background("#E8F4FD")
   .Border(1, "#2196F3")
   .Padding(8)
   .AlignCenter()
   .Text("Destacado")
   .Bold();
```

---

## 5. TextBuilder

Devuelto por `ContainerBuilder.Text(string)`. Configura el estilo del texto.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.FontSize(float size)` | `TextBuilder` | Tamaño de fuente en puntos |
| `.FontFamily(string family)` | `TextBuilder` | Familia tipográfica (ej.: `"Arial"`, `"sans-serif"`) |
| `.Bold()` | `TextBuilder` | Negrita |
| `.Italic()` | `TextBuilder` | Cursiva |
| `.Underline()` | `TextBuilder` | Subrayado |
| `.Color(string hex)` | `TextBuilder` | Color del texto `"#RRGGBB"` |
| `.Color(ReportColor color)` | `TextBuilder` | Color del texto como `ReportColor` |
| `.AlignCenter()` | `TextBuilder` | Centrado |
| `.AlignRight()` | `TextBuilder` | Alineación a la derecha |
| `.AlignLeft()` | `TextBuilder` | Alineación a la izquierda |
| `.AlignJustify()` | `TextBuilder` | Texto justificado |
| `.LineSpacing(float spacing)` | `TextBuilder` | Interlineado como multiplicador del tamaño de fuente (default `1.2f`) |

### Métodos de retorno al padre

Estos métodos permiten continuar encadenando decoradores del `ContainerBuilder` padre después de configurar el texto:

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Padding(float all)` | `ContainerBuilder` | Delega a `ContainerBuilder.Padding()` |
| `.PaddingVertical(float v)` | `ContainerBuilder` | Delega a `ContainerBuilder.PaddingVertical()` |
| `.PaddingHorizontal(float h)` | `ContainerBuilder` | Delega a `ContainerBuilder.PaddingHorizontal()` |

```csharp
col.Item().Text("Título principal")
   .FontSize(20)
   .Bold()
   .Color("#1A237E")
   .AlignCenter()
   .LineSpacing(1.5f);
```

---

## 6. DynamicTextBuilder

Devuelto en el callback de `ContainerBuilder.Text(Action<DynamicTextBuilder>)`. Permite componer texto con múltiples tramos (*spans*) con estilos independientes, incluyendo números de página dinámicos.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Span(string text, Action<TextStyle>? configure = null)` | `DynamicTextBuilder` | Agrega un span de texto estático con estilo opcional |
| `.CurrentPageNumber(Action<TextStyle>? configure = null)` | `DynamicTextBuilder` | Agrega un span con el número de página actual |
| `.TotalPages(Action<TextStyle>? configure = null)` | `DynamicTextBuilder` | Agrega un span con el total de páginas del documento |

```csharp
// Pie de página con número de página
page.Footer().AlignCenter().Text(x =>
{
    x.Span("Página ");
    x.CurrentPageNumber();
    x.Span(" de ");
    x.TotalPages();
});

// Texto con estilos mixtos en el mismo párrafo
col.Item().Text(x =>
{
    x.Span("Estado: ", s => s.Bold = true);
    x.Span("Activo",  s => s.Color = ReportColor.FromHex("#00AA00"));
});
```

> **Nota:** Los spans de un `DynamicTextBuilder` se renderizan en línea (inline). El word-wrap automático solo aplica a spans de texto único (`.Text(string)`).

---

## 7. ColumnBuilder

Organiza elementos en una pila vertical.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Spacing(float spacing)` | `ColumnBuilder` | Espacio vertical entre ítems, en puntos |
| `.Item()` | `ContainerBuilder` | Agrega un elemento a la columna |

```csharp
page.Content().Column(col =>
{
    col.Spacing(10);

    col.Item().Text("Sección 1").FontSize(14).Bold();
    col.Item().Line(0.5f, "#CCCCCC");
    col.Item().Text("Párrafo de texto del reporte.");
    col.Item().Spacer(20);
    col.Item().Text("Sección 2").FontSize(14).Bold();
});
```

---

## 8. RowBuilder

Organiza elementos en una fila horizontal. El ancho de cada ítem puede ser relativo o fijo.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Spacing(float spacing)` | `RowBuilder` | Espacio horizontal entre ítems, en puntos |
| `.Item()` | `ContainerBuilder` | Ítem con ancho relativo peso 1 (equivalente a `RelativeItem(1)`) |
| `.RelativeItem(float weight = 1)` | `ContainerBuilder` | Ítem con ancho relativo; el ancho es proporcional al peso total de la fila |
| `.FixedItem(float width)` | `ContainerBuilder` | Ítem con ancho fijo en puntos |

```csharp
page.Content().Row(row =>
{
    row.Spacing(12);

    // 2/4 del ancho disponible
    row.RelativeItem(2).Column(col =>
    {
        col.Item().Text("Nombre del cliente").Bold();
        col.Item().Text("Empresa S.A.");
    });

    // 1/4 del ancho disponible
    row.RelativeItem(1).Column(col =>
    {
        col.Item().Text("Fecha").Bold();
        col.Item().Text("01/05/2026");
    });

    // Ancho fijo de 80 pt
    row.FixedItem(80).AlignRight().Text("$1.200").Bold().FontSize(14);
});
```

---

## 9. TableBuilder

Genera una tabla con columnas de ancho relativo o fijo, cabecera repetida y soporte de `ColSpan`.

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.ColumnsDefinition(Action<TableColumnDefinitionBuilder>)` | `TableBuilder` | Define el número y ancho de las columnas |
| `.Header(Action<TableHeaderBuilder>)` | `TableBuilder` | Define la fila de encabezado |
| `.Cell(int colSpan = 1)` | `ContainerBuilder` | Agrega una celda de datos; `colSpan > 1` fusiona columnas |
| `.BorderEachCell(float width = 1, string? colorHex = null)` | `TableBuilder` | Aplica borde a todas las celdas de la tabla |

### TableHeaderBuilder

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Cell(int colSpan = 1)` | `ContainerBuilder` | Agrega una celda de encabezado; `colSpan > 1` fusiona columnas |

### TableColumnDefinitionBuilder

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.RelativeColumn(float weight = 1)` | `TableColumnDefinitionBuilder` | Columna con ancho relativo al total |
| `.ConstantColumn(float width)` | `TableColumnDefinitionBuilder` | Columna con ancho fijo en puntos |

```csharp
col.Item().Table(table =>
{
    // Definir columnas: 50% | 30% | fijo 80pt
    table.ColumnsDefinition(cols =>
    {
        cols.RelativeColumn(5);
        cols.RelativeColumn(3);
        cols.ConstantColumn(80);
    });

    // Encabezado con ColSpan
    table.Header(h =>
    {
        h.Cell(2).Background("#4472C4").Padding(5).Text("Producto / Descripción").Bold().Color("#FFFFFF");
        h.Cell().Background("#4472C4").Padding(5).Text("Precio").Bold().Color("#FFFFFF");
    });

    table.BorderEachCell(0.5f, "#CCCCCC");

    // Datos
    foreach (var item in productos)
    {
        table.Cell().Padding(4).Text(item.Nombre);
        table.Cell().Padding(4).Text(item.Descripcion);
        table.Cell().Padding(4).AlignRight().Text(item.Precio.ToString("C"));
    }
});
```

> **Orden de celdas:** las celdas de datos se agregan secuencialmente de izquierda a derecha y de arriba a abajo; FluentReport distribuye automáticamente las celdas en filas según la definición de columnas, respetando los `colSpan`.

---

## 10. ChartBuilder

Devuelto por `ContainerBuilder.Chart()`. Configura un gráfico de barras o líneas con múltiples series.

```csharp
using FluentReport.Elements; // necesario para ChartType
```

| Método | Devuelve | Descripción |
|--------|----------|-------------|
| `.Type(ChartType type)` | `ChartBuilder` | `ChartType.Bar` (barras) o `ChartType.Line` (líneas) |
| `.Title(string title)` | `ChartBuilder` | Título mostrado sobre el gráfico |
| `.Height(float height)` | `ChartBuilder` | Alto del gráfico en puntos (default `200`) |
| `.Categories(IEnumerable<string> labels)` | `ChartBuilder` | Etiquetas del eje X |
| `.AddSeries(string label, IEnumerable<double> values, string? colorHex = null)` | `ChartBuilder` | Agrega una serie de datos; el color es opcional (se asigna por paleta si es `null`) |
| `.Padding(float all)` | `ContainerBuilder` | Delega padding al `ContainerBuilder` padre |
| `.Background(string hex)` | `ContainerBuilder` | Delega background al `ContainerBuilder` padre |

```csharp
page.Content().Chart()
    .Type(ChartType.Bar)
    .Title("Ventas por Trimestre")
    .Height(220)
    .Categories(new[] { "Q1", "Q2", "Q3", "Q4" })
    .AddSeries("Ingresos", new double[] { 100_000, 145_000, 132_000, 198_000 })
    .AddSeries("Costos",   new double[] {  78_000,  91_000,  85_000, 110_000 }, "#FF6666");
```

### Paleta de colores por defecto

Si no se especifica `colorHex` en `AddSeries`, se usa esta paleta en orden:

| Índice | Color | RGB |
|--------|-------|-----|
| 0 | Azul acero | `(70, 130, 180)` |
| 1 | Rojo | `(220, 80, 80)` |
| 2 | Verde | `(80, 160, 80)` |
| 3 | Naranja | `(210, 140, 40)` |
| 4 | Púrpura | `(140, 80, 200)` |
| 5 | Teal | `(60, 180, 180)` |

---

## 11. Elementos especiales

### List\<T\>

Repite una plantilla por cada elemento de una colección, apilando los resultados verticalmente.

```csharp
ContainerBuilder.List<T>(
    IEnumerable<T> items,
    Action<ContainerBuilder, T> itemTemplate,
    float spacing = 0)
```

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `items` | `IEnumerable<T>` | Colección de datos |
| `itemTemplate` | `Action<ContainerBuilder, T>` | Callback que configura el contenedor por cada ítem |
| `spacing` | `float` | Espacio vertical entre ítems, en puntos |

```csharp
var pedidos = GetPedidos();

page.Content().List(pedidos, (container, pedido) =>
{
    container.Column(col =>
    {
        col.Item().Text(pedido.Descripcion).Bold();
        col.Item().Text($"Total: {pedido.Total:C}");
        col.Item().Line(0.5f, "#CCCCCC");
    });
}, spacing: 8f);
```

---

### Subreport

Incrusta un `Document` completo como un elemento inline dentro de otro documento. Las páginas del documento anidado se renderizan una después de la otra en el punto de inserción.

```csharp
ContainerBuilder.Subreport(Document nested)
```

```csharp
var anexo = Document.Create(c =>
{
    c.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);
        page.Content().Text("Contenido del anexo.");
    });
});

page.Content().Column(col =>
{
    col.Item().Text("Cuerpo principal del reporte.");
    col.Item().Subreport(anexo);
});
```

---

## 12. ReportColor

Estructura de color inmutable usada en toda la API como alternativa a las cadenas hexadecimales.

```csharp
namespace FluentReport.Styling;

public readonly struct ReportColor
```

### Constructores y fábrica

| Forma | Descripción |
|-------|-------------|
| `new ReportColor(byte r, byte g, byte b)` | Color opaco |
| `new ReportColor(byte r, byte g, byte b, byte a)` | Color con canal alfa (`255` = totalmente opaco) |
| `ReportColor.FromHex(string hex)` | Desde cadena `"#RRGGBB"` o `"#RRGGBBAA"` |

### Colores predefinidos

| Constante | Valor |
|-----------|-------|
| `ReportColor.Black` | `(0, 0, 0)` |
| `ReportColor.White` | `(255, 255, 255)` |
| `ReportColor.Gray` | `(128, 128, 128)` |
| `ReportColor.LightGray` | `(211, 211, 211)` |
| `ReportColor.Transparent` | `(0, 0, 0, 0)` |

```csharp
// Las siguientes formas son equivalentes
.Color("#4472C4")
.Color(ReportColor.FromHex("#4472C4"))
.Color(new ReportColor(68, 114, 196))
```

---

## 13. TextStyle y estilos condicionales

`TextStyle` contiene todas las propiedades de estilo de un span de texto. Se accede directamente en los callbacks de `DynamicTextBuilder.Span()` y `DynamicTextBuilder.CurrentPageNumber()`.

```csharp
namespace FluentReport.Styling;
```

### Propiedades

| Propiedad | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `FontSize` | `float` | `12` | Tamaño de fuente en puntos |
| `FontFamily` | `string` | `"sans-serif"` | Familia tipográfica |
| `Bold` | `bool` | `false` | Negrita estática |
| `Italic` | `bool` | `false` | Cursiva estática |
| `Underline` | `bool` | `false` | Subrayado |
| `Color` | `ReportColor` | `Black` | Color estático |
| `Alignment` | `TextAlignment` | `Left` | `Left`, `Center`, `Right`, `Justify` |
| `LineSpacing` | `float` | `1.2f` | Multiplicador de interlineado |
| `BoldResolver` | `Func<bool>?` | `null` | Delegate que sobreescribe `Bold` en tiempo de render |
| `ItalicResolver` | `Func<bool>?` | `null` | Delegate que sobreescribe `Italic` en tiempo de render |
| `ColorResolver` | `Func<ReportColor>?` | `null` | Delegate que sobreescribe `Color` en tiempo de render |

Cuando un resolver no es `null`, tiene precedencia sobre la propiedad estática correspondiente.

### Estilos condicionales (resolvers)

Los resolvers se cierran sobre cualquier variable del contexto circundante, lo que permite estilo dinámico sin re-construir el documento:

```csharp
using FluentReport.Styling;

col.Item().Text(t =>
{
    t.Span("Estado: ", s => s.Bold = true);

    t.Span(item.Estado, s =>
    {
        // Color evaluado en tiempo de renderizado
        s.ColorResolver = () => item.Activo
            ? new ReportColor(0, 150, 0)   // verde
            : new ReportColor(200, 0, 0);  // rojo

        // Negrita condicional
        s.BoldResolver = () => item.Prioridad == "Alta";
    });
});
```

---

## 14. PageSizes — tamaños predefinidos

```csharp
namespace FluentReport.Core;
```

| Constante | Ancho (pt) | Alto (pt) | Notas |
|-----------|------------|-----------|-------|
| `PageSizes.A4` | 595.28 | 841.89 | ISO A4 |
| `PageSizes.A3` | 841.89 | 1190.55 | ISO A3 |
| `PageSizes.A5` | 419.53 | 595.28 | ISO A5 |
| `PageSizes.Letter` | 612 | 792 | US Letter |
| `PageSizes.Legal` | 612 | 1008 | US Legal |

Para orientación horizontal usa `.Landscape()`:

```csharp
page.Size(PageSizes.A4);       // Portrait  595 × 842
page.Size(PageSizes.A4);
page.Landscape();              // Landscape 842 × 595
```

Para tamaño personalizado:

```csharp
page.Size(400, 300);           // 400 × 300 puntos
```

---

## 15. Ejemplo completo

El siguiente ejemplo muestra un documento con cabecera, pie de página con numeración, sección de información con `Row`, tabla de productos, lista de notas, y un gráfico de ventas.

```csharp
using FluentReport;
using FluentReport.Core;
using FluentReport.Elements;  // ChartType
using FluentReport.Styling;   // ReportColor, TextStyle

var productos = new[]
{
    new { Nombre = "Widget A", Categoria = "Electrónica", Precio = 49.99m },
    new { Nombre = "Gadget B", Categoria = "Hogar",       Precio = 24.99m },
    new { Nombre = "Module C", Categoria = "Electrónica", Precio = 99.99m },
};

var notas = new[]
{
    "Los precios no incluyen IVA.",
    "Envío gratuito en compras mayores a $100.",
    "Consultar disponibilidad de stock."
};

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        // ── Cabecera ──────────────────────────────────────────────────────────
        page.Header().Row(row =>
        {
            row.RelativeItem(3).Text("Catálogo de Productos").FontSize(16).Bold();
            row.RelativeItem(1).AlignRight().Text("Acme Corp.").Color("#555555");
        });

        // ── Contenido ─────────────────────────────────────────────────────────
        page.Content().Column(col =>
        {
            col.Spacing(12);

            // Bloque de fecha y referencia
            col.Item().Row(row =>
            {
                row.Spacing(20);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Fecha").Bold().FontSize(9).Color("#888888");
                    c.Item().Text("01/05/2026");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Referencia").Bold().FontSize(9).Color("#888888");
                    c.Item().Text("CAT-2026-001");
                });
            });

            col.Item().Line(1, "#DDDDDD");

            // Tabla de productos
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);  // Nombre
                    cols.RelativeColumn(2);  // Categoría
                    cols.ConstantColumn(70); // Precio
                });

                table.Header(h =>
                {
                    h.Cell().Background("#1565C0").Padding(5).Text("Nombre").Bold().Color("#FFFFFF");
                    h.Cell().Background("#1565C0").Padding(5).Text("Categoría").Bold().Color("#FFFFFF");
                    h.Cell().Background("#1565C0").Padding(5).Text("Precio").Bold().Color("#FFFFFF");
                });

                table.BorderEachCell(0.5f, "#CCCCCC");

                foreach (var p in productos)
                {
                    table.Cell().Padding(4).Text(p.Nombre);
                    table.Cell().Padding(4).Text(p.Categoria);
                    table.Cell().Padding(4).AlignRight().Text(p.Precio.ToString("C"));
                }
            });

            // Lista de notas
            col.Item().Text("Notas").FontSize(11).Bold();

            col.Item().List(notas, (container, nota) =>
            {
                container.Text($"• {nota}").FontSize(9).Color("#333333");
            }, spacing: 4f);

            // Gráfico de ventas
            col.Item().Text("Ventas históricas").FontSize(11).Bold();

            col.Item().Chart()
                .Type(ChartType.Bar)
                .Title("Ventas mensuales (unidades)")
                .Height(180)
                .Categories(new[] { "Ene", "Feb", "Mar", "Abr", "May" })
                .AddSeries("Widget A", new double[] { 120, 145, 98, 160, 175 })
                .AddSeries("Gadget B", new double[] { 85, 90, 110, 95, 130 }, "#FF6666");
        });

        // ── Pie de página ─────────────────────────────────────────────────────
        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    });
})
.GeneratePdf("catalogo.pdf");
```
