# FluentReport — API Reference

> **Required namespaces**
> ```csharp
> using FluentReport;           // Document
> using FluentReport.Core;      // PageSizes
> using FluentReport.Elements;  // ChartType
> using FluentReport.Styling;   // ReportColor, TextStyle
> using FluentReport.Schema;    // Schema import (optional package)
> ```

---

## Document

| Method | Description |
|--------|-------------|
| `Document.Create(Action<DocumentBuilder>)` | Creates a document from the fluent API |
| `Document.FromSettings(DocumentSettings)` | Creates a document from a pre-built `DocumentSettings` (advanced / translation layers) |
| `.GeneratePdf(path\|stream\|—)` | Renders to PDF; overloads: save to disk, write to stream, return `byte[]` |
| `.GenerateImages(float scale = 1f)` | Renders each page to PNG; returns `IReadOnlyList<byte[]>` |
| `.GenerateExcel(path\|stream\|—)` | Renders to `.xlsx` *(requires FluentReport.Excel)* |
| `.GenerateHtml(path\|stream\|—)` | Renders to full HTML *(requires FluentReport.Html)* |
| `.GenerateHtmlFragment()` | Renders to an inline-style HTML fragment for embedding in emails *(requires FluentReport.Html)* |

For RDLC and YAML/JSON schema import overloads see the [RDLC section](#rdlc-import) and [Schema import section](#schema-import) below.

---

## DocumentBuilder

Received in the `Document.Create(...)` callback.

| Method | Description |
|--------|-------------|
| `.Page(Action<PageBuilder>)` | Adds a page section (chainable for multi-section documents) |

---

## PageBuilder

| Method | Description |
|--------|-------------|
| `.Size(PageSize)` | Predefined page size (e.g. `PageSizes.A4`) |
| `.Size(float width, float height)` | Custom size in points (1 pt = 1/72 in) |
| `.Landscape()` | Swaps width and height |
| `.MarginAll(float)` | Equal margins on all sides |
| `.Margin(top, right, bottom, left)` | Individual margins |
| `.MarginHorizontal(float)` / `.MarginVertical(float)` | Horizontal or vertical margins |
| `.Header()` | Returns `ContainerBuilder` for the page header (repeated on every page) |
| `.Content()` | Returns `ContainerBuilder` for the main body |
| `.Footer()` | Returns `ContainerBuilder` for the page footer (repeated on every page) |

```csharp
container.Page(page =>
{
    page.Size(PageSizes.A4);
    page.MarginAll(40);

    page.Header().Text("My Report").FontSize(16).Bold().AlignCenter();
    page.Content().Column(col => { col.Spacing(8); col.Item().Text("..."); });
    page.Footer().AlignCenter().Text(x =>
    {
        x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages();
    });
});
```

---

## ContainerBuilder

The core composition block. Every table cell, column/row item, and page zone returns a `ContainerBuilder`. It accepts **one child element** — the last content call wins.

### Content methods

| Method | Returns | Description |
|--------|---------|-------------|
| `.Text(string)` | `TextBuilder` | Static text |
| `.Text(Action<DynamicTextBuilder>)` | `TextBuilder` | Multi-span text (page numbers, mixed styles) |
| `.Column(Action<ColumnBuilder>)` | `ContainerBuilder` | Vertical stack |
| `.Row(Action<RowBuilder>)` | `ContainerBuilder` | Horizontal stack |
| `.Table(Action<TableBuilder>)` | `ContainerBuilder` | Data table |
| `.Image(string path)` / `.Image(byte[])` | `ContainerBuilder` | Image from file path or bytes |
| `.Spacer(float size = 0)` | `ContainerBuilder` | Empty space |
| `.Line(float thickness = 1, string? colorHex = null)` | `ContainerBuilder` | Horizontal rule |
| `.PageBreak()` | `ContainerBuilder` | Explicit page break |
| `.List<T>(items, template, spacing)` | `ContainerBuilder` | Repeats a template per collection item |
| `.Chart()` | `ChartBuilder` | Bar or line chart |
| `.Subreport(Document)` | `ContainerBuilder` | Embeds a nested `Document` inline |
| `.Canvas(width, height, draw)` | `ContainerBuilder` | Renders arbitrary vector drawing commands at a fixed size |

### Visual decorators

Chainable before or after the content method:

| Method | Description |
|--------|-------------|
| `.Padding(float)` | Uniform padding |
| `.PaddingHorizontal(float)` / `.PaddingVertical(float)` | Axis padding |
| `.PaddingTop(float)` / `.PaddingBottom(float)` / `.PaddingLeft(float)` / `.PaddingRight(float)` | Side padding |
| `.Background(string hex)` / `.Background(ReportColor)` | Background color |
| `.Border(float width = 1, string? colorHex = null)` | Rectangular border |
| `.AlignLeft()` / `.AlignCenter()` / `.AlignRight()` | Horizontal alignment |

```csharp
col.Item()
   .Background("#E8F4FD")
   .Border(1, "#2196F3")
   .Padding(8)
   .AlignCenter()
   .Text("Highlighted").Bold();
```

---

## TextBuilder

Returned by `ContainerBuilder.Text(string)`.

| Method | Description |
|--------|-------------|
| `.FontSize(float)` | Font size in points |
| `.FontFamily(string)` | Font family |
| `.Bold()` / `.Italic()` / `.Underline()` | Weight and decoration |
| `.Color(string hex)` / `.Color(ReportColor)` | Text color |
| `.AlignLeft()` / `.AlignCenter()` / `.AlignRight()` / `.AlignJustify()` | Alignment |
| `.LineSpacing(float)` | Line height multiplier (default `1.2f`) |
| `.Padding(float)` / `.PaddingVertical(float)` / `.PaddingHorizontal(float)` | Delegates to parent `ContainerBuilder` |

```csharp
col.Item().Text("Section title")
   .FontSize(18).Bold().Color("#1A237E").AlignCenter();
```

---

## DynamicTextBuilder

Returned in the `ContainerBuilder.Text(Action<DynamicTextBuilder>)` callback.

| Method | Description |
|--------|-------------|
| `.Span(string, Action<TextStyle>? = null)` | Static text span with optional style |
| `.CurrentPageNumber(Action<TextStyle>? = null)` | Current page number span |
| `.TotalPages(Action<TextStyle>? = null)` | Total pages span |

```csharp
page.Footer().AlignCenter().Text(x =>
{
    x.Span("Page ");
    x.CurrentPageNumber();
    x.Span(" of ");
    x.TotalPages();
});
```

---

## ColumnBuilder

| Method | Description |
|--------|-------------|
| `.Spacing(float)` | Vertical gap between items in points |
| `.Item()` | Adds an item; returns `ContainerBuilder` |

---

## RowBuilder

| Method | Description |
|--------|-------------|
| `.Spacing(float)` | Horizontal gap between items in points |
| `.Item()` | Relative item with weight 1 (same as `RelativeItem(1)`) |
| `.RelativeItem(float weight = 1)` | Item with proportional width |
| `.FixedItem(float width)` | Item with fixed width in points |

```csharp
page.Content().Row(row =>
{
    row.Spacing(12);
    row.RelativeItem(2).Text("Customer").Bold();
    row.RelativeItem(1).Text("Date");
    row.FixedItem(80).AlignRight().Text("$1,200").Bold();
});
```

---

## TableBuilder

| Method | Description |
|--------|-------------|
| `.ColumnsDefinition(Action<TableColumnDefinitionBuilder>)` | Defines column count and widths |
| `.Header(Action<TableHeaderBuilder>)` | Defines the header row |
| `.Cell(int colSpan = 1)` | Adds a data cell; returns `ContainerBuilder` |
| `.BorderEachCell(float width = 1, string? colorHex = null)` | Applies a border to every cell |

**TableHeaderBuilder** — same as above but cells belong to the header row.

**TableColumnDefinitionBuilder:**

| Method | Description |
|--------|-------------|
| `.RelativeColumn(float weight = 1)` | Column with proportional width |
| `.ConstantColumn(float width)` | Column with fixed width in points |

```csharp
col.Item().Table(table =>
{
    table.ColumnsDefinition(cols =>
    {
        cols.RelativeColumn(3);
        cols.RelativeColumn(2);
        cols.ConstantColumn(80);
    });
    table.Header(h =>
    {
        h.Cell(2).Background("#4472C4").Padding(5).Text("Product / Description").Bold().Color("#FFFFFF");
        h.Cell().Background("#4472C4").Padding(5).Text("Price").Bold().Color("#FFFFFF");
    });
    table.BorderEachCell(0.5f, "#CCCCCC");

    foreach (var item in products)
    {
        table.Cell().Padding(4).Text(item.Name);
        table.Cell().Padding(4).Text(item.Description);
        table.Cell().Padding(4).AlignRight().Text(item.Price.ToString("C"));
    }
});
```

Cells are added left-to-right, top-to-bottom; FluentReport wraps them into rows automatically respecting `colSpan`.

---

## ChartBuilder

Returned by `ContainerBuilder.Chart()`.

| Method | Description |
|--------|-------------|
| `.Type(ChartType)` | `ChartType.Bar` or `ChartType.Line` |
| `.Title(string)` | Chart title |
| `.Height(float)` | Height in points (default `200`) |
| `.Categories(IEnumerable<string>)` | X-axis labels |
| `.AddSeries(string label, IEnumerable<double> values, string? colorHex = null)` | Adds a data series; color defaults to the built-in palette |

```csharp
page.Content().Chart()
    .Type(ChartType.Bar)
    .Title("Quarterly Sales")
    .Height(220)
    .Categories(new[] { "Q1", "Q2", "Q3", "Q4" })
    .AddSeries("Revenue", new double[] { 100_000, 145_000, 132_000, 198_000 })
    .AddSeries("Costs",   new double[] {  78_000,  91_000,  85_000, 110_000 }, "#FF6666");
```

Default series palette (in order): steel blue `(70,130,180)`, red `(220,80,80)`, green `(80,160,80)`, orange `(210,140,40)`, purple `(140,80,200)`, teal `(60,180,180)`.

---

## List\<T\>

Repeats a template for each item in a collection, stacked vertically.

```csharp
page.Content().List(orders, (container, order) =>
{
    container.Column(col =>
    {
        col.Item().Text(order.Description).Bold();
        col.Item().Text($"Total: {order.Total:C}");
    });
}, spacing: 8f);
```

---

## Subreport

Embeds a complete `Document` as an inline element. Its pages render sequentially at the insertion point.

```csharp
var annex = Document.Create(c => { c.Page(/* ... */); });
page.Content().Column(col =>
{
    col.Item().Text("Main body");
    col.Item().Subreport(annex);
});
```

---

## ReportColor

Immutable color struct in `FluentReport.Styling`.

| Form | Description |
|------|-------------|
| `new ReportColor(r, g, b)` | Opaque color |
| `new ReportColor(r, g, b, a)` | Color with alpha (`255` = fully opaque) |
| `ReportColor.FromHex("#RRGGBB")` | From hex string |

Predefined constants: `Black`, `White`, `Gray`, `LightGray`, `Transparent`.

All three forms below are equivalent:
```csharp
.Color("#4472C4")
.Color(ReportColor.FromHex("#4472C4"))
.Color(new ReportColor(68, 114, 196))
```

---

## TextStyle and conditional styling

`TextStyle` holds all style properties for a text span, accessible in `DynamicTextBuilder` callbacks.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FontSize` | `float` | `12` | Font size in points |
| `FontFamily` | `string` | `"sans-serif"` | Font family |
| `Bold` | `bool` | `false` | Bold (static) |
| `Italic` | `bool` | `false` | Italic (static) |
| `Underline` | `bool` | `false` | Underline |
| `Color` | `ReportColor` | `Black` | Color (static) |
| `Alignment` | `TextAlignment` | `Left` | `Left`, `Center`, `Right`, `Justify` |
| `LineSpacing` | `float` | `1.2f` | Line height multiplier |
| `BoldResolver` | `Func<bool>?` | `null` | Overrides `Bold` at render time |
| `ItalicResolver` | `Func<bool>?` | `null` | Overrides `Italic` at render time |
| `ColorResolver` | `Func<ReportColor>?` | `null` | Overrides `Color` at render time |

Resolvers take precedence over the corresponding static property when non-null.

```csharp
col.Item().Text(t =>
{
    t.Span("Status: ", s => s.Bold = true);
    t.Span(item.Status, s =>
    {
        s.ColorResolver = () => item.Active
            ? new ReportColor(0, 150, 0)
            : new ReportColor(200, 0, 0);
    });
});
```

---

## PageSizes

| Constant | Width (pt) | Height (pt) |
|----------|------------|-------------|
| `PageSizes.A4` | 595.28 | 841.89 |
| `PageSizes.A3` | 841.89 | 1190.55 |
| `PageSizes.A5` | 419.53 | 595.28 |
| `PageSizes.Letter` | 612 | 792 |
| `PageSizes.Legal` | 612 | 1008 |

---

## RDLC Import

`FluentReport.Rdlc` converts `.rdlc` (SSRS) files into a standard `Document`.

```shell
dotnet add package FluentReport.Rdlc
```

### API overloads

| Method | Source |
|--------|--------|
| `DocumentRdlcExtensions.FromRdlc(path, datasets?, parameters?)` | File path |
| `DocumentRdlcExtensions.FromRdlcStream(stream, datasets?, parameters?)` | `Stream` (e.g. embedded resource) |
| `DocumentRdlcExtensions.FromRdlcXml(xml, datasets?, parameters?)` | XML `string` |

All three share the same `datasets` and `parameters` parameters:
- `datasets` — `IDictionary<string, IEnumerable<object>>` — rows per dataset name
- `parameters` — `IDictionary<string, object>` — report parameter values

```csharp
using FluentReport.Rdlc;

var doc = DocumentRdlcExtensions.FromRdlc(
    "reports/catalog.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Products"] = products.Cast<object>()
    },
    parameters: new Dictionary<string, object>
    {
        ["Company"] = "Acme Corp."
    });

