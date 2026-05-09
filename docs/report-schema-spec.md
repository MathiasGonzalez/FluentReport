# FluentReport Report Schema v1 (Propuesta)

## 1. Objetivo

Definir un formato declarativo para modelar reportes sin escribir C# fluido, de forma que pueda:

- Validarse con un esquema (JSON Schema).
- Traducirse a `DocumentSettings`.
- Renderizarse por los backends existentes (PDF, HTML, Excel).
- Mantener compatibilidad y versionado explicito.

Este documento propone un **Schema v1** como base para un importador nuevo (`FluentReport.Schema`).

## 2. Alcance v1

Incluye:

- Documento, paginas, margenes y tamano.
- Header, content y footer.
- Contenedores `column`, `row`, `table`.
- Elementos `text`, `line`, `spacer`, `image`, `pageBreak`.
- Estilos basicos (tipografia, color, background, border, padding, alignment).
- Binding de datos por expresiones restringidas.

No incluye en v1:

- Motor de expresiones arbitrario (sin ejecutar C# ni JS).
- Layout avanzado de Tablix (tipo RDLC complejo).
- Reglas condicionales complejas por renderer.
- Componentes custom de terceros en runtime.

## 3. Formato de archivo

Se recomienda soportar ambos serializadores:

- YAML (mejor authoring humano): `.frpt.yaml`
- JSON (integracion/tooling): `.frpt.json`

Ambos mapean a la misma estructura DTO interna.

## 4. Estructura top-level

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
  orientation: portrait # portrait | landscape
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

## 5. Modelo de pagina

Cada pagina define zonas:

- `header`
- `content` (obligatoria)
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

## 6. Tipos de nodos

Todos los nodos comparten campos comunes:

- `type` (obligatorio)
- `id` (opcional)
- `style` (inline, opcional)
- `styleRef` (opcional)
- `padding`, `background`, `border`, `align` (opcionales)
- `visibleWhen` (opcional, expresion booleana simple)

### 6.1 text

```yaml
type: text
value: "Resumen {{ parameters.period }}"
# o formato por spans:
runs:
  - value: "Page "
  - token: currentPage
  - value: " of "
  - token: totalPages
```

Reglas:

- Debe existir `value` o `runs`.
- `runs[].token` solo permite: `currentPage`, `totalPages`.

### 6.2 column

```yaml
type: column
spacing: 8
items:
  - type: text
    value: "Item 1"
  - type: line
```

### 6.3 row

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

### 6.4 table

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

Reglas:

- `columns` es obligatoria.
- Si hay `rows.source`, debe existir en `dataSources`.
- Cada fila resultante debe completar la grilla respetando `colSpan`.

### 6.5 image

```yaml
type: image
source:
  mode: path # path | base64
  value: "assets/logo.png"
fit: contain # contain | cover | none
```

### 6.6 line

```yaml
type: line
thickness: 1
color: "#D0D0D0"
```

### 6.7 spacer

```yaml
type: spacer
size: 12
```

### 6.8 pageBreak

```yaml
type: pageBreak
```

## 7. Estilos

### 7.1 styleRef + style inline

- `styleRef`: aplica estilo nombrado desde `styles`.
- `style`: override inline sobre el estilo referenciado.

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

### 7.2 Propiedades de estilo v1

- `fontSize: number`
- `fontFamily: string`
- `bold: boolean`
- `italic: boolean`
- `underline: boolean`
- `color: #RRGGBB`
- `background: #RRGGBB`
- `lineSpacing: number`
- `align: left|center|right|justify`
- `padding`: number o objeto `{top,right,bottom,left}`
- `border`: `{ width, color }`

## 8. Binding y expresiones

Se propone una sintaxis segura tipo template:

- `{{ parameters.companyName }}`
- `{{ row.region }}`
- `{{ row.revenue | currency }}`

Funciones permitidas v1:

- `upper`
- `lower`
- `trim`
- `currency`
- `number(format)`
- `date(format)`

Condiciones (`visibleWhen`) permitidas:

- comparacion simple: `row.revenue > 0`
- igualdad: `parameters.country == "UY"`
- booleanos: `and`, `or`, `not`

Restricciones de seguridad:

- Sin ejecucion de codigo dinamico.
- Sin reflection arbitraria sobre expresiones.
- Sin llamadas IO/red desde expresiones.

## 9. Contrato de validacion

Validaciones minimas antes de renderizar:

- `kind == FluentReport`
- `schemaVersion` soportado
- `pages.length >= 1`
- Cada pagina tiene `content`
- `type` conocido en cada nodo
- `styleRef` existente
- Colores hex validos
- `dataSources` referenciados existen
- `rows.cells` consistente con columnas y `colSpan`

Errores deben incluir:

- Codigo (`FRS001`, `FRS002`, ...)
- Mensaje claro
- Ruta del nodo (`pages[0].content.items[3]`)
- Linea/columna (si el parser lo soporta)

## 10. Mapeo a FluentReport actual

Mapeo propuesto:

- Schema -> DTO (`ReportDefinition`)
- DTO -> `DocumentSettings`
- `Document.FromSettings(...)` -> renderers existentes

Tabla de equivalencias:

- `page.size/margin` -> `PageSettings`
- `header/content/footer` -> `HeaderElement/ContentElement/FooterElement`
- `column/row/table/text/...` -> `IElement` concretos
- `runs token currentPage/totalPages` -> spans dinamicos
- `rows.source` -> expansion de filas en `TableElement`

## 11. Ejemplo completo (v1)

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

## 12. Versionado y compatibilidad

- `schemaVersion` obligatorio.
- Cambios compatibles: agregar campos opcionales.
- Cambios no compatibles: cambiar semantica o campos requeridos.
- Estrategia recomendada: `v1`, `v2`, con parser por version.

## 13. Integracion con MD/MDX (opcional)

Propuesta:

- Markdown/MDX como capa de authoring.
- Frontmatter YAML + bloques declarativos.
- Compilacion a Schema v1 antes de render.

Ejemplo conceptual:

```md
---
kind: FluentReport
schemaVersion: 1
name: md-report
---

# {{ parameters.companyName }}

<FrTable source="sales" columns="region,month,revenue" />
```

Recomendacion: mantener **Schema v1** como fuente canonica y tratar MD/MDX como transpiler de entrada.

## 14. Plan de implementacion sugerido

1. Definir DTOs (`ReportDefinition`, `PageNode`, `ElementNode`, etc.).
2. Publicar JSON Schema de validacion v1.
3. Implementar parser YAML/JSON + validador.
4. Implementar traductor DTO -> `DocumentSettings`.
5. Agregar paquete `FluentReport.Schema` y extensiones:
   - `DocumentSchemaExtensions.FromSchema(path, data, parameters)`
6. Tests:
   - snapshots PDF/HTML/Excel
   - validacion de errores
   - casos con tablas y pageBreak.
