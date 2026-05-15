# FluentReport

![FluentReport Logo](logo.png)

[![CI](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/ci.yml)
[![Publish to NuGet](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml/badge.svg)](https://github.com/MathiasGonzalez/FluentReport/actions/workflows/nuget.yml)
[![NuGet FluentReport.Core](https://img.shields.io/nuget/v/FluentReport.Core.svg?label=FluentReport.Core)](https://www.nuget.org/packages/FluentReport.Core)
[![NuGet FluentReport](https://img.shields.io/nuget/v/FluentReport.svg?label=FluentReport)](https://www.nuget.org/packages/FluentReport)
[![NuGet FluentReport.Excel](https://img.shields.io/nuget/v/FluentReport.Excel.svg?label=FluentReport.Excel)](https://www.nuget.org/packages/FluentReport.Excel)
[![NuGet FluentReport.Html](https://img.shields.io/nuget/v/FluentReport.Html.svg?label=FluentReport.Html)](https://www.nuget.org/packages/FluentReport.Html)
[![NuGet FluentReport.Rdlc](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)
[![NuGet FluentReport.Schema](https://img.shields.io/nuget/v/FluentReport.Schema.svg?label=FluentReport.Schema)](https://www.nuget.org/packages/FluentReport.Schema)

.NET 10 library for generating PDF, Excel, and HTML reports using a fluent C# API.

## Packages

| Package | Description |
|---------|-------------|
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | PDF renderer (SkiaSharp — Linux compatible) |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer (ClosedXML) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML/email renderer (inline styles) |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC/SSRS importer |
| [`FluentReport.Schema`](https://www.nuget.org/packages/FluentReport.Schema) | YAML/JSON schema importer + validator |
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Core model without rendering dependencies — install directly only if implementing a custom renderer |
| `FluentReport.Mcp` _(tool)_ | MCP server — exposes all rendering and validation tools to AI coding agents |

## Supported targets

| Feature | PDF | HTML | Excel | RDLC import |
|---------|:---:|:----:|:-----:|:-----------:|
| Text & fonts | ✓ | ✓ | ✓ | ✓ |
| Tables | ✓ | ✓ | ✓ | ✓ |
| Column / Row layout | ✓ | ✓ | ✓ | — |
| Images | ✓ | ✓ | — | ✓ |
| Borders & backgrounds | ✓ | ✓ | ✓ | ✓ |
| Lines & spacers | ✓ | ✓ | ✓ | ✓ |
| Header / Footer | ✓ | ✓ | ✓ | ✓ |
| Multi-page | ✓ | ✓ (page-break) | ✓ (→ sheets) | ✓ |
| Page sizes & margins | ✓ | ✓ | — | ✓ |
| Custom fonts | ✓ | ✓ | — | — |
| Linux compatible | ✓ | ✓ | ✓ | ✓ |

## Installation

```shell
dotnet add package FluentReport           # PDF
dotnet add package FluentReport.Excel     # + Excel
dotnet add package FluentReport.Html      # + HTML/email
dotnet add package FluentReport.Rdlc      # + RDLC import
dotnet add package FluentReport.Schema    # + YAML/JSON schema import + validation
dotnet tool install -g FluentReport.Mcp   # MCP server for AI agents
```

## Quick start

```csharp
using FluentReport;
using FluentReport.Core;

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.MarginAll(40);

        page.Header()
            .Text("My Report")
            .FontSize(20).Bold().AlignCenter();

        page.Content().Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Summary").FontSize(14).Bold();
            col.Item().Line(1);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                });
                table.Header(h =>
                {
                    h.Cell().Background("#4472C4").Padding(5).Text("Product").Bold().Color("#FFFFFF");
                    h.Cell().Background("#4472C4").Padding(5).Text("Price").Bold().Color("#FFFFFF");
                });
                table.Cell().Padding(5).Text("Widget A");
                table.Cell().Padding(5).Text("$9.99");
            });
        });

        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    });
})
.GeneratePdf("report.pdf");
```

For Excel, HTML, RDLC, and schema usage see the individual package READMEs:
[FluentReport.Excel](src/FluentReport.Excel/README.md) · [FluentReport.Html](src/FluentReport.Html/README.md) · [FluentReport.Rdlc](src/FluentReport.Rdlc/README.md) · [FluentReport.Schema](src/FluentReport.Schema/README.md)

### Example output — e-Factura (Uruguay)

![e-Factura sample](examples/factura.png)


## Page sizes

| Constant | Size (pt) |
|----------|-----------|
| `PageSizes.A4` | 595 × 842 |
| `PageSizes.A3` | 842 × 1191 |
| `PageSizes.A5` | 420 × 595 |
| `PageSizes.Letter` | 612 × 792 |
| `PageSizes.Legal` | 612 × 1008 |

Use `.Landscape()` to swap width and height, or `page.Size(width, height)` for a custom size.

## AI-first design

FluentReport is designed to be used directly by AI coding agents without a visual editor. The recommended agent workflow:

1. **`validate_schema`** — iterate on a YAML schema and get structured errors (`code`/`message`/`path`) without exceptions
2. **`render_to_html`** — preview the output in the chat window
3. **`render_to_pdf`** — produce the final file

No visual editor needed. See [`docs/agent-quickstart.md`](docs/agent-quickstart.md) for a complete end-to-end guide, and [src/FluentReport.Mcp/README.md](src/FluentReport.Mcp/README.md) for MCP server setup.

## Documentation

- [AI coding agent quickstart](docs/agent-quickstart.md) — minimal end-to-end flow, binding syntax, renderer differences, common errors
- [MCP server setup](src/FluentReport.Mcp/README.md) — use FluentReport from any AI agent via Model Context Protocol
- [API reference](docs/api.md) — all builders and methods
- [YAML schema reference](docs/schema/report-schema.md) — schema contract, binding grammar, style precedence, renderer compatibility
- [JSON Schema validator](docs/schema/report-schema.schema.json) — machine-readable validation rules
- [Schema v1 proposal](docs/schema/report-schema-spec.md) — historical design proposal (not normative)
- [RDLC limitations](docs/rdlc-limitations.md) — known constraints and processing flow
- [UY fiscal document samples](docs/uy-fiscal-samples.md) — salary slip, delivery note, payment receipt

## Project structure

```
src/
├── FluentReport/          # PDF renderer (SkiaSharp)
├── FluentReport.Core/     # Core model and fluent API
├── FluentReport.Excel/    # Excel renderer (ClosedXML)
├── FluentReport.Html/     # HTML renderer
├── FluentReport.Rdlc/     # RDLC importer
├── FluentReport.Schema/   # YAML/JSON schema importer + validator
└── FluentReport.Mcp/      # MCP server (dotnet global tool for AI agents)
tests/
├── FluentReport.Tests/
├── FluentReport.Excel.Tests/
├── FluentReport.Html.Tests/
├── FluentReport.Rdlc.Tests/
└── FluentReport.Schema.Tests/
samples/
└── FluentReport.Samples/  # Sample documents (PDF, Excel, HTML)
docs/
├── agent-quickstart.md      # AI coding agent guide (start here)
├── api.md
├── schema/
│   ├── report-schema.md             # normative schema contract
│   ├── report-schema.schema.json
│   └── report-schema-spec.md    # historical proposal
├── rdlc-limitations.md
└── uy-fiscal-samples.md
```

## Dependencies

- [SkiaSharp](https://www.nuget.org/packages/SkiaSharp) 3.116.1 — PDF rendering
- [ClosedXML](https://www.nuget.org/packages/ClosedXML) 0.102.2 — Excel rendering
