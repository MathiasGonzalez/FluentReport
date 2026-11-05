# FluentReport.Rdlc — Supported features and limitations

`FluentReport.Rdlc` turns an `.rdlc` (SSRS 2005 / 2008+) definition and its data into a `Document` that renders to PDF, HTML and Excel. Anything it cannot import is listed (once) in `RdlcDocumentFactory.Warnings`; with `RdlcImportOptions.StrictExpressions` unsupported expressions throw instead.

## Report items

| RDLC item | Imported as | Notes |
|-----------|-------------|-------|
| `<Textbox>` | `TextElement` | Value or `<Paragraphs>/<TextRuns>` (one span per run). FontSize, FontFamily, FontWeight, FontStyle, TextDecoration, Color, TextAlign (textbox or paragraph), BackgroundColor, Padding, borders, `Format`. Style values can be expressions. |
| `<Rectangle>` | `ColumnElement` in a border/background | Its items are laid out like the body; the designed height is kept when it is taller than the content. |
| `<Line>` | `LineElement` | Color and width (`Border` or legacy `BorderWidth`). |
| `<Image>` | `ImageElement` | `Embedded`, `Database` (bytes or base64) and `External` (local file inside `ExternalImageDirectory`; URLs are not fetched). Only PNG, JPEG, GIF, WebP and BMP are rendered. |
| `<Tablix>` (table, list) | `TableElement` | Row groups, details, sorting, filters, hidden rows/columns, `ColSpan`, `NoRowsMessage`. See below. |
| `<PageHeader>` / `<PageFooter>` | page header / footer | `Globals!PageNumber` / `TotalPages` render the real page numbers. |
| `<Page>` | page size and margins | |
| `<Chart>`, `<Subreport>`, gauges, maps | — | Skipped and reported in `Warnings`. |

Absolute positions are approximated: items whose `Top` is within 2 pt are placed side by side in a row (horizontal gaps kept), and vertical gaps of 6 pt or more become spacers. `PageBreak` with `Start` / `End` / `StartAndEnd` on a body item adds a page break.

### Styles

- **Format**: the `Format` style property (on the text run, else the textbox) is applied to numbers and dates with the import culture, e.g. `N2`, `C`, `#,0.00`, `dd/MM/yyyy`. Text values are left as they are.
- **Borders**: `Border` plus `TopBorder` / `RightBorder` / `BottomBorder` / `LeftBorder` (RDL 2008+) and `BorderStyle` / `BorderColor` / `BorderWidth` (RDL 2005). Every style other than `None` is drawn solid. In tables, cell borders line up into a grid.
- **Visibility**: `<Visibility><Hidden>` (literal or expression) on report items, tablix row members and tablix column members. Hidden items take no space. Toggles (drill-down) keep their initial state.
- Not imported: `VerticalAlign`, `LineHeight`, `WritingMode`, `CanGrow`/`CanShrink` (text always grows), border styles (dotted, dashed, double).

## Tablix

The row hierarchy is walked the way SSRS does:

- Static rows before the first group are the table header, repeated on every page. Static rows after the last group are the footer (grand totals), shown once at the end.
- A group with `<GroupExpressions>` renders once per distinct value, in first-appearance order or by its `<SortExpressions>`. Its static child rows (group header, subtotal) render once per group. Aggregates in them cover that group's rows.
- A details group (`<Group>` without expressions) renders once per row. `SortExpressions` on the member or on the tablix order the rows.
- `<Filters>` on the dataset, the tablix and groups: `Equal`, `NotEqual`, `GreaterThan(OrEqual)`, `LessThan(OrEqual)`, `In` (multi-value parameters included), `Between`, `Like`. `TopN`/`BottomN`/`TopPercent`/`BottomPercent` are reported and ignored.
- A tablix nested inside a group cell (e.g. a list with an inner table) shows the rows of that group when it uses the same dataset.

Not supported: column groups (matrix) — the columns are rendered once and a warning is reported; `RowSpan`; page breaks between group instances; `RepeatOnNewPage`/`KeepWithGroup` (the header always repeats).

## Expressions

Values keep their type (number, date, boolean, text) while an expression is evaluated; they become text at the end, formatted with the import culture (`RdlcImportOptions.Culture`, else the report `<Language>`, else invariant — never the machine's regional settings). Text comparisons and sorting use the same culture's collation.

| Kind | Supported |
|------|-----------|
| References | `Fields!X.Value`, `Parameters!X.Value` / `.Label`, `Globals!X(.Value)` |
| Literals | strings, numbers, `True`, `False`, `Nothing` |
| Operators | `^ * / \ Mod + - &`, `= <> < > <= >=`, `Not And AndAlso Or OrElse Xor` |
| Conditionals | `IIF`, `Switch`, `Choose` |
| Formatting | `Format` (.NET and VB named formats such as `"Currency"`, `"Short Date"`), `FormatNumber`, `FormatCurrency`, `FormatPercent` |
| Conversion / text | `CStr`, `CInt`, `CLng`, `CDec`, `CDbl`, `CBool`, `CDate`, `Len`, `Trim`, `LTrim`, `RTrim`, `UCase`, `LCase`, `Left`, `Right`, `Mid`, `Replace`, `IsNothing`, `Now`, `Today` |
| Aggregates | `Sum`, `Count`, `CountDistinct`, `CountRows`, `Avg`/`Average`, `Min`, `Max`, `First`, `Last` |
| Row functions | `RowNumber`, `RunningValue(expr, Sum, scope)`, `Previous` |

Aggregate scope follows SSRS: without a scope argument an aggregate covers the current scope — the row in a detail row, the group in a group header or footer, the tablix in its header or footer, the only dataset outside a tablix. A scope argument may name a group, a tablix or a dataset. `RowNumber` and `RunningValue` count rows in the order they are displayed.

Globals: `PageNumber`, `TotalPages` (and `Overall…`) become page-number spans resolved at render time. `ExecutionTime` defaults to the import time and `ReportName` to the file name. Other globals come from the `globals` dictionary, which also overrides these defaults.

Not supported (reported as warnings): `Lookup`, `LookupSet`, `MultiLookup`, `ReportItems!`, `User!`, custom code (`Code.`), `InScope`, `Level`, and other VB functions. Page numbers used inside arithmetic or `Format` are not computed.

## Other limitations

- `<Body><Height>` is ignored; content height follows the content.
- Each `<ReportSection>` becomes a page.
- Reflection is used for POCO rows; for trimmed/AOT apps use dictionary rows.
- The HTML renderer does not draw borders on table cells (PDF does).
