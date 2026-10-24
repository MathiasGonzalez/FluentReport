# FluentReport — API Reference

> **Required namespaces**
> ```csharp
> using FluentReport;           // Document, GeneratePdf (FluentReport package)
> using FluentReport.Core;      // PageSizes, DocumentSettings
> using FluentReport.Elements;  // ChartType
> using FluentReport.Styling;   // ReportColor, TextStyle
> using FluentReport.Html;      // GenerateHtml (FluentReport.Html package)
> using FluentReport.Excel;     // GenerateExcel (FluentReport.Excel package)
> using FluentReport.Rdlc;      // Document.FromRdlc (FluentReport.Rdlc package)
> ```

---

## Document

| Member | Description |
|--------|-------------|
| `Document.Create(Action<DocumentBuilder>)` | Builds a document with the fluent API. At least one page is required. |
| `Document.FromSettings(DocumentSettings)` | Wraps a model built by hand or by an importer. At least one page is required. |
| `.Settings` | The document model (`DocumentSettings` → pages → elements). |
| `.GeneratePdf(path\|stream\|—, options?)` | PDF *(FluentReport)* — see [PDF output](#pdf-output). |
| `.GenerateImages(scale = 1, options?)` | One PNG per page *(FluentReport)*. |
| `.GetPdfPageCount(options?)` | Number of PDF pages *(FluentReport)*. |
| `.GenerateHtml(path\|stream\|—, options?)` | Full HTML document *(FluentReport.Html)*. |
| `.GenerateHtmlFragment(options?)` | HTML fragment for an email body *(FluentReport.Html)*. |
| `.GenerateExcel(path\|stream\|—)` | `.xlsx` workbook *(FluentReport.Excel)*. |
| `Document.FromRdlc(path, …)` / `FromRdlcStream` / `FromRdlcXml` | RDLC import *(FluentReport.Rdlc)* — see [RDLC import](#rdlc-import). |

---

## DocumentBuilder

| Method | Description |
|--------|-------------|
| `.Page(Action<PageBuilder>)` | Adds a page section. In PDF its content flows over as many pages as needed with the header and footer repeated; in HTML each section is a block separated by a divider; in Excel a worksheet. |

## PageBuilder

| Method | Description |
|--------|-------------|
| `.Size(PageSize)` / `.Size(float width, float height)` | Page size in points (1 pt = 1/72 in). |
| `.Landscape()` | Swaps width and height. |
| `.MarginAll(float)` / `.Margin(top, right, bottom, left)` / `.MarginHorizontal(float)` / `.MarginVertical(float)` | Margins in points; in HTML they become cell padding. |
| `.Header()` / `.Content()` / `.Footer()` | Return the `ContainerBuilder` of each region. |

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

`CurrentPageNumber()` / `TotalPages()` resolve to the document page and the page count (in Excel: the worksheet's page).

---

## ContainerBuilder

Every region, column/row item and table cell is a `ContainerBuilder`. It holds **one** content element; calling a second content method throws `InvalidOperationException` — use `Column()` or `Row()` to place several elements.

### Content

| Method | Returns | Description |
|--------|---------|-------------|
| `.Text(string)` | `TextBuilder` | Text. |
| `.Text(Action<DynamicTextBuilder>)` | `TextBuilder` | Text made of spans (mixed styles, page numbers). |
| `.Column(Action<ColumnBuilder>)` | `ContainerBuilder` | Vertical stack. |
| `.Row(Action<RowBuilder>)` | `ContainerBuilder` | Horizontal stack. |
| `.Table(Action<TableBuilder>)` | `ContainerBuilder` | Table. |
| `.List<T>(items, template, spacing = 0)` | `ContainerBuilder` | One item per element of `items`, stacked vertically. |
| `.Image(string path)` / `.Image(byte[])` | `ContainerBuilder` | PNG, JPEG, GIF, WebP or BMP. `Image(path)` throws if the file does not exist. HTML embeds it as a data URI; Excel skips images. Size: natural size (1 px = 1 pt) capped to the width; a single fixed side keeps the aspect ratio. |
| `.Spacer(float height = 0)` | `ContainerBuilder` | Empty vertical space. |
| `.Line(float thickness = 1, string? colorHex = null)` | `ContainerBuilder` | Horizontal rule. |
| `.PageBreak()` | `ContainerBuilder` | New page in PDF (at any depth); a divider in HTML; a new worksheet in Excel (top level of `Content()`). |
| `.Chart()` | `ChartBuilder` | Bar or line chart (PDF; HTML/Excel render its fallback). |
| `.Subreport(Document)` | `ContainerBuilder` | Inserts the content of another document's pages (headers/footers not repeated). |
| `.Canvas(width, height, (SKCanvas, SKRect) => …)` | `ContainerBuilder` | Custom vector drawing, PDF only *(FluentReport)*. |
| `.Element(IElement)` | `ContainerBuilder` | Custom element (the renderer must know it). |

### Decorators

Order-independent; they can be called before or after the content method.

| Method | Description |
|--------|-------------|
| `.Padding(float)`, `.PaddingHorizontal/Vertical(float)`, `.PaddingTop/Bottom/Left/Right(float)` | Padding. |
| `.Background(string hex)` / `.Background(ReportColor)` | Background color. |
| `.Border(float width = 1, string? colorHex = null)` | Border. |
| `.AlignLeft()` / `.AlignCenter()` / `.AlignRight()` | Horizontal alignment of the content. |

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

| Method | Description |
|--------|-------------|
| `.FontSize(float)` | Size in points (HTML uses the same number in px). |
| `.FontFamily(string)` | Font family. |
| `.Bold()` / `.Italic()` / `.Underline()` | Weight and decoration. |
| `.Color(string hex)` / `.Color(ReportColor)` | Text color. |
| `.AlignLeft()` / `.AlignCenter()` / `.AlignRight()` / `.AlignJustify()` | Text alignment (inside a table cell it aligns the cell). |
| `.LineSpacing(float)` | Line height multiplier (default `1.2`). |
| `.Rotate(float degrees)` | Rotation, counter-clockwise. PDF only; layout uses the unrotated box. |

The style applies to every span of `Text(x => …)` as well; a span's own settings win:

```csharp
page.Footer().Text(x =>
{
    x.Span("Page ");
    x.CurrentPageNumber(s => s.Bold = true);
}).FontSize(8).Color("#666666");   // both spans are 8 pt grey; the number is also bold
```

## DynamicTextBuilder

| Method | Description |
|--------|-------------|
| `.Span(string, Action<TextStyle>? = null)` | Text span. |
| `.CurrentPageNumber(Action<TextStyle>? = null)` | Current page number. |
| `.TotalPages(Action<TextStyle>? = null)` | Page count. |

---

## ColumnBuilder / RowBuilder

| Method | Description |
|--------|-------------|
| `ColumnBuilder.Spacing(float)` / `.Item()` | Gap between items; adds an item. |
| `RowBuilder.Spacing(float)` | Gap between items. |
| `RowBuilder.RelativeItem(float weight = 1)` / `.Item()` | Proportional width. |
| `RowBuilder.FixedItem(float width)` | Fixed width in points. |

---

## TableBuilder

| Method | Description |
|--------|-------------|
| `.ColumnsDefinition(c => c.RelativeColumn(w) / c.ConstantColumn(pt))` | Columns and their widths. |
| `.Header(h => h.Cell(colSpan))` | Header rows (`<thead>`, bold in Excel). |
| `.Cell(int colSpan = 1)` | Data cell. |
| `.Footer(f => f.Cell(colSpan))` | Rows after the data (totals, notes): `<tfoot>` in HTML. |
| `.BorderEachCell(float width = 1, string? colorHex = null)` | Border on every cell. |

Cells are listed left-to-right, top-to-bottom and wrap into rows automatically, respecting `colSpan`.

```csharp
col.Item().Table(table =>
{
    table.ColumnsDefinition(cols => { cols.RelativeColumn(3); cols.ConstantColumn(80); });
    table.Header(h =>
    {
        h.Cell().Background("#4472C4").Padding(5).Text("Product").Bold().Color("#FFFFFF");
        h.Cell().Background("#4472C4").Padding(5).Text("Price").Bold().Color("#FFFFFF");
    });
    foreach (var item in products)
    {
        table.Cell().Padding(4).Text(item.Name);
        table.Cell().Padding(4).Text(item.Price.ToString("N2")).AlignRight();
    }
    table.Footer(f =>
    {
        f.Cell().Padding(4).Text("Total").Bold();
        f.Cell().Padding(4).Text(total.ToString("N2")).Bold().AlignRight();
    });
    table.BorderEachCell(0.5f, "#CCCCCC");
});
```

---

## ChartBuilder

| Method | Description |
|--------|-------------|
| `.Type(ChartType.Bar \| ChartType.Line)` | Chart type. |
| `.Title(string)` / `.Height(float)` | Title; height in points (default 200). |
| `.Categories(IEnumerable<string>)` | X-axis labels. |
| `.AddSeries(label, values, colorHex?)` | Data series; color defaults to a palette. Bars grow from zero, so negative values point down. |
| `.Fallback(string)` | Text HTML and Excel show instead of the chart. |

---

## Styling

`ReportColor` (in `FluentReport.Styling`): `new ReportColor(r, g, b[, a])`, `ReportColor.FromHex("#RRGGBB" | "#RRGGBBAA")`, constants `Black`, `White`, `Gray`, `LightGray`, `Transparent`.

`TextStyle` properties: `FontSize` (12), `FontFamily` ("sans-serif" → the HTML font option), `Bold`, `Italic`, `Underline`, `Color`, `Alignment`, `LineSpacing` (1.2), `Rotation` (PDF only), and the optional `BoldResolver`, `ItalicResolver`, `ColorResolver` delegates that override the static value when the document is rendered.

`PageSizes`: `A3`, `A4`, `A5`, `Letter`, `Legal`.

---

## PDF output

Content flows across pages: columns split between items (also nested or inside padding/background/border), text between lines, and tables between rows, repeating header rows and keeping footer rows on the last page. Rows, images, charts and canvases move to the next page whole; one taller than a page is cut at the footer.

`PdfOptions`:

| Option | Description |
|--------|-------------|
| `FontResolver` | `Func<TextStyle, SKTypeface?>` — e.g. embedded fonts. Return `null` for the system font. Typefaces are cached per render and never disposed. |
| `Title` / `Author` | PDF metadata. |

Fonts are embedded whole (SkiaSharp does not subset), adding a few hundred KB per face.

---

## Renderer options

`HtmlRendererOptions`: `MaxWidth` (600 px, `null` = 100 %), `FontFamily` (default font stack), `PageDividerStyle`, `OutlookCompatible`.

Numbers in the generated CSS always use `.` as decimal separator, whatever the machine culture.

A custom `IElement` that a renderer does not know throws `NotSupportedException` instead of being dropped; an `IRendererSpecificElement` (charts, canvases) renders its `Fallback` in renderers that cannot draw it.

---

## RDLC import

```csharp
using FluentReport.Rdlc;

var doc = Document.FromRdlc(
    "reports/catalog.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>> { ["Products"] = products.Cast<object>() },
    parameters: new Dictionary<string, object> { ["Company"] = "Acme Corp." },
    options: new RdlcImportOptions { Culture = new CultureInfo("es-UY") });

doc.GeneratePdf("catalog.pdf");
```

| Entry point | Source |
|-------------|--------|
| `Document.FromRdlc(path, datasets?, parameters?, globals?, options?)` | File |
| `Document.FromRdlcStream(stream, …)` | Stream |
| `Document.FromRdlcXml(xml, …)` | XML string |
| `new RdlcDocumentFactory(…).ParseFromFile/Stream/Xml(…)` | Same, plus `factory.Warnings` |

Rows can be POCOs or dictionaries; field names are case-insensitive.

`RdlcImportOptions`:

| Option | Default | Description |
|--------|---------|-------------|
| `Culture` | report `<Language>`, else invariant | Formats values and reads numbers/dates given as text. Never the machine culture. |
| `LoadExternalImages` | `true` | Load `<Image Source="External">`. |
| `ExternalImageDirectory` | the `.rdlc` folder (file import only) | External images are read only from inside this directory. |
| `StrictExpressions` | `false` | Throw instead of rendering empty text for unsupported expressions. |

Supported report items, expressions and known limitations: [rdlc-limitations.md](rdlc-limitations.md).
