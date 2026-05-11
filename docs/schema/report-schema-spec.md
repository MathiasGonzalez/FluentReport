# FluentReport Report Schema v1 (Proposal)

> **Status:** This document is an early design proposal. It was superseded during implementation.
> The normative schema actually exported by the editor and consumed by `FluentReport.Schema` is defined in
> [docs/editor-yaml-schema.md](./editor-yaml-schema.md).
>
> **Sections of this proposal that were NOT implemented:**
> - `type: column` and `type: row` as composable node types inside regions (the importer only supports flat node lists with absolute frames)
> - `rows.source` / `rows.cells` table syntax (replaced by `dataSource` + `columns[]`)
> - `dataSources[*].requiredFields`
> - `visibleWhen` conditional rendering
>
> **What was implemented** aligns with the editor schema: `text`, `line`, `spacer`, `pageBreak`, `image`, `table`, `repeat`, `groupInstance` nodes; `styles`, `styleRef`, `parameters`, `dataSources`, `rendererOptions`, and pipe functions (`upper`, `lower`, `trim`, `currency`, `number(fmt)`, `date(fmt)`).


## 1. Goal

Define a declarative format for modeling reports without writing fluent C#, so that it can:

- be validated with a schema (JSON Schema),
- be translated into `DocumentSettings`,
- be rendered by the existing backends (PDF, HTML, Excel),
- maintain explicit compatibility and versioning.

This document proposes **Schema v1** as the basis for a new importer package (`FluentReport.Schema`).

## 2. v1 scope

Included:

- document, pages, margins, and page size,
- `header`, `content`, and `footer`,
- `column`, `row`, and `table` containers,
- `text`, `line`, `spacer`, `image`, and `pageBreak` elements,
- basic styling (typography, color, background, border, padding, alignment),
- data binding through restricted expressions.

Not included in v1:

