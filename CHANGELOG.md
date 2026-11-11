# Changelog

Notable changes to the FluentReport packages. All five packages share one version.

## Unreleased

### Fixed

PDF and PNG

- A page break at the start of a nested column is no longer ignored when the page already has
  content above it.
- A page break at the end of a column inside `Padding`, `Border`, `Background` or an alignment no
  longer adds a blank page.
- A decorated page break (`Item().PaddingTop(5).PageBreak()`) breaks the page. Before, it was drawn
  as empty space. The decoration of a page break is not drawn.
- After a page break that ends a nested column, the next item starts at the top of the new page
  instead of one column spacing below it.
- A word longer than the line is cut between characters as the reader sees them: an emoji or a
  letter with a combining accent is no longer split in two.
- Charts: a legend that needs several rows takes its height from the plot instead of drawing below
  the chart, over the next element. `NaN` and infinite values are gaps (a missing bar, a break in
  the line) instead of leaving the chart without a scale.

HTML

- A line break (`\n`) in a text becomes `<br>`, as the PDF breaks the line there. Before, the text
  ran on in one line.
- Colors keep their alpha: `ReportColor.Transparent` is written as `transparent` and translucent
  colors as `rgba()`. Before, alpha was dropped, so a transparent background came out black.
- An image inside `AlignCenter()` or `AlignRight()` is centered or right-aligned.

Excel

- A translucent color is written as it looks on a white sheet, since Excel has no alpha; a fully
  transparent background leaves the cell unfilled.
- `FontFamily` is applied to texts with one span, not only to texts with several. The default
  `sans-serif`, which is not an Excel font, is no longer written as a font name.

RDLC import

- Page margins of `0` are applied. Before, they were taken as missing and the default 40 pt was
  used, which shifted labels and roll-paper tickets. A missing or unreadable margin still keeps
  the default.
- Each `<Paragraph>` of a textbox starts on its own line. Before, they were joined on one line.
- An expression with a malformed or out-of-range number (`=1.2.3`, `=1E+30`,
  `=CInt(99999999999999999999)`, `=Left("abc", 99999999999)`) no longer aborts the import with a
  `FormatException` or `OverflowException`. It is reported like any other expression that cannot
  be evaluated: a warning, or `NotSupportedException` with `StrictExpressions`. Counts and
  positions out of range saturate (`Left("abc", 99999999999)` is `"abc"`).
- `FormatNumber`, `FormatCurrency` and `FormatPercent` use 2 decimals for a negative count, as VB
  does for `-1`, and at most 99.
- `2 ^ -1` is accepted.
- A field read from an object with members that differ only in case (`Total` and `total`) picks
  the exact name instead of failing.

Element model

- `TextSpan.Style` of a span added with `AddSpan(text, configure)` returns the same instance on
  every read, so `span.Style.Bold = true` is kept; before, each read built a new style and the
  change was lost. The style is still derived again when the element's style changes, until it is
  assigned or changed through the span, which fixes it. `configure` now runs once per derivation,
  not on every read.

## 3.2.0

### Added

- `page.ContinuousSize(width)`: a page that grows to the height of its content, for receipts on roll
  paper. The whole content goes on one page with no blank space below it (PDF and PNG).
- `PdfOptions.SubsetFonts` (default `true`): PDFs embed only the glyphs they use instead of whole
  font files. A one-page document goes from several hundred KB to a few tens. Applies to TrueType
  fonts; others are embedded whole as before. Set it to `false` to embed whole fonts.
- XML documentation for the whole public API, and a package icon, project URL and symbol packages
  (`.snupkg`) with Source Link.

### Fixed

- A background or border on a container with aligned text now fills the container. Before, it
  shrank to the text: a table header cell written as
  `Cell().Background(..).Padding(4).AlignCenter().Text(..)` was only shaded behind its text.
  Aligned content that is not text (an image, a nested column) is unchanged: it is still shrunk to
  its own width together with its background and border.
