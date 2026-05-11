# FluentReport.Schema

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Schema.svg?label=FluentReport.Schema)](https://www.nuget.org/packages/FluentReport.Schema)

Importador de esquemas **YAML/JSON** para FluentReport. Convierte un archivo del esquema del editor en un `Document` listo para renderizar en **PDF**, **Excel** o **HTML** usando el pipeline existente.

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
| `FromSchema(path, dataSources, parameters)` | Importa desde archivo `.yaml`, `.yml` o `.json` |
| `FromSchemaStream(stream, format, dataSources, parameters)` | Importa desde `Stream` (`format`: `yaml` o `json`) |
| `FromSchemaYaml(yaml, dataSources, parameters)` | Importa desde YAML `string` |
| `FromSchemaJson(json, dataSources, parameters)` | Importa desde JSON `string` |

## Mapping YAML => Fluent API

| YAML node | FluentReport equivalente |
|----------|---------------------------|
| `regions.*.nodes[].type: text` | `TextElement` / `.Text(...)` |
| `line` | `LineElement` / `.Line(...)` |
| `spacer` | `SpacerElement` / `.Spacer(...)` |
| `pageBreak` | `PageBreakElement` / `.PageBreak()` |
| `image` | `ImageElement` / `.Image(...)` |
| `table` | `TableElement` / `.Table(...)` |
| `repeat` | `ListElement` / `.List(...)` |
| `groupInstance` | Expansión de `definitions.groups` |
| `styleRef` + `styles.*` | `TextStyle` |
| `{{ parameters.x }}` / `{{ row.y }}` | Resolución de bindings antes del render |

> La traducción final usa `Document.FromSettings(...)` para reutilizar los renderers existentes.