doc.GeneratePdf("catalog.pdf");
```

**Dataset rows** can be POCOs (public properties) or `IDictionary<string, object>`. Field names are case-insensitive.

### Supported expressions

| Expression | Description |
|------------|-------------|
| `=Fields!FieldName.Value` | Field value from the current data row |
| `=First(Fields!FieldName.Value, "DataSetName")` | Field value from the first row of the named dataset |
| `=Parameters!ParamName.Value` | Report parameter value |
| `=IIF(condition, trueValue, falseValue)` | Conditional expression (supports simple equality checks) |
| `=Switch(cond1, val1, cond2, val2, ...)` | Multi-branch conditional expression |
| Literal (no `=` prefix) | Returned as-is |

Unsupported/unknown expressions (aggregates, `Format`, `Globals!`, concatenations, etc.) are replaced with an empty string.

### Supported RDLC elements

| RDLC element | FluentReport equivalent | Notes |
|---|---|---|
| `<Textbox>` | `TextElement` | FontSize, FontWeight, FontStyle, TextDecoration, Color, TextAlign, BackgroundColor, Padding |
| `<Line>` | `LineElement` | Color, BorderWidth |
| `<Image>` | `ImageElement` | `External` (file path) and `Database` (base64 bytes from field). `Embedded` source is ignored. |
| `<Tablix>` | `TableElement` | Static header rows + detail rows repeated per dataset row. Supports `ColSpan`. |
| `<PageHeader>` / `<PageFooter>` | Document header/footer | |
| `<Page>` dimensions | `PageSettings.Size` + margins | PageWidth, PageHeight, all four margins |

Unsupported elements (`<Chart>`, `<Subreport>`, `<Rectangle>`, `<List>`, gauges, maps, groups, aggregates) are silently ignored.

### Unit conversion

| Unit | Equivalent |
|------|-----------|
| `in` | 72 pt |
| `cm` | ≈ 28.35 pt |
| `mm` | ≈ 2.83 pt |
| `pt` | 1:1 |
| `px` | 0.75 pt (96 dpi) |

### Colors

Accepted formats: `#RRGGBB`, `#RRGGBBAA`, and basic CSS color names (`Black`, `White`, `Red`, etc.).  
3-digit hex (`#RGB`) is **not supported** — always use 6 digits.  
Unrecognized colors fall back to black.

