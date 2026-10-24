# Migrating to FluentReport 2.0

2.0 narrows the library to **HTML, Excel and RDLC import**. The document model and fluent API stay the same for those uses; the main changes are listed below.

## Removed packages

| Package | Status | Alternative |
|---------|--------|-------------|
| `FluentReport` (PDF, SkiaSharp) | Removed; 1.0.0 remains on NuGet | Render HTML and convert it to PDF with a dedicated tool, or keep 1.0.0 |
| `FluentReport.Schema` (YAML/JSON) | Removed; 1.0.0 remains on NuGet | Build documents with the fluent API |
| `FluentReport.Mcp` | Removed (was never published) | — |

`FluentReport.Html`, `FluentReport.Excel` and `FluentReport.Rdlc` no longer depend on SkiaSharp.

## Removed APIs

| 1.x | 2.0 |
|-----|-----|
| `GeneratePdf`, `GenerateImages`, `DocumentRenderer`, `SkiaFonts` | Removed with the PDF package |
| `.Chart()`, `.Subreport()`, `.Canvas()`, `TextBuilder.Rotate()`, `TextStyle.Rotation` | Removed (PDF only) |
| `IElement.Measure/Render`, `IDrawingCanvas`, `ITextMeasurer`, `MeasureContext`, `RenderContext`, `ElementBase`, `Rect`, `Position`, `Size` | Removed: elements are data; renderers decide the layout |
| `ListElement` | `List()` now produces a `ColumnElement` |
| `LazyElement` | Internal; `Document.Create` returns a tree of concrete elements |
| `TableCell.IsHeader`, `TableCell.RowSpan`, `ImageElement.Fit`, `ImageElement.SourcePath`, `LineElement.Direction` | Removed (never rendered) |
| `TextBuilder.Padding/PaddingVertical/PaddingHorizontal` | Call them on the container: `.Padding(5).Text("x")` |
| `RdlcExpressionEvaluator`, `RdlcDocumentFactory.ParseUnit/ParseColor` | Internal |
| `DocumentRdlcExtensions.FromRdlc(...)` | Still works; prefer `Document.FromRdlc(...)` |

## Behaviour changes

- `Document.Create` throws when no page is added (it produced an empty file before).
- A container accepts one content: a second `Text()`, `Image()`… throws instead of silently replacing the first.
- `Image(path)` throws `FileNotFoundException` when the file does not exist.
- Styles chained after `Text(x => …)` apply to every span.
- HTML and Excel throw `NotSupportedException` for elements they do not know instead of dropping them.
- HTML: page numbers show the page index and count; table column widths, cell alignment and row spacing are applied; output is UTF-8 without BOM.
- Excel: column spans merge cells instead of shifting the row.
- RDLC: values are formatted with `RdlcImportOptions.Culture`, else the report `<Language>`, else the invariant culture — never the machine culture. External images load only from the report folder (file import) or `ExternalImageDirectory`. Static rows after the detail rows become table footer rows. Unsupported items and expressions are listed in `RdlcDocumentFactory.Warnings`.

## New APIs

- `TableBuilder.Footer(...)` and `TableElement.FooterCells`.
- `TableElement.PartitionIntoRows(...)`.
- `ImageElement.MimeType` / `ImageElement.DetectMimeType(...)`.
- `SpacerElement.Height`.
- `RdlcImportOptions` and `RdlcDocumentFactory.Warnings`.
