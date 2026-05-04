# FluentReport.Rdlc — Guía de uso

`FluentReport.Rdlc` es un paquete opcional que permite importar archivos de definición de reporte `.rdlc` (SSRS/RDLC) y renderizarlos mediante el pipeline de FluentReport (PDF, PNG, Excel). Convierte el XML del `.rdlc` en un `Document` estándar que luego se puede renderizar igual que cualquier otro documento creado con la API fluent.

---

## Instalación

```shell
dotnet add package FluentReport
dotnet add package FluentReport.Rdlc
```

---

## Uso rápido

```csharp
using FluentReport.Rdlc;

// Desde un archivo .rdlc en disco
var doc = Document.FromRdlc("reportes/ventas.rdlc");
doc.GeneratePdf("ventas.pdf");

// Con datos y parámetros
var doc = Document.FromRdlc(
    "reportes/ventas.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Ventas"] = listaDeVentas
    },
    parameters: new Dictionary<string, object>
    {
        ["Titulo"] = "Reporte de Ventas Q1"
    });

doc.GeneratePdf("ventas.pdf");
```

---

## API

### `Document.FromRdlc(path, datasets?, parameters?)`

Parsea un archivo `.rdlc` desde disco y devuelve un `Document` listo para renderizar.

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `path` | `string` | Ruta absoluta o relativa al archivo `.rdlc` |
| `datasets` | `IDictionary<string, IEnumerable<object>>?` | Filas de datos por nombre de dataset |
| `parameters` | `IDictionary<string, object>?` | Valores de parámetros del reporte |

### `Document.FromRdlcStream(stream, datasets?, parameters?)`

Igual a `FromRdlc` pero lee el XML desde un `Stream`. Útil cuando el `.rdlc` está embebido como recurso o descargado de una red.

```csharp
using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.Resources.report.rdlc")!;

var doc = Document.FromRdlcStream(stream, datasets: myDatasets);
```

### `Document.FromRdlcXml(xml, datasets?, parameters?)`

Igual a `FromRdlc` pero acepta el contenido XML directamente como `string`. Útil para pruebas o cuando el XML se almacena en base de datos.

```csharp
var xml = File.ReadAllText("report.rdlc");
var doc = Document.FromRdlcXml(xml, datasets: myDatasets);
```

---

## Filas de datos

Cada fila puede ser:

- **POCO** (objeto con propiedades públicas):
  ```csharp
  datasets["Productos"] = new[]
  {
      new { Nombre = "Widget", Precio = 9.99m },
      new { Nombre = "Gadget", Precio = 29.99m }
  };
  ```

- **`IDictionary<string, object>`**:
  ```csharp
  datasets["Productos"] = new[]
  {
      new Dictionary<string, object> { ["Nombre"] = "Widget", ["Precio"] = 9.99 }
  };
  ```

Los nombres de campo son **case-insensitive** (tanto en las expresiones `=Fields!X.Value` como al resolver la propiedad/campo del objeto).

---

## Expresiones soportadas

| Expresión RDLC | Descripción |
|----------------|-------------|
| `=Fields!NombreCampo.Value` | Valor de un campo en la fila de datos actual |
| `=Parameters!NombreParam.Value` | Valor de un parámetro del reporte |
| `Texto literal` (sin `=`) | Devuelto tal cual |

> **No soportado:** expresiones de agregado (`=Sum(...)`, `=Count(...)`), `=Globals!...`, expresiones condicionales RDLC (`=IIF(...)`), funciones de formato (`=Format(...)`), expresiones de múltiples campos concatenados, etc. Las expresiones no reconocidas se reemplazan por cadena vacía.

---

## Elementos RDLC soportados

