# FluentReport.Cli

[![NuGet](https://img.shields.io/nuget/v/FluentReport.Cli.svg?label=FluentReport.Cli)](https://www.nuget.org/packages/FluentReport.Cli)

Command-line tool for [FluentReport](https://github.com/MathiasGonzalez/FluentReport). It renders
**RDLC / SSRS** reports with **JSON** data to **PDF**, **PNG**, **HTML** or **Excel**, without
ReportViewer and without writing a project.

## Run it

With the .NET 10 SDK, `dnx` runs the tool without installing it:

```bash
dnx FluentReport.Cli render invoice.rdlc --data Lines=lines.json -o invoice.pdf
```

`dnx` reads `--help`, `--version` and `-v` as its own options, wherever they are. Use the `help`
and `version` commands instead (`dnx FluentReport.Cli help`), or put `--` after the package name:
`dnx FluentReport.Cli -- render --help`.

Or install it once and call it `fluentreport`:

```bash
dotnet tool install -g FluentReport.Cli
fluentreport render invoice.rdlc --data Lines=lines.json -o invoice.pdf
```

## Data

Each dataset of the report takes an array of JSON objects; each object is a row and its
properties are the fields (`=Fields!Amount.Value`). Field names follow the report's
`<DataField>` mapping and are matched without regard to case.

```bash
# One file per dataset
fluentreport render invoice.rdlc -d Lines=lines.json -d Taxes=taxes.json

# One file with every dataset: {"Lines": [...], "Taxes": [...]}
fluentreport render invoice.rdlc -d data.json

# A report with a single dataset takes a plain array
fluentreport render list.rdlc -d rows.json

# From standard input
curl -s https://example.com/api/lines | fluentreport render invoice.rdlc -d Lines=- -o invoice.pdf
```

Numbers are read as decimals. Dates stay text and are read with the report's culture, so pass
`--culture` when they are not in ISO format.

## Options

| Option | |
|---|---|
| `-o`, `--output <file>` | Output file; its extension picks the format (`.pdf`, `.png`, `.html`, `.xlsx`). Default: the report's name with `.pdf`. |
| `-f`, `--format <format>` | `pdf`, `png`, `html` or `xlsx`, when the output has another extension. |
| `-d`, `--data [Name=]<file>` | Rows as JSON; `-` reads standard input. Repeat it for several datasets. |
| `-p`, `--param Name=value` | Report parameter (`=Parameters!Name.Value`). |
| `--culture <name>` | Culture for numbers and dates (`es-UY`, `en-US`, ...). Default: the report's `<Language>`, else invariant. |
| `--scale <n>` | PNG pixels per point; default 2 (144 dpi). Several pages write `name.png`, `name-2.png`, ... |
| `--strict` | Fail on an expression that cannot be evaluated, instead of leaving it empty with a warning. |
| `--no-external-images` | Do not load images from files next to the report. |

Warnings from the import (an expression that could not be evaluated, an unknown dataset) go to
standard error; the files written go to standard output. Exit codes: `0` done, `1` the report or its
data could not be used, `2` wrong arguments.

What the importer supports is listed in
[RDLC import](https://github.com/MathiasGonzalez/FluentReport/blob/main/docs/rdlc-limitations.md).