- arbitrary expression engines (no C# or JS execution),
- advanced tablix layout (complex RDLC-like structures),
- complex renderer-specific conditional rules,
- third-party runtime custom components.

## 3. File format

Both serializers should be supported:

- YAML (better for human authoring): `.frpt.yaml`
- JSON (better for tooling/integration): `.frpt.json`

Both map to the same internal DTO structure.

## 4. Top-level structure

```yaml
kind: FluentReport
schemaVersion: 1
name: sales-summary
metadata:
  title: Sales Summary
  author: BI Team
  tags: [sales, monthly]

pageDefaults:
  size: A4
  orientation: portrait
  margin:
    top: 40
    right: 40
    bottom: 40
    left: 40

dataSources:
  sales:
    type: array
    requiredFields: [region, revenue, month]

parameters:
  companyName:
    type: string
    required: true
  period:
    type: string
    required: true

styles:
  heading:
    fontSize: 18
    bold: true
    color: "#1A237E"
  tableHeader:
    fontSize: 11
    bold: true
    color: "#FFFFFF"
    background: "#4472C4"

pages:
  - id: main
    content:
      type: column
      spacing: 8
      items: []
```

## 5. Page model

Each page defines these zones:

- `header`
- `content` (required)
- `footer`

```yaml
pages:
  - id: invoice
    size: A4
    orientation: portrait
    margin:
      top: 30
      right: 30
      bottom: 30
      left: 30
    header:
      type: text
      value: "{{ parameters.companyName }}"
      styleRef: heading
    content:
      type: column
      spacing: 6
      items: []
    footer:
      type: text
      runs:
        - value: "Page "
        - token: currentPage
        - value: " / "
        - token: totalPages
```

## 6. Node types

All nodes share these common fields:

- `type` (required)
- `id` (optional)
- `style` (inline, optional)
- `styleRef` (optional)
- `padding`, `background`, `border`, `align` (optional)
- `visibleWhen` (optional, simple boolean expression)

### 6.1 `text`

```yaml
type: text
value: "Summary {{ parameters.period }}"
# or span-based formatting:
runs:
  - value: "Page "
  - token: currentPage
  - value: " of "
  - token: totalPages
```

Rules:

- `value` or `runs` must exist.
- `runs[].token` only allows `currentPage` and `totalPages`.

### 6.2 `column`

```yaml
type: column
spacing: 8
items:
  - type: text
    value: "Item 1"
  - type: line
```

### 6.3 `row`

```yaml
type: row
spacing: 12
items:
  - width:
      mode: relative
      value: 2
    node: { type: text, value: "Left" }
  - width:
      mode: fixed
      value: 100
    node: { type: text, value: "Right" }
```

### 6.4 `table`

```yaml
type: table
columns:
  - { mode: relative, value: 3 }
  - { mode: relative, value: 2 }
  - { mode: fixed, value: 80 }
header:
  - colSpan: 2
    node: { type: text, value: "Product / Description", styleRef: tableHeader }
  - node: { type: text, value: "Price", styleRef: tableHeader }
rows:
  source: sales
  cells:
    - { node: { type: text, value: "{{ row.region }}" } }
    - { node: { type: text, value: "{{ row.month }}" } }
    - { node: { type: text, value: "{{ row.revenue | currency }}", align: right } }
cellBorder:
  width: 0.5
  color: "#CCCCCC"
```

Rules:

- `columns` is required.
- If `rows.source` exists, it must exist in `dataSources`.
- Each resulting row must complete the grid while respecting `colSpan`.

### 6.5 `image`

```yaml
type: image
source:
  mode: path
  value: "assets/logo.png"
fit: contain
```

### 6.6 `line`

```yaml
type: line
thickness: 1
color: "#D0D0D0"
```

### 6.7 `spacer`

```yaml
type: spacer
size: 12
```

### 6.8 `pageBreak`

```yaml
type: pageBreak
```

## 7. Styles

### 7.1 `styleRef` + inline `style`

- `styleRef`: applies a named style from `styles`.
- `style`: inline override on top of the referenced style.

```yaml
styles:
  body:
    fontSize: 11
    color: "#222222"

content:
  type: text
  styleRef: body
  style:
    bold: true
```

### 7.2 v1 style properties

- `fontSize: number`
- `fontFamily: string`
- `bold: boolean`
- `italic: boolean`
- `underline: boolean`
- `color: #RRGGBB`
- `background: #RRGGBB`
- `lineSpacing: number`
- `align: left|center|right|justify`
- `padding`: number or object `{top,right,bottom,left}`
- `border`: `{ width, color }`

## 8. Binding and expressions

The proposal uses a safe template-like syntax:

- `{{ parameters.companyName }}`
- `{{ row.region }}`
- `{{ row.revenue | currency }}`

Allowed v1 functions:

- `upper`
- `lower`
- `trim`
- `currency`
- `number(format)`
- `date(format)`

Allowed `visibleWhen` conditions:

- simple comparison: `row.revenue > 0`
- equality: `parameters.country == "UY"`
- boolean operators: `and`, `or`, `not`

Security restrictions:

- no dynamic code execution,
- no arbitrary reflection over expressions,
- no IO/network calls from expressions.

## 9. Validation contract

Minimum validations before rendering:

- `kind == FluentReport`
- supported `schemaVersion`
- `pages.length >= 1`
- every page has `content`
- known `type` on every node
- existing `styleRef`
- valid hex colors
- referenced `dataSources` exist
- `rows.cells` are consistent with columns and `colSpan`

Errors should include:

- code (`FRS001`, `FRS002`, ...)
- clear message
- node path (`pages[0].content.items[3]`)
- line/column when supported by the parser

## 10. Mapping to the current FluentReport runtime

Proposed mapping:

- Schema -> DTO (`ReportDefinition`)
- DTO -> `DocumentSettings`
- `Document.FromSettings(...)` -> existing renderers

Equivalence table:

- `page.size/margin` -> `PageSettings`
- `header/content/footer` -> `HeaderElement/ContentElement/FooterElement`
- `column/row/table/text/...` -> concrete `IElement` implementations
- `runs token currentPage/totalPages` -> dynamic spans
- `rows.source` -> row expansion in `TableElement`

## 11. Full example (v1)

```yaml
kind: FluentReport
schemaVersion: 1
name: revenue-by-region

pageDefaults:
  size: A4
  orientation: portrait
  margin: { top: 40, right: 40, bottom: 40, left: 40 }

parameters:
  companyName: { type: string, required: true }
  period: { type: string, required: true }

dataSources:
  sales:
    type: array
    requiredFields: [region, month, revenue]

styles:
  title: { fontSize: 20, bold: true, align: center }
  h2: { fontSize: 13, bold: true }
  th: { fontSize: 11, bold: true, color: "#FFFFFF", background: "#2C5CC5" }

pages:
  - id: p1
    header:
      type: text
      value: "{{ parameters.companyName }}"
      styleRef: h2

    content:
      type: column
      spacing: 10
      items:
        - type: text
          value: "Revenue Report - {{ parameters.period }}"
          styleRef: title

        - type: line
          thickness: 1
          color: "#D8D8D8"

        - type: table
          columns:
            - { mode: relative, value: 2 }
            - { mode: relative, value: 2 }
            - { mode: fixed, value: 100 }
          header:
            - { node: { type: text, value: "Region", styleRef: th } }
            - { node: { type: text, value: "Month", styleRef: th } }
            - { node: { type: text, value: "Revenue", styleRef: th, align: right } }
          rows:
            source: sales
            cells:
              - { node: { type: text, value: "{{ row.region }}" } }
              - { node: { type: text, value: "{{ row.month }}" } }
              - { node: { type: text, value: "{{ row.revenue | currency }}", align: right } }
          cellBorder: { width: 0.5, color: "#CFCFCF" }

    footer:
      type: text
      align: center
      runs:
        - { value: "Page " }
        - { token: currentPage }
        - { value: " of " }
        - { token: totalPages }
```

## 12. Versioning and compatibility

- `schemaVersion` is required.
- Compatible changes: adding optional fields.
- Breaking changes: changing semantics or required fields.
- Recommended strategy: `v1`, `v2`, with a parser per version.

## 13. Integration with MD/MDX (optional)

Proposal:

- Markdown/MDX as an authoring layer.
- YAML frontmatter + declarative blocks.
- Compilation to Schema v1 before rendering.

Conceptual example:

```md
---
kind: FluentReport
schemaVersion: 1
name: md-report
---

# {{ parameters.companyName }}

<FrTable source="sales" columns="region,month,revenue" />
```

Recommendation: keep **Schema v1** as the canonical source and treat MD/MDX as an input transpiler.

## 14. Suggested implementation plan

1. Define DTOs (`ReportDefinition`, `PageNode`, `ElementNode`, etc.).
2. Publish a v1 JSON Schema validator.
3. Implement a YAML/JSON parser + validator.
4. Implement a DTO -> `DocumentSettings` translator.
5. Add a `FluentReport.Schema` package and extensions:
   - `DocumentSchemaExtensions.FromSchema(path, data, parameters)`
6. Add tests:
   - PDF/HTML/Excel snapshots,
   - validation error coverage,
   - table and `pageBreak` scenarios.
