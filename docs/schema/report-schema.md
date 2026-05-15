# FluentReport YAML Schema Reference

## Goal

This document defines the canonical YAML schema accepted by `FluentReport.Schema` (`FromSchemaYaml` / `FromSchemaJson`).

Mechanical validation is defined in [report-schema.schema.json](./report-schema.schema.json).

## Schema contract

The schema is a canonical authoring document. The following fields are preserved:


- `metadata.title`
- `frame.x`, `frame.y`, `frame.width`, `frame.height`
- `zIndex`
- multiple pages in `pages[]`
- page regions (`header`, `content`, `footer`)
- group definitions built from `groupId`
- group instances placed on the page

Supported element types:

- `text`, `line`, `spacer`, `pageBreak`, `image`, `table`, and `repeat`
- `table` is also exported as a definition under `definitions.repeatables`
- `repeat/list` is also exported as a definition under `definitions.repeatables`

## Top-level structure

The YAML exported by the editor has this general shape:

```yaml
kind: FluentReport
schemaVersion: 1
name: revenue-by-region
metadata:
  title: "Revenue Report - {{ parameters.period }}"
pageDefaults:
  size: A4
  orientation: portrait
  margin:
    top: 40
    right: 40
    bottom: 40
    left: 40
parameters:
  companyName:
    type: string
    required: true
  period:
    type: string
    required: true
dataSources:
  sales:
    type: array
assets: {}
styles:
  title:
    fontSize: 20
    bold: true
    align: center
  h2:
    fontSize: 13
    bold: true
rendererOptions:
  html:
    maxWidth: 600
    fontFamily: "Arial, Helvetica, sans-serif"
    pageDividerStyle: "border-top: 2px dashed #cccccc; padding-top: 16px; padding-bottom: 16px"
    outlookCompatible: false
definitions:
  groups: []
  repeatables:
    - id: table-b-5
      type: table
      name: sales-table
      dataSource: sales
      columns: []
    - id: repeat-b-6
      type: repeat
      name: highlights
      dataSource: sales
      itemTemplate: "{{ row.title }}\n{{ row.description }}"
      itemGap: 10
pages:
  - id: p1
    size: A4
    orientation: portrait
    margin:
      top: 40
      right: 40
      bottom: 40
      left: 40
    regions:
      header:
        frame: { x: 56, y: 0, width: 682, height: 38 }
        nodes: []
      content:
        frame: { x: 56, y: 38, width: 682, height: 1033 }
        nodes: []
      footer:
        frame: { x: 56, y: 1071, width: 682, height: 52 }
        nodes: []
```

## Formal definition by section

### 1. Document header

- `kind`: always `FluentReport`
- `schemaVersion`: always `1`
- `name`: report identifier
- `metadata.title`: visible or functional document title

These fields identify the document and are part of the canonical authoring contract.

### 2. `pageDefaults`

Defines the document's base page configuration.

```yaml
pageDefaults:
  size: A4
  orientation: portrait
  margin:
    top: 40
    right: 40
    bottom: 40
    left: 40
```

Rules:

- `size` allows `A4`, `Letter`, `Legal`, `A3`, `A5`
- `orientation` allows `portrait` or `landscape`
- `margin.top|right|bottom|left` are numeric values in points

### 2.1 `pages`

`pages` contains the visual sequence of the document. The editor already supports multiple pages and exports each one with its own `id` and `regions`.

Rules:

- the order of `pages[]` defines the final render order
- each canvas block is exported into the page it belongs to
- each page currently repeats the base configuration from `pageDefaults`

### 3. `parameters`, `dataSources`, and `assets`

The current editor emits two named parameters, allows named data sources, and reserves space for assets.

```yaml
parameters:
  companyName:
    type: string
    required: true
  period:
    type: string
    required: true

dataSources:
  sales:
    type: array
assets: {}
```

Rules:

- each key under `parameters` is the logical parameter name
- each value must be `{ type: string, required: boolean }`
- each editor-exported data source is serialized as `{ type: array }`
- `assets` exists in the contract even though the editor does not yet edit it visually

### 4. `styles`

`styles` is a dictionary of named styles for text nodes.

Supported style properties in the current contract:

- `fontSize`
- `fontFamily`
- `bold`
- `italic`
- `underline`
- `color`
- `background`
- `lineSpacing`
- `align`

