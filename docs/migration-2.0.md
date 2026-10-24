# Migrating to FluentReport 2.0

2.0 keeps the fluent API and the PDF, HTML, Excel and RDLC packages, rewrites the PDF layout engine (real pagination) and removes the YAML schema importer and the MCP server.

## Removed packages

| Package | Status | Alternative |
|---------|--------|-------------|
| `FluentReport.Schema` (YAML/JSON) | Removed; 1.0.0 remains on NuGet | Build documents with the fluent API |
| `FluentReport.Mcp` | Removed (was never published) | — |

`FluentReport.Html`, `FluentReport.Excel` and `FluentReport.Rdlc` no longer depend on SkiaSharp; only `FluentReport` (PDF) does.

## Removed or changed APIs

| 1.x | 2.0 |
|-----|-----|
| `IElement.Measure/Render`, `IDrawingCanvas`, `ITextMeasurer`, `MeasureContext`, `RenderContext`, `IPageImagesProvider`, `ElementBase`, `Rect`, `Position`, `Size` | Removed: elements are data and each renderer lays them out. Write a custom renderer by walking the element tree. |
| `DocumentRenderer` (public, returned `SKImage`) | Internal. Use `GeneratePdf`, `GenerateImages`, `GetPdfPageCount`. |
| `SkiaFonts.TypefaceFactory` | Still honoured; prefer `PdfOptions.FontResolver`. Returned typefaces are no longer disposed, so return shared instances. |
| `.Canvas(width, height, Action<IDrawingCanvas, Position, Size>)` | `.Canvas(width, height, Action<SKCanvas, SKRect>)` in the `FluentReport` package |
| `.Subreport(Document)` | Same call; now inserts the nested content as vector (was a 72 dpi image) and also works in HTML and Excel |
| `ListElement` | `List()` produces a `ColumnElement` |
| `LazyElement` | Internal; `Document.Create` returns a tree of concrete elements |
| `TableCell.IsHeader`, `TableCell.RowSpan`, `ImageElement.Fit`, `ImageElement.SourcePath`, `LineElement.Direction` | Removed (never rendered) |
| `TextBuilder.Padding/PaddingVertical/PaddingHorizontal` | Call them on the container: `.Padding(5).Text("x")` |
| `RdlcExpressionEvaluator`, `RdlcDocumentFactory.ParseUnit/ParseColor` | Internal |
| `DocumentRdlcExtensions.FromRdlc(...)` | Still works; prefer `Document.FromRdlc(...)` |

## Behaviour changes

**PDF**
- Content paginates: nested or decorated columns, long text and tables split across pages; table headers repeat and footer rows stay on the last page. In 1.x only the top-level items of a bare `Column` moved to new pages and everything else overflowed.
- Text with several spans wraps; long words break; page numbers align with their real width.
- Row items take the row's full height (backgrounds and borders line up).
- Images keep their aspect ratio with only one fixed side, JPEGs are embedded without recompression, and an image repeated on several pages is embedded once.
- `GeneratePdf(path)` writes the file only when rendering succeeds.

**All renderers**
- `Document.Create` throws when no page is added (it produced an empty file before).
- A container accepts one content: a second `Text()`, `Image()`… throws instead of silently replacing the first.
- `Image(path)` throws `FileNotFoundException` when the file does not exist.
- Styles chained after `Text(x => …)` apply to every span.
- Unknown element types throw `NotSupportedException`; charts and canvases render their fallback in HTML and Excel.

**HTML / Excel**
- HTML: page numbers show the page index and count; table column widths, cell alignment and row spacing are applied; CSS numbers ignore the machine culture; output is UTF-8 without BOM.
- Excel: column spans merge cells instead of shifting the row; page numbers are filled in.

**RDLC**
- Values are formatted with `RdlcImportOptions.Culture`, else the report `<Language>`, else the invariant culture — never the machine culture.
- External images load only from the report folder (file import) or `ExternalImageDirectory`.
- Static rows after the detail rows become table footer rows. Unsupported items and expressions are listed in `RdlcDocumentFactory.Warnings`.

## New APIs

- `PdfOptions` (`FontResolver`, `Title`, `Author`) and `GetPdfPageCount()`.
- `TableBuilder.Footer(...)` / `TableElement.FooterCells` and `TableElement.PartitionIntoRows(...)`.
- `ChartBuilder.Fallback(text)`, `IRendererSpecificElement`, `ContainerBuilder.Element(IElement)`.
- `ImageElement.MimeType` / `ImageElement.DetectMimeType(...)`, `SpacerElement.Height`.
- `RdlcImportOptions` and `RdlcDocumentFactory.Warnings`.
