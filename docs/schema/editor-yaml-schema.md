# Canonical YAML Schema for the FluentReport Editor

## Goal

This document defines the YAML currently exported by the editor located at `editor/fluentreport-editor` and explains how it maps to the visual model edited by the user on the canvas.

The normative source of the current contract is the editor code:

- `buildSchema(config)` in `editor/fluentreport-editor/src/reportSchema.ts`
- `toYaml(value)` in `editor/fluentreport-editor/src/reportSchema.ts`

Mechanical validation is defined in [docs/editor-yaml-schema.schema.json](./editor-yaml-schema.schema.json).

## Principles of the current contract

The editor exports a canonical authoring document. That means the YAML must no longer lose important visual decisions from the canvas.

The current contract persists:

- `metadata.title`
- `frame.x`, `frame.y`, `frame.width`, `frame.height`
- `zIndex`
- multiple pages in `pages[]`
- page regions (`header`, `content`, `footer`)
- group definitions built from `groupId`
- group instances placed on the page

Current functional limits:

- the editor exposes `text`, `line`, `spacer`, `pageBreak`, `image`, `table`, and `repeat`
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
