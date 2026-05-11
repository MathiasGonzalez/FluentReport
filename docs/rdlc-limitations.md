# FluentReport.Rdlc — Known Limitations

## Limitations

1. **Expressions** — Supported: `=Fields!X.Value`, `=First(Fields!X.Value, "DataSet")`, `=Parameters!X.Value`, `=IIF(...)`, `=Switch(...)`, and literals. Other expressions (aggregates, `Format`, `Globals!`, concatenations, arithmetic) are replaced with an empty string.

2. **Advanced Tablix** — `<RowGroups>` / `<ColumnGroups>` with multiple hierarchy levels are not processed. The parser detects detail rows via `<Group>` presence and falls back to expression heuristics; complex structures may not render as expected.

3. **RowSpan** — The model supports `TableCell.RowSpan` but the renderer does not apply vertical spanning. A cell with `RowSpan > 1` renders normally in its own position; cells in subsequent rows that should be merged render as independent empty cells.

4. **Embedded images** (`Source = "Embedded"`) — Bytes from the `<EmbeddedImages>` section are not extracted; the image is omitted from output.

5. **External image URLs** — Only local file paths are supported. HTTP/HTTPS URLs are not fetched.

6. **Conditional styles in RDLC** — Style expressions such as `=IIF(Fields!Active.Value, "Bold", "Normal")` are not evaluated; the literal string is used as the value, which typically results in the default style.

7. **Body height** — The `<Height>` field of `<Body>` is ignored; FluentReport calculates content height dynamically.

8. **Multi-section reports** — Each `<ReportSection>` becomes a separate `Page` in the resulting document.

9. **Landscape detection** — Landscape orientation is not auto-detected from a flag. It is implied when `PageWidth > PageHeight` in the RDLC, so the resulting page size already reflects it.

10. **No AOT/trimming support** — The expression evaluator uses reflection to resolve POCO properties. In projects with `PublishTrimmed=true`, add `[DynamicallyAccessedMembers]` attributes or use `IDictionary<string, object>` as the row type.

---

## Internal processing flow

```
.rdlc (XML)
    │
    ▼
RdlcDocumentFactory.ParseFromFile / ParseFromStream / ParseFromXml
    │
    ├─ DetectNamespace (SSRS 2005 / 2008+)
    │
    ├─ Per <ReportSection>:
    │   ├─ ApplyPageDimensions → PageSettings.Size + Margins
    │   ├─ BuildReportItems (Body) → List<IElement>
    │   │   ├─ Textbox → TextElement
    │   │   ├─ Line    → LineElement
    │   │   ├─ Image   → ImageElement
    │   │   └─ Tablix  → TableElement
    │   │       ├─ TablixColumns → TableColumnDefinition (fixed width in pt)
    │   │       ├─ TablixRowHierarchy → header vs. detail row detection
    │   │       └─ detail rows × dataset rows → TableCell
    │   ├─ BuildReportItems (PageHeader) → PageSettings.HeaderElement
    │   └─ BuildReportItems (PageFooter) → PageSettings.FooterElement
    │
    ▼
Document.FromSettings(settings)
    │
    ▼
Document  →  .GeneratePdf() / .GenerateExcel() / .GenerateImages()
```
