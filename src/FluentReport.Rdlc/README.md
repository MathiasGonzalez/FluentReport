# FluentReport.Rdlc

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Rdlc.svg?label=FluentReport.Rdlc)](https://www.nuget.org/packages/FluentReport.Rdlc)

Importador **RDLC / SSRS** para FluentReport. Convierte archivos `.rdlc` (Visual Studio Report Designer / SQL Server Reporting Services) en documentos FluentReport, permitiendo renderizarlos como **PDF**, **Excel** o **HTML** sin dependencias de SSRS.

## Instalación

```shell
dotnet add package FluentReport.Rdlc
```

## Uso rápido

```csharp
using FluentReport.Rdlc;

// Desde un archivo en disco
var doc = DocumentRdlcExtensions.FromRdlc(
    "reportes/catalogo.rdlc",
    datasets: new Dictionary<string, IEnumerable<object>>
    {
        ["Productos"] = productos.Cast<object>()
    },
    parameters: new Dictionary<string, object>
    {
        ["Empresa"] = "Acme Corp."
    });

doc.GeneratePdf("catalogo.pdf");

// Desde un Stream (útil con recursos embebidos)
using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.Reports.catalogo.rdlc")!;
var doc2 = DocumentRdlcExtensions.FromRdlcStream(stream, datasets, parameters);
doc2.GenerateExcel("catalogo.xlsx");

// Desde una cadena XML
var doc3 = DocumentRdlcExtensions.FromRdlcXml(xmlString, datasets, parameters);
```

## API

| Método | Descripción |
|--------|-------------|
| `FromRdlc(path, datasets, parameters)` | Importa desde archivo |
| `FromRdlcStream(stream, datasets, parameters)` | Importa desde `Stream` |
| `FromRdlcXml(xml, datasets, parameters)` | Importa desde `string` XML |

## Elementos RDLC soportados

| Elemento RDLC | Equivalente FluentReport |
|---------------|--------------------------|
| `Textbox` | `TextElement` |
| `Line` | `LineElement` |
| `Image` | `ImageElement` |
| `Tablix` | `TableElement` (con datos y `ColSpan`) |
| `PageHeader` / `PageFooter` | Header / Footer de página |
| Márgenes y tamaño de página | `PageSettings` |

## Expresiones soportadas

| Expresión | Resultado |
|-----------|-----------|
| `=Fields!NombreCampo.Value` | Valor del campo en el dataset |
| `=Parameters!NombreParam.Value` | Valor del parámetro |
| Literal (sin `=`) | Texto estático |

> 📄 Full documentation and limitations: [`docs/api.md#rdlc-import`](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/api.md#rdlc-import) · [`docs/rdlc-limitations.md`](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/rdlc-limitations.md)

## Paquetes del ecosistema

| Paquete | Función |
|---------|---------|
| [`FluentReport.Core`](https://www.nuget.org/packages/FluentReport.Core) | Modelo y API fluent |
| [`FluentReport`](https://www.nuget.org/packages/FluentReport) | Renderer PDF |
| [`FluentReport.Excel`](https://www.nuget.org/packages/FluentReport.Excel) | Renderer Excel |
| [`FluentReport.Html`](https://www.nuget.org/packages/FluentReport.Html) | Renderer HTML / email |
| `FluentReport.Rdlc` | Importador RDLC — este paquete |

> 📖 Documentación completa en el [repositorio](https://github.com/MathiasGonzalez/FluentReport).
