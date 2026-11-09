# Changelog

Notable changes to the FluentReport packages. All five packages share one version.

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
