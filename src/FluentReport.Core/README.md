# FluentReport.Core

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Core.svg?label=FluentReport.Core)](https://www.nuget.org/packages/FluentReport.Core)

Document model and fluent API for **FluentReport**. You normally install a renderer (`FluentReport.Html`, `FluentReport.Excel`) or the importer (`FluentReport.Rdlc`), which bring this package along. Install it directly to build documents in a project that does not render them, or to write your own renderer.

## Installation

```shell
dotnet add package FluentReport.Core
```

## What is included

- `Document`, `DocumentSettings`, `PageSettings`, `PageSizes`
- Builders: `DocumentBuilder`, `PageBuilder`, `ContainerBuilder`, `ColumnBuilder`, `RowBuilder`, `TableBuilder`, `TextBuilder`, `DynamicTextBuilder`
- Elements (data only): `TextElement`, `TableElement`, `ImageElement`, `ColumnElement`, `RowElement`, `BorderElement`, `PaddingElement`, `AlignElement`, `LineElement`, `SpacerElement`, `PageBreakElement`
- Styling: `TextStyle`, `ReportColor`, `BorderStyle`

## Writing a renderer

Elements carry data only; a renderer walks the tree and decides the layout:

```csharp
string Render(IElement element) => element switch
{
    TextElement text     => string.Concat(text.Spans.Select(s => s.StaticText)),
    ColumnElement column => string.Join("\n", column.Items.Select(Render)),
    PaddingElement p     => p.Child is null ? "" : Render(p.Child),
    // …
    _ => throw new NotSupportedException(element.GetType().Name)
};

var doc = Document.Create(c => c.Page(p => p.Content().Text("Hello")));
foreach (var page in doc.Settings.Pages)
    Console.WriteLine(Render(page.ContentElement!));
```

`TableElement.PartitionIntoRows` turns the flat list of cells into rows, respecting column spans.

## Ecosystem packages

| Package | Purpose |
|---------|---------|
| `FluentReport.Core` | Document model and fluent API (this package) |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | HTML / email renderer |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Excel (.xlsx) renderer |
| [`FluentReport.Rdlc`](https://www.nuget.org/packages/FluentReport.Rdlc) | RDLC / SSRS importer |

> Full documentation is available in the [repository](https://github.com/MathiasGonzalez/FluentReport).
