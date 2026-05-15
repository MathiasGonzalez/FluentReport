# FluentReport.Mcp

MCP (Model Context Protocol) server for FluentReport. Exposes report generation, schema validation, and authoring tools directly to AI coding agents.

## Install as a .NET global tool

```bash
dotnet tool install -g FluentReport.Mcp
```

## Run

```bash
fluentreport-mcp
```

The server communicates over **stdio** (standard MCP transport). Configure it in your agent host (e.g., VS Code Copilot, Claude Desktop, Cursor):

```json
{
  "mcpServers": {
    "fluentreport": {
      "command": "fluentreport-mcp"
    }
  }
}
```

## Available tools

| Tool | Description |
|------|-------------|
| `validate_schema` | Validates YAML/JSON schema. Returns `{ isValid, errors[], warnings[] }` — no exceptions. |
| `render_to_pdf` | Renders schema → base64 PDF |
| `render_to_html` | Renders schema → HTML string (full page or fragment) |
| `render_to_excel` | Renders schema → base64 XLSX |
| `list_node_types` | Returns documentation for all supported node types |
| `get_schema_template` | Returns a ready-to-use YAML template (`minimal`, `invoice`, `table_report`, and Uruguayan document types — see below) |
| `schema_to_csharp` | Converts a YAML/JSON schema to equivalent C# `Document.Create()` fluent-API code |

## Typical agent workflow

```
1. get_schema_template("invoice")       → start with a working template
2. Modify the schema                    → customise for the task
3. validate_schema(schema)              → check for errors without rendering
4. render_to_html(schema, dataSources)  → preview in the chat window
5. render_to_pdf(schema, dataSources)   → final output
6. schema_to_csharp(schema)             → optional: get equivalent C# code
```

## Schema → C# workflow (for image/description-based generation)

When an AI agent sees a document image or receives a natural-language description,
it can use the MCP tools to produce ready-to-compile C# code:

```
1. get_schema_template("invoice")       → start from the closest template
   — or —
   list_node_types()                    → discover available elements
2. Build / adjust the schema            → match the image or description
3. validate_schema(schema)              → confirm there are no errors
4. schema_to_csharp(schema)             → convert to C# Document.Create() code
```

## Uruguayan document templates

`get_schema_template` supports these Uruguayan fiscal and legal document types:

| useCase | Document |
|---------|----------|
| `factura_uy` | e-Factura / CFE (DGI) |
| `recibo_sueldo_uy` | Recibo de Sueldo (MTSS) |
| `remito_uy` | Remito de Entrega (DGI Res. 2.530/991) |
| `recibo_pago_uy` | Recibo de Pago (Cód. Comercio) |

YAML examples with inline usage instructions are also available in [`examples/uy-docs/`](../../examples/uy-docs/).

## Data sources format

Pass as a JSON string mapping source name → array of row objects:

```json
{
  "lines": [
    { "description": "Widget A", "qty": 2, "price": 49.99 },
    { "description": "Widget B", "qty": 1, "price": 19.99 }
  ]
}
```

## Parameters format

Pass as a JSON string mapping name → value:

```json
{
  "companyName": "Acme Corp",
  "invoiceNo": "INV-2024-001",
  "generatedAt": "2024-01-15"
}
```

## Schema reference

See [`docs/agent-quickstart.md`](../../docs/agent-quickstart.md) and [`docs/schema/report-schema.md`](../../docs/schema/report-schema.md) for the full schema reference.