| Elemento RDLC | Equivalente FluentReport | Notas |
|---------------|--------------------------|-------|
| `<Textbox>` | `TextElement` | Soporta `FontSize`, `FontWeight`, `FontStyle`, `TextDecoration`, `Color`, `TextAlign`, `BackgroundColor`, `PaddingTop/Bottom/Left/Right` |
| `<Line>` | `LineElement` | Soporta `Color`, `BorderWidth` |
| `<Image>` | `ImageElement` | Source = `External` (ruta de archivo) y `Database` (bytes en base64 desde campo). `Embedded` genera imagen vacía |
| `<Tablix>` | `TableElement` | Filas estáticas como encabezados y filas de detalle que se repiten por cada fila del dataset. Soporta `ColSpan`. Ver limitaciones. |
| `<PageHeader>` | Header del documento | |
| `<PageFooter>` | Footer del documento | |
| `<Page>` → dimensiones | `PageSettings.Size` / márgenes | Soporta `PageWidth`, `PageHeight`, `TopMargin`, `BottomMargin`, `LeftMargin`, `RightMargin` |

### Elementos no soportados

Los siguientes elementos RDLC son ignorados silenciosamente (se omiten del output):

- `<Chart>` — usar `ChartElement` directamente en la API fluent
- `<Subreport>` — usar `SubreportElement` directamente
- `<Rectangle>` (como contenedor)
- `<List>` RDLC
- `<GaugePanel>`, `<Map>`, `<CustomReportItem>`
- Grupos (`<RowGroups>`, `<ColumnGroups>` avanzados)
- Subtotales y expresiones de agregado

---

## Unidades de medida

El parser convierte automáticamente las unidades RDLC a puntos (pt):

| Unidad | Equivalencia |
|--------|-------------|
| `in` | 1 in = 72 pt |
| `cm` | 1 cm ≈ 28.35 pt |
| `mm` | 1 mm ≈ 2.83 pt |
| `pt` | 1:1 |
| `px` | 1 px = 0.75 pt (asume 96 dpi) |

---

## Colores

Se aceptan:
- Colores hexadecimales: `#FF0000`, `#RGB`
- Nombres CSS básicos: `Black`, `White`, `Red`, `Green`, `Blue`, `Yellow`, `Orange`, `Purple`, `Navy`, `Teal`, `Gray`, `Silver`, etc.

Los colores no reconocidos se reemplazan por negro.

---

## Namespace RDLC

Se acepta cualquier namespace XML presente en el archivo `.rdlc`. Los formatos SSRS 2005 y 2008+ son compatibles:

- `http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition`
- `http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition`

---

## Ejemplo completo

Dado el siguiente esquema de datos:

```csharp
record Producto(string Nombre, decimal Precio, int Stock);

var productos = new List<Producto>
{
    new("Widget A", 9.99m, 150),
    new("Gadget B", 24.99m, 75),
    new("Module C", 49.99m, 30),
};
```

Y el archivo `catalogo.rdlc` que contiene un `<Tablix>` con `DataSetName="Productos"` y tres columnas cuyos valores son `=Fields!Nombre.Value`, `=Fields!Precio.Value` y `=Fields!Stock.Value`:

```csharp
using FluentReport.Rdlc;

var doc = Document.FromRdlc(
    "catalogo.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Productos"] = productos.Cast<object>()
    },
    parameters: new Dictionary<string, object>
    {
        ["FechaReporte"] = DateTime.Today.ToString("dd/MM/yyyy"),
        ["Empresa"] = "Acme Corp."
    });

// Renderizar a PDF
doc.GeneratePdf("catalogo.pdf");

// También se puede renderizar a Excel si FluentReport.Excel está instalado
// using FluentReport.Excel;
// doc.GenerateExcel("catalogo.xlsx");
```

---

## Limitaciones conocidas

1. **Expresiones**: solo `=Fields!X.Value`, `=Parameters!X.Value` y literales. Cualquier otra expresión (agregados, funciones, condicionales, concatenaciones) se reemplaza por cadena vacía.

2. **Tablix avanzado**: los grupos de filas/columnas (`<RowGroups>`, `<ColumnGroups>`) con jerarquía múltiple no se procesan. El parser detecta filas de detalle por la presencia de `<Group>` en `<TablixRowHierarchy>` y usa heurística de expresión como fallback; estructuras complejas pueden no renderizarse como se espera.

3. **RowSpan**: el modelo (`TableCell.RowSpan`) existe pero el renderer no aplica spanning vertical. Las celdas con `RowSpan > 1` se muestran con su contenido en la primera fila y la celda extra se ignora.

4. **Imágenes embebidas** (`Source = "Embedded"`): el parser no extrae los bytes de la sección `<EmbeddedImages>`. La imagen se omite del output.

