# Plan de Implementacion: Editor FluentReport con Blazor WebAssembly + React

## 1. Objetivo

Construir un editor visual para reportes FluentReport usando:

- Blazor WebAssembly como shell de aplicacion y coordinador de estado.
- React para la experiencia WYSIWYG (drag and drop, propiedades, tabs).
- API ASP.NET Core para preview real y exportacion (PDF, HTML, Excel).

El objetivo es combinar UX rica en frontend con rendering fiel y consistente en backend .NET.

## 2. Alcance inicial (MVP)

Incluye:

- Editor visual de bloques basicos (text, line, spacer, pageBreak).
- Tabs: Preview, YAML, Data JSON.
- Edicion de data JSON de ejemplo.
- Preview servidor por API (imagen o HTML).
- Exportacion a PDF y HTML.
- Guardado/carga de schema v1 desde archivo.

No incluye en MVP:

- Tabla avanzada con row groups.
- Colaboracion multiusuario.
- Versionado historico de documentos.
- Editor de temas avanzado.

## 3. Arquitectura propuesta

## 3.1 Componentes

1. Cliente Blazor WebAssembly
- Routing.
- Shell de UI.
- Autenticacion futura.
- Coordinacion de estado global de documento.

2. Modulo React embebido
- Canvas WYSIWYG.
- Drag and drop de componentes.
- Panel de propiedades.
- Emision de cambios de schema/data al host Blazor.

3. API ASP.NET Core
- Validacion de schema.
- Transformacion schema -> DocumentSettings.
- Render preview (PNG/HTML).
- Export PDF/HTML/Excel.

4. Shared Contracts (.NET class library)
- DTOs de schema.
- DTOs de preview/export.
- Validaciones base.

## 3.2 Flujo de datos

1. React edita esquema y data.
2. React notifica cambios via bridge JS <-> Blazor.
3. Blazor decide cuando sincronizar con API (debounce).
4. API devuelve preview y errores de validacion por nodo.
5. Blazor actualiza UI con resultados.

## 4. Estructura de proyectos sugerida

```text
editor/
  FluentReport.Editor.sln
  src/
    FluentReport.Editor.Client/          # Blazor WASM
    FluentReport.Editor.ReactHost/       # assets React build o source
    FluentReport.Editor.Api/             # ASP.NET Core backend
    FluentReport.Editor.Contracts/       # DTOs compartidos
    FluentReport.Editor.SchemaAdapter/   # schema -> DocumentSettings
```

## 5. Fases de implementacion

## Fase 1: Base de solucion y contratos

Entregables:

- Solucion con 4 proyectos (Client, Api, Contracts, SchemaAdapter).
- DTOs minimos de schema v1.
- Endpoint de healthcheck y version.

Criterio de salida:

- Build y run de Client + Api en local.

## Fase 2: Integracion React en Blazor WASM

Entregables:

- Componente Razor host para React.
- Bridge JS interop:
  - initEditor(initialSchema, initialData)
  - onSchemaChanged
  - onDataChanged

Criterio de salida:

- Cambios en React visibles en estado Blazor.

## Fase 3: Preview real por API

Entregables:

- Endpoint POST /preview/html
- Endpoint POST /preview/image
- Endpoint POST /validate
- Pipeline schema -> DocumentSettings -> render

Criterio de salida:

- Preview actualizado desde editor con tiempos aceptables.

## Fase 4: Exportaciones

Entregables:

- Endpoint POST /export/pdf
- Endpoint POST /export/html
- Endpoint POST /export/excel

Criterio de salida:

- Descarga de archivos desde la UI.

## Fase 5: UX y robustez

Entregables:

- Manejo de errores por path de nodo.
- Guardar/cargar documento.
- Indicadores de estado (saving, validating, rendering).

Criterio de salida:

- Flujo de edicion continuo sin bloqueos.

## 6. Contrato de integracion React <-> Blazor

Eventos React -> Blazor:

- schemaChanged(payload)
- dataChanged(payload)
- selectionChanged(payload)
- requestPreview(payload)

Comandos Blazor -> React:

- setSchema(payload)
- setData(payload)
- setValidationErrors(payload)
- setPreview(payload)

Recomendacion:

- Versionar el bridge con campo apiVersion.

## 7. Endpoints API (borrador)

1. POST /api/editor/validate
Request:
- schema
- sampleData

Response:
- isValid
- errors[]: code, message, path, line, column

2. POST /api/editor/preview
Request:
- schema
- sampleData
- format: html|png

Response:
- html o imageBytes/base64

3. POST /api/editor/export
Request:
- schema
- sampleData
- format: pdf|html|excel

Response:
- file stream

## 8. Riesgos y mitigaciones

1. Latencia de preview
- Mitigacion: debounce 300-500ms + cancelacion de requests previos.

2. Divergencia entre preview y export final
- Mitigacion: usar el mismo pipeline de render para ambos.

3. Complejidad del bridge React/Blazor
- Mitigacion: contrato estable y eventos acotados.

4. Escalabilidad de JSON/YAML grandes
- Mitigacion: snapshots parciales y virtualizacion en UI.

## 9. Criterios de aceptacion del MVP

1. Se puede crear un reporte basico desde UI.
2. Se puede editar data JSON de ejemplo.
3. Preview se actualiza con cambios.
4. Se puede exportar a PDF y HTML.
5. Errores de validacion muestran path del nodo.

## 10. Roadmap posterior al MVP

1. Soporte de bloque table completo (rows.source, cells).
2. Header/footer avanzados.
3. Historial undo/redo persistente.
4. Templates predefinidos.
5. Autoguardado y versionado.

## 11. Recomendacion de arranque

Primer sprint sugerido (1-2 semanas):

1. Crear solucion base + contratos.
2. Integrar React dentro de Blazor WASM.
3. Exponer endpoint de validate y preview HTML.
4. Conectar tabs Preview/YAML/Data con API.