### 5. `rendererOptions`

The contract already supports renderer-specific options. At this stage the editor exposes HTML/email settings through the Document panel and exports them in `rendererOptions.html`.

```yaml
rendererOptions:
  html:
    maxWidth: 600
    fontFamily: "Arial, Helvetica, sans-serif"
    pageDividerStyle: "border-top: 2px dashed #cccccc; padding-top: 16px; padding-bottom: 16px"
    outlookCompatible: false
```

Rules:

- `maxWidth` is exported as a positive integer
- `fontFamily` and `pageDividerStyle` are exported as editable strings
- `outlookCompatible` is exported as a boolean

### 6. `definitions`

`definitions` groups reusable elements in the document.

#### 6.1 `definitions.groups`

When several blocks share the same `groupId`, the editor exports a group definition and then places a group instance on the page.

```yaml
definitions:
  groups:
    - id: g-2
      type: group
      name: "Group g-2"
      frame:
        x: 0
        y: 0
        width: 240
        height: 120
      nodes: []
```

Rules:

- the `frame` inside a group definition is relative to the group origin
- `nodes[]` contains child nodes with frames relative to the group
- the group is instantiated on a page through `groupInstance`

#### 6.2 `definitions.repeatables`

`definitions.repeatables` stores tables and other repeatable definitions. The current editor already exports entries of type `table` and `repeat` when those blocks exist on the canvas.

Following the pattern used by market report editors, these definitions also persist how they grow when they receive more data than initially expected.

```yaml
definitions:
  repeatables:
    - id: table-b-5
      type: table
      name: sales-table
      dataSource: sales
      growthMode: grow
      overflowMode: nextPage
      keepTogether: false
      columns:
        - field: region
          header: Region
          width: 2
        - field: month
          header: Month
          width: 2
        - field: revenue
          header: Revenue
          width: 1
          align: right
    - id: repeat-b-6
      type: repeat
      name: highlights
      dataSource: sales
      itemTemplate: "{{ row.title }}\n{{ row.description }}"
      itemGap: 10
      growthMode: grow
      overflowMode: nextPage
      keepTogether: false
```

Rules:

- `growthMode: grow` lets the region increase its actual height according to data
- `growthMode: fixed` forces the designed frame height to be respected
- `overflowMode: nextPage` continues the content on the next page
- `overflowMode: truncate` clips the content when it runs out of space
- `keepTogether: true` attempts to move the whole block to the next page before splitting it

### 7. `pages` and `regions`

Each page persists its visual regions.

```yaml
pages:
  - id: p1
    size: A4
    orientation: portrait
    margin: { top: 40, right: 40, bottom: 40, left: 40 }
    regions:
      header:
        frame: { x: 56, y: 0, width: 682, height: 38 }
        nodes: []
      content:
        frame: { x: 56, y: 38, width: 682, height: 1033 }
        nodes: []
      footer:
        frame: { x: 56, y: 1071, width: 682, height: 52 }
        nodes: []
```

Rules:

- `regions.header`, `regions.content`, and `regions.footer` are part of the contract
- each region persists its `frame`
- each region contains `nodes[]`

## Node types in `regions.*.nodes[]`

All visual nodes share these base properties:

- `id`
- `type`
- `frame`
- `zIndex`

### `text`

```yaml
- id: b-3
  type: text
  frame: { x: 154, y: 214, width: 486, height: 72 }
  zIndex: 2
  value: "Revenue Report - {{ parameters.period }}"
  styleRef: title
  align: center
```

A `text` node can use either `value` or `runs`.

### `line`

```yaml
- id: b-2
  type: line
  frame: { x: 72, y: 172, width: 650, height: 6 }
  zIndex: 1
  thickness: 1
  color: "#D8D8D8"
```

The importer infers direction from the frame: when `frame.height > frame.width` the line is rendered **vertically**; otherwise it is rendered horizontally.

### `spacer`

```yaml
- id: b-4
  type: spacer
  frame: { x: 72, y: 314, width: 220, height: 28 }
  zIndex: 3
  size: 12
```

### `pageBreak`

```yaml
- id: b-10
  type: pageBreak
  frame: { x: 72, y: 620, width: 180, height: 40 }
  zIndex: 8
```

### `image`