5. **Imágenes externas con URL**: solo se soportan rutas de archivo local. Las URLs HTTP/HTTPS no se descargan.

6. **Estilo condicional en RDLC**: estilos definidos mediante expresiones (ej. `=IIF(Fields!Activo.Value, "Bold", "Normal")`) no se evalúan. Se usa el valor literal como texto (y si no es una cadena de estilo válida, el estilo por defecto).

7. **Tamaño del Body**: el campo `<Height>` del `<Body>` en el RDLC se ignora; FluentReport calcula la altura del contenido de forma dinámica.

8. **Multi-sección**: si el `.rdlc` tiene múltiples `<ReportSection>`, cada sección se convierte en una `Page` separada del documento.

9. **Orientación**: la orientación Landscape no se detecta automáticamente. Si el reporte original usa landscape, el tamaño de página resultante (ancho × alto) ya lo reflejará siempre que `PageWidth > PageHeight` en el RDLC.

10. **Sin soporte AOT/trimming**: el evaluador de expresiones usa reflexión para resolver propiedades de POCOs. En proyectos con `PublishTrimmed=true` pueden ser necesarios atributos `[DynamicallyAccessedMembers]` o usar `IDictionary<string, object>` como tipo de fila.

---

## Flujo de procesamiento interno

```
.rdlc (XML)
    │
    ▼
RdlcDocumentFactory.ParseFromFile / ParseFromStream / ParseFromXml
    │
    ├─ DetectNamespace (SSRS 2005 / 2008+)
    │
    ├─ Por cada <ReportSection>:
    │   ├─ ApplyPageDimensions → PageSettings.Size + Margins
    │   ├─ BuildReportItems (Body) → List<IElement>
    │   │   ├─ Textbox → TextElement (+ PaddingElement, BorderElement si aplica)
    │   │   ├─ Line    → LineElement
    │   │   ├─ Image   → ImageElement
    │   │   └─ Tablix  → TableElement
    │   │       ├─ TablixColumns → TableColumnDefinition (FixedWidth en pt)
    │   │       ├─ TablixRowHierarchy → detección header vs. detail
    │   │       └─ Por cada fila de detalle × dataset rows → TableCell
    │   ├─ BuildReportItems (PageHeader) → PageSettings.HeaderElement
    │   └─ BuildReportItems (PageFooter) → PageSettings.FooterElement
    │
    ▼
Document.FromSettings(settings)
    │
    ▼
Document (listo para .GeneratePdf() / .GenerateExcel() / .GenerateImages())
```

---

## Nuevas capacidades del núcleo relacionadas

Junto con `FluentReport.Rdlc`, se añadieron al núcleo de `FluentReport` varias capacidades que también están disponibles directamente en la API fluent:

### `ListElement` — repetición de plantilla por colección

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

### `ChartElement` — gráficos de barras y líneas

```csharp
page.Content().Chart()
    .Type(ChartType.Bar)
    .Title("Ventas por Trimestre")
    .Categories(new[] { "Q1", "Q2", "Q3", "Q4" })
    .AddSeries("Ingresos", new double[] { 100_000, 145_000, 132_000, 198_000 })
    .AddSeries("Costos",   new double[] { 78_000, 91_000, 85_000, 110_000 }, "#FF6666")
    .Height(220);
```

### `SubreportElement` — documento anidado

```csharp
var documentoAnexo = Document.Create(c => { /* ... */ });

page.Content().Column(col =>
{
    col.Item().Text("Anexo A").Bold().FontSize(14);
    col.Item().Subreport(documentoAnexo);
});
```

### `TextStyle` con delegates condicionales

```csharp
col.Item().Text(t =>
{
    t.Span("Estado: ", s => s.Bold = true);
    t.Span(item.Estado, s =>
    {
        s.ColorResolver = () => item.Activo
            ? new ReportColor(0, 150, 0)
            : new ReportColor(200, 0, 0);
    });
});
```

### `TableElement` con `ColSpan`

```csharp
table.Header(h =>
{
    h.Cell(3).Background("#4472C4").Text("Encabezado que abarca 3 columnas").Color("#FFFFFF");
});
```
