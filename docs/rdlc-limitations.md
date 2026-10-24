# FluentReport.Rdlc — Supported features and limitations

`FluentReport.Rdlc` turns an `.rdlc` (SSRS 2005 / 2008+) definition into a `Document` that the HTML and Excel renderers can render. Anything it cannot import is listed in `RdlcDocumentFactory.Warnings`; with `RdlcImportOptions.StrictExpressions` unsupported expressions throw instead.

## Report items

| RDLC item | Imported as | Notes |
|-----------|-------------|-------|
| `<Textbox>` | `TextElement` | Value or `<Paragraphs>/<TextRuns>` (one span per run). FontSize, FontFamily, FontWeight, FontStyle, TextDecoration, Color, TextAlign (textbox or paragraph), BackgroundColor, Padding. Style values can be expressions. |
| `<Line>` | `LineElement` | Color, BorderWidth. |
| `<Image>` | `ImageElement` | `Embedded`, `Database` (bytes or base64) and `External` (local file inside `ExternalImageDirectory`; URLs are not fetched). Only PNG, JPEG, GIF, WebP and BMP are rendered. |
| `<Tablix>` | `TableElement` | Static rows before the details → header; detail rows repeat per dataset row; static rows after the details → footer (totals). `ColSpan` from `<CellContents>`. |
| `<PageHeader>` / `<PageFooter>` | page header / footer | |
| `<Page>` | page size and margins | |
| `<Rectangle>`, `<Chart>`, `<Subreport>`, `<List>`, gauges, maps | — | Skipped and reported in `Warnings`. |

Absolute positions are approximated: items whose `Top` is within 2 pt are placed side by side in a row (horizontal gaps kept), and vertical gaps of 6 pt or more become spacers.

## Expressions

Values keep their type (number, date, boolean, text) while an expression is evaluated; they become text at the end, formatted with the import culture (`RdlcImportOptions.Culture`, else the report `<Language>`, else invariant — never the machine's regional settings).

| Kind | Supported |
|------|-----------|
| References | `Fields!X.Value`, `Parameters!X.Value` / `.Label`, `Globals!X(.Value)` (values come from the `globals` dictionary) |
| Literals | strings, numbers, `True`, `False`, `Nothing` |
| Operators | `^ * / \ Mod + - &`, `= <> < > <= >=`, `Not And AndAlso Or OrElse Xor` |
| Conditionals | `IIF`, `Switch`, `Choose` |
| Formatting | `Format` (.NET and VB named formats such as `"Currency"`, `"Short Date"`), `FormatNumber`, `FormatCurrency`, `FormatPercent` |
| Conversion / text | `CStr`, `CInt`, `CLng`, `CDec`, `CDbl`, `CBool`, `CDate`, `Len`, `Trim`, `LTrim`, `RTrim`, `UCase`, `LCase`, `Left`, `Right`, `Mid`, `Replace`, `IsNothing`, `Now`, `Today` |
| Aggregates | `Sum`, `Count`, `CountDistinct`, `CountRows`, `Avg`/`Average`, `Min`, `Max`, `First`, `Last` (over a named dataset, or the only dataset) |

Not supported (reported as warnings): `RowNumber`, `RunningValue`, `Previous`, `Lookup`, `ReportItems!`, `User!`, custom code (`Code.`), group scopes in aggregates, and other VB functions.

`Globals!PageNumber` / `TotalPages` are not computed (evaluation happens before rendering); supply values through `globals` if needed, or use the page-number spans of the fluent API.

## Other limitations

- Row and column groups: the importer distinguishes header, detail and footer rows; nested groups and group headers are not reproduced.
- `RowSpan` is ignored.
- `<Body><Height>` is ignored; content height follows the content.
- Each `<ReportSection>` becomes a page.
- Reflection is used for POCO rows; for trimmed/AOT apps use dictionary rows.