Both SSRS 2005 and 2008+ XML namespaces are accepted automatically.

> For known limitations and the internal processing flow see [rdlc-limitations.md](rdlc-limitations.md).

---

## Schema Import

`FluentReport.Schema` converts schema files (`.yaml`, `.yml`, `.json`) into a standard `Document`.

```shell
dotnet add package FluentReport.Schema
```

### API overloads

| Method | Source |
|--------|--------|
| `DocumentSchemaExtensions.FromSchema(path, dataSources?, parameters?)` | File path |
| `DocumentSchemaExtensions.FromSchemaStream(stream, format?, dataSources?, parameters?)` | `Stream` |
| `DocumentSchemaExtensions.FromSchemaYaml(yaml, dataSources?, parameters?)` | YAML `string` |
| `DocumentSchemaExtensions.FromSchemaJson(json, dataSources?, parameters?)` | JSON `string` |

All four overloads share:
- `dataSources` — `IDictionary<string, IEnumerable<object>>` — rows per data source name.
- `parameters` — `IDictionary<string, object>` — values used in templates like `{{ parameters.period }}`.

### Mapping YAML => fluent API

| YAML node | FluentReport equivalent |
|-----------|-------------------------|
| `type: text` | `.Text(...)` / `TextElement` |
| `type: line` | `.Line(...)` / `LineElement` |
| `type: spacer` | `.Spacer(...)` / `SpacerElement` |
| `type: pageBreak` | `.PageBreak()` / `PageBreakElement` |
| `type: image` | `.Image(...)` / `ImageElement` |
| `type: table` | `.Table(...)` / `TableElement` |
| `type: repeat` | `.List(...)` / `ListElement` |
| `type: groupInstance` | group expansion from `definitions.groups` |
| `styles.*` + `styleRef` | `TextStyle` merge |
| `{{ parameters.* }}` / `{{ row.* }}` | template resolution before render |

### Example: YAML and equivalent fluent shape

YAML:

```yaml
pages:
  - id: p1
    regions:
      content:
        nodes:
          - type: text
            value: "Revenue Report - {{ parameters.period }}"
            styleRef: title
          - type: line
            thickness: 1
          - type: table
            dataSource: sales
            columns:
              - field: region
                header: Region
                width: 2
              - field: revenue
                header: Revenue
                width: 1
                align: right
```

Equivalent fluent shape:

```csharp
page.Content().Column(col =>
{
    col.Item().Text("Revenue Report - ...").FontSize(20).Bold().AlignCenter();
    col.Item().Line(1);
    col.Item().Table(table =>
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(2);
            c.RelativeColumn(1);
        });
        table.Header(h =>
        {
            h.Cell().Text("Region").Bold();
            h.Cell().AlignRight().Text("Revenue").Bold();
        });
        // repeated data rows from dataSources["sales"]
    });
});
```
