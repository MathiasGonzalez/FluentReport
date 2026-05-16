# FluentReport.Schema

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Schema.svg?label=FluentReport.Schema)](https://www.nuget.org/packages/FluentReport.Schema)

YAML/JSON schema importer for FluentReport. Converts a report definition file (as produced by the web editor or written by hand) into a `Document` ready to render as **PDF**, **Excel**, or **HTML** using the existing rendering pipeline.

## Installation

```shell
dotnet add package FluentReport.Schema
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Schema;

var dataSources = new Dictionary<string, IEnumerable<object>>
{
    ["sales"] = new[]
    {
        (object)new { region = "North", month = "Jan", revenue = 1200m },
        (object)new { region = "South", month = "Jan", revenue = 980m }
    }
};

var parameters = new Dictionary<string, object>
{
    ["companyName"] = "Acme Corp",
    ["period"] = "2026-Q1"
};

var doc = DocumentSchemaExtensions.FromSchema("reports/revenue.frpt.yaml", dataSources, parameters);

doc.GeneratePdf("revenue.pdf");
doc.GenerateExcel("revenue.xlsx");
doc.GenerateHtml("revenue.html");
```

## API

| Method | Description |
|--------|-------------|
| `FromSchema(path, dataSources, parameters)` | Imports from a `.yaml`, `.yml`, or `.json` file on disk |
| `FromSchemaStream(stream, format, dataSources, parameters)` | Imports from a `Stream`; `format` must be `"yaml"` or `"json"` |
| `FromSchemaYaml(yaml, dataSources, parameters)` | Imports from a YAML `string` |
| `FromSchemaJson(json, dataSources, parameters)` | Imports from a JSON `string` |

All overloads return a `Document`. Both `dataSources` and `parameters` are optional (pass `null` or omit if the schema has no bindings).

## Node type → Fluent API mapping

| YAML `type` | FluentReport element | Notes |
|-------------|---------------------|-------|
| `text` | `TextElement` / `.Text(...)` | Supports `value`, `styleRef`, `align`, `color`, inline style |
| `line` | `LineElement` / `.Line(...)` | Direction inferred from frame aspect ratio |
| `spacer` | `SpacerElement` / `.Spacer(...)` | |
| `pageBreak` | `PageBreakElement` / `.PageBreak()` | |
| `image` | `ImageElement` / `.Image(...)` | `source.mode`: `path`, `base64`, or `bytes` |
| `table` | `TableElement` / `.Table(...)` | Requires `dataSource` at import time |
| `repeat` | `ListElement` / `.List(...)` | Requires `dataSource` at import time |
| `groupInstance` | Group expansion | Resolved from `definitions.groups` at import time |

> The import layer converts the schema into a `DocumentSettings` object and calls `Document.FromSettings(...)`, reusing all existing renderers without additional dependencies.

## Data binding

### Parameters

Reference document-level parameters anywhere in text values or `itemTemplate` strings:

```
{{ parameters.companyName }}
{{ parameters.period }}
```

### Row fields

Inside `table.columns[*]` and `repeat.itemTemplate`, reference the current row's fields:

```
{{ row.region }}
{{ row.revenue }}
```

### Pipe functions

Apply transformations by appending `| functionName` to any binding expression:

| Pipe | Example | Result |
|------|---------|--------|
| `upper` | `{{ row.name \| upper }}` | `"north"` → `"NORTH"` |
| `lower` | `{{ row.name \| lower }}` | `"NORTH"` → `"north"` |
| `trim` | `{{ row.label \| trim }}` | `"  hi  "` → `"hi"` |
| `currency` | `{{ row.revenue \| currency }}` | `1200` → `"$1,200.00"` |
| `number(fmt)` | `{{ row.rate \| number(P1) }}` | `0.145` → `"14.5%"` |
| `date(fmt)` | `{{ row.date \| date(yyyy-MM) }}` | `DateTime` → `"2026-01"` |

## Data source row shape

Row objects passed in `dataSources` can be:

- **Anonymous types**: `new { region = "North", revenue = 1200m }`
- **`Dictionary<string, object>`**: `new Dictionary<string, object> { ["region"] = "North" }`
- **POCOs**: any class with public properties

Field name resolution is case-insensitive. Missing fields resolve to an empty string (no exception).

## Validation and error behavior

The importer is fail-fast. Common exceptions:

| Condition | Exception | Message contains |
|-----------|-----------|-----------------|
| `schemaVersion` ≠ 1 | `NotSupportedException` | version number |
| Unknown `type` value | `InvalidOperationException` | node type string |
| `styleRef` not in `styles` | `InvalidOperationException` | style id |
| `groupRef` not in `definitions.groups` | `InvalidOperationException` | group id |
| `definitionRef` not in `definitions.repeatables` | `InvalidOperationException` | definition id |
| `dataSource` not provided at import | `InvalidOperationException` | datasource name |
| `image.source.mode` not `path`/`base64`/`bytes` | `InvalidOperationException` | `"Unsupported image source mode"` |
| Invalid base64 image content | `InvalidOperationException` | `"base64"` |
| Invalid color hex value | `InvalidOperationException` | `"Invalid text color"` |
| No pages defined | `ArgumentException` | — |

## Schema reference

For the full schema specification and all node properties see:

- [report-schema.md](../../docs/schema/report-schema.md) — normative contract
- [report-schema.schema.json](../../docs/schema/report-schema.schema.json) — JSON Schema validator
- [agent-quickstart.md](../../docs/agent-quickstart.md) — minimal end-to-end example for AI coding agents