```yaml
- id: b-7
  type: image
  frame: { x: 72, y: 420, width: 240, height: 160 }
  zIndex: 6
  source:
    mode: path
    value: assets/logo.png
  fit: contain
  alt: Logo
```

Supported `source.mode` values:

| `mode` | Description |
|--------|-------------|
| `path` | Local file path (relative paths are resolved from the schema file's directory) |
| `base64` | Base64-encoded image bytes directly in the YAML/JSON value |
| `bytes` | Alias for `base64` |

### `table`

```yaml
- id: b-5
  type: table
  frame: { x: 72, y: 372, width: 520, height: 144 }
  zIndex: 4
  name: sales-table
  dataSource: sales
  definitionRef: table-b-5
  growthMode: grow
  overflowMode: nextPage
  keepTogether: false
  columns:
    - field: region
      header: Region
      width: 2
    - field: month
      header: Month
      width: 2
    - field: revenue
      header: Revenue
      width: 1
      align: right
```

A `table` can grow vertically and paginate like a classic report data region.

### `repeat`

```yaml
- id: b-6
  type: repeat
  frame: { x: 72, y: 540, width: 320, height: 160 }
  zIndex: 5
  name: highlights
  dataSource: sales
  definitionRef: repeat-b-6
  itemTemplate: "{{ row.title }}\n{{ row.description }}"
  itemGap: 10
  growthMode: grow
  overflowMode: nextPage
  keepTogether: false
```

A `repeat` follows the same growth rules as a market-style list or repeater: grow, continue on the next page, or truncate, with the option to try to keep the whole block together.

### `groupInstance`

```yaml
- id: instance-g-2
  type: groupInstance
  groupRef: g-2
  frame: { x: 72, y: 96, width: 240, height: 120 }
  zIndex: 0
```

## Practical consequences

The YAML exported by the editor now represents the visual document rather than a linearized version of the canvas.

That enables:

- round-trip without losing position or size
- persistence of real groups
- persistence of tables as repeatable definitions from the editor
- persistence of repeat/list blocks as reusable definitions from the editor
- a cleaner separation between document state and ephemeral editor state

## Runtime validation behavior

The editor schema contract is consumed at runtime by `FluentReport.Schema` in strict mode.

Current runtime behavior is fail-fast: invalid contracts produce explicit exceptions instead of silent fallback.

Validation currently enforced:

- `schemaVersion` must be supported by the runtime (currently `1`)
- `type` must be a supported node type
- `styleRef` must exist in `styles`
- `groupInstance.groupRef` must exist in `definitions.groups`
- `definitionRef` used by `table`/`repeat` must exist and match expected type
- referenced `dataSource` must be provided at import time
- colors must be valid (`InvalidOperationException` on invalid values)
- `image.source.value` is required
- `image.source.mode` must be `path`, `base64`, or `bytes`
- `base64`/`bytes` image content must be valid base64

This strict behavior improves consistency and debuggability of the end-to-end flow `schema -> Document -> renderer`.

---

## Binding expression reference

### Syntax

All binding expressions use double-brace delimiters and are resolved at import time (before rendering). The resolved string replaces the expression in place.

```
{{ expression }}
{{ expression | pipe }}
{{ expression | pipe(argument) }}
```

Multiple expressions can appear in the same string:

```yaml
value: "{{ parameters.companyName }} — Report for {{ parameters.period }}"
```

Whitespace inside the braces is ignored: `{{ row.name }}` and `{{row.name}}` are equivalent.

### Expression contexts

| Location in schema | Available root | Example |
|-------------------|---------------|---------|
| Any node `value` in `pages[*].regions.*.nodes[]` | `parameters` | `{{ parameters.period }}` |
| `metadata.title` | `parameters` | `{{ parameters.companyName }}` |
| `table` column cell values | `row` | `{{ row.region }}` |
| `repeat` `itemTemplate` | `row` | `{{ row.title }}` |

> `row` is only available inside a data-bound region (table column or repeat template). Using `row` outside those contexts produces an empty string.

### Pipe functions

Apply a transformation by appending `| pipeName` or `| pipeName(arg)` after the expression.

| Pipe | Input type | Example | Output |
|------|-----------|---------|--------|
| `upper` | string | `{{ row.region \| upper }}` | `"north"` → `"NORTH"` |
| `lower` | string | `{{ row.region \| lower }}` | `"NORTH"` → `"north"` |
| `trim` | string | `{{ row.label \| trim }}` | `"  hi  "` → `"hi"` |
| `currency` | numeric | `{{ row.amount \| currency }}` | `1200` → `"$1,200.00"` |
| `number(fmt)` | numeric | `{{ row.rate \| number(P1) }}` | `0.145` → `"14.5%"` |
| `date(fmt)` | DateTime or parseable string | `{{ row.date \| date(yyyy-MM) }}` | → `"2026-01"` |

- `fmt` for `number` follows standard .NET numeric format strings (`C`, `N2`, `P1`, `0.00`, etc.).
- `fmt` for `date` follows standard .NET date format strings (`yyyy-MM-dd`, `MMMM yyyy`, etc.).
- Pipes are not chainable (only one pipe per expression).

### Null and missing field behavior

If a referenced field does not exist on the row object or its value is `null`, the expression resolves to an empty string. No exception is thrown.

---

## Style inheritance and precedence

### Named styles (`styles` + `styleRef`)

Named styles are defined at document level under `styles` and referenced by nodes via `styleRef`:

```yaml
styles:
  heading:
    fontSize: 18
    bold: true
    color: "#1A237E"
    align: center

pages:
  - id: p1
    regions:
      content:
        nodes:
          - id: t1
            type: text
            value: "Section Title"
            styleRef: heading
```

### Inline style properties

Text nodes also accept style properties directly as node properties (`fontSize`, `bold`, `color`, `align`, etc.). These are **inline** styles.

### Precedence rules

When both `styleRef` and inline properties are present on the same node, **inline properties win over the referenced style on a per-property basis**:

```yaml
styles:
  base:
    fontSize: 12
    bold: false
    color: "#000000"
    align: left

pages:
  - id: p1
    regions:
      content:
        nodes:
          - id: t1
            type: text
            value: "Override example"
            styleRef: base      # pulls fontSize=12, bold=false, color=#000000, align=left
            bold: true          # overrides bold → true
            color: "#FF0000"    # overrides color → red
            # fontSize and align remain from the styleRef: 12pt, left
```

Properties not specified anywhere use renderer defaults (typically: `fontSize: 10`, `bold: false`, `italic: false`, `color: "#000000"`, `align: left`).

---

## Growth, overflow, and pagination

### Properties

`growthMode`, `overflowMode`, and `keepTogether` apply to `table` and `repeat` nodes (both inline and in `definitions.repeatables`).

| Property | Values | Description |
|----------|--------|-------------|
| `growthMode` | `grow` \| `fixed` | Whether the element expands beyond its designed `frame.height` |
| `overflowMode` | `nextPage` \| `truncate` | What happens when content exceeds available vertical space |
| `keepTogether` | `true` \| `false` | Attempt to avoid splitting the block across pages |

### Behavior matrix

| `growthMode` | `overflowMode` | `keepTogether` | Behavior |
|-------------|---------------|---------------|----------|
| `grow` | `nextPage` | `false` | Element expands; rows that don't fit continue on the next page. Default for data tables. |
| `grow` | `nextPage` | `true` | If the entire element does not fit on the current page, it is moved to the next page. If it still doesn't fit on a single page it will paginate normally. |
| `grow` | `truncate` | `false` | Element expands but rows beyond the page boundary are silently clipped. |
| `fixed` | `nextPage` | `false` | Frame height is respected; content that overflows continues on the next page. |
| `fixed` | `truncate` | `false` | Frame height is respected; content that overflows is silently clipped. |

> `keepTogether: true` combined with `overflowMode: truncate` behaves identically to `keepTogether: false` with `overflowMode: truncate` — there is no "move and then truncate" semantics.

### Defaults

If not specified, the runtime uses:
- `growthMode: grow`
- `overflowMode: nextPage`
- `keepTogether: false`

---

## Renderer feature compatibility

The following table shows which schema features produce output in each renderer. "✓" = full support. "~" = partial/degraded. "—" = not supported (silently ignored).

| Feature | PDF | HTML (full doc) | HTML (fragment) | Excel |
|---------|:---:|:---------------:|:---------------:|:-----:|
| `text` with styles | ✓ | ✓ | ✓ | ✓ |
| `line` | ✓ | ✓ | ✓ | ✓ |
| `spacer` | ✓ | ✓ | ✓ | ✓ |
| `pageBreak` | ✓ | ✓ | ✓ | ✓ → new sheet |
| `image` (path / base64) | ✓ | ✓ (data URI) | ✓ (data URI) | — |
| `table` with data rows | ✓ | ✓ | ✓ | ✓ |
| `table` header row | ✓ | ✓ | ✓ | ✓ (first row) |
| `repeat` / `itemTemplate` | ✓ | ✓ | ✓ | ✓ |
| `groupInstance` expansion | ✓ | ✓ | ✓ | ✓ |
| Multi-page (`pages[]`) | ✓ | ✓ (sections) | ✓ (sections) | ✓ (one sheet each) |
| `page.Header` / `.Footer` | ✓ (every page) | ✓ (once per page section) | ✓ | ~ (first/last row) |
| `page.Size` / margins | ✓ | ✓ | — | — |
| Custom `fontFamily` | ✓ | ✓ | ✓ | — |
| `rendererOptions.html.*` | — | ✓ | ✓ | — |
| `growthMode` / `overflowMode` | ✓ | ✓ | ✓ | ~ |
| `keepTogether` | ✓ | ~ | ~ | — |

### Notable degradations

- **Excel — images**: `image` nodes are silently skipped in Excel output.
- **Excel — page size/margins**: Physical page dimensions and margin settings are not applied. Excel renders to its own grid width.
- **Excel — `page.Header` / `.Footer`**: Renders as the first and last rows of the sheet respectively, not as repeated print header/footer rows.
- **Excel — `keepTogether`**: The Excel renderer does not support content grouping for pagination purposes.
- **HTML — `page.Size` in fragments**: Page size is ignored in `GenerateHtmlFragment()` output; use `rendererOptions.html.maxWidth` instead.

---

## Validation rule traceability

Each runtime validation rule is backed by a test in `tests/FluentReport.Schema.Tests/SchemaTests.cs`.

| Rule | Test method | Exception |
|------|-------------|-----------|
| `schemaVersion` must be `1` | `FromSchemaYaml_UnsupportedSchemaVersion_Throws` | `NotSupportedException` |
| `type` must be a supported node type | `FromSchemaYaml_UnknownNodeType_Throws` | `InvalidOperationException` |
| `styleRef` must exist in `styles` | `FromSchemaYaml_UnknownStyleRef_Throws` | `InvalidOperationException` |
| `groupRef` must exist in `definitions.groups` | `FromSchemaYaml_MissingGroupDefinition_Throws` | `InvalidOperationException` |
| `definitionRef` must exist in `definitions.repeatables` | `FromSchemaYaml_MissingRepeatDefinition_Throws` | `InvalidOperationException` |
| Referenced `dataSource` must be provided at import | `FromSchemaYaml_MissingDataSource_Throws` | `InvalidOperationException` |
| Colors must be valid hex | `FromSchemaYaml_InvalidColor_Throws` | `InvalidOperationException` |
| `image.source.mode` must be `path`/`base64`/`bytes` | `FromSchemaYaml_UnsupportedImageSourceMode_Throws` | `InvalidOperationException` |
| `base64`/`bytes` image content must be valid base64 | `FromSchemaYaml_InvalidImageBase64_Throws` | `InvalidOperationException` |
| At least one page required | `FromSchemaYaml_WithoutPages_Throws` | `ArgumentException` |

### Known coverage gaps

The following features are implemented but do not yet have dedicated schema-level tests:

| Gap | Notes |
|-----|-------|
| `image.source.mode: path` file resolution | Relative path from schema file directory not covered by a test |
| `growthMode` / `overflowMode` pagination behavior | Covered indirectly by PDF generation; no assertion on page count or row splitting |
| `keepTogether` semantics | No test verifies block movement to next page |
| `rendererOptions.html.*` round-trip | No test verifies HTML output respects `maxWidth` / `outlookCompatible` |
| `definitions.groups` with multiple nested nodes | Only single-node group tested |
| `repeat` with multi-line `itemTemplate` | Newline handling in template string not explicitly tested |
| Schema equivalence for `image` | `SchemaAndFluentApi_*_AreEquivalent` tests cover text and table; image not included |


