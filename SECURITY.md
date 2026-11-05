# Security

## Reporting a vulnerability

Report vulnerabilities privately through [GitHub security advisories](https://github.com/MathiasGonzalez/FluentReport/security/advisories/new). Do not open a public issue. Include the package, version and a minimal input that reproduces the problem.

Fixes are released for the latest major version.

## Processing untrusted input

FluentReport can render report definitions (`.rdlc`) and images that come from users. The packages are built so that hostile input fails with a catchable exception or a warning, instead of reading files, running code or taking the process down:

| Input | Protection |
|-------|------------|
| RDLC XML | DTDs are rejected, so external entities (XXE) and entity expansion never run. Documents nested more than 256 elements deep are rejected. |
| RDLC `<Code>` blocks and custom assemblies | Never executed. Expressions can call only the built-in functions and read public properties of the data rows. |
| RDLC expressions | More than 100 nested parentheses/calls or more than 500 chained operators produce a warning (an exception with `StrictExpressions`). |
| RDLC `Like` filters | Evaluated in linear time. |
| External images in RDLC | Only local files inside `ExternalImageDirectory` (by default the `.rdlc` folder, and none for streams and strings). URLs, paths outside the directory and symbolic links are ignored with a warning. Set `LoadExternalImages = false` to disable them. |
| Images in PDF | Images above `PdfOptions.MaxImagePixels` (100 megapixels by default) throw before they are decoded. |
| Very deep element trees | Building and rendering throw `InsufficientExecutionStackException` instead of overflowing the stack. |
| Text in HTML output | HTML-encoded; font names are filtered and colours are emitted as hex. |
| Text in Excel output | Written as text values, never as formulas. |

Rendering still takes time and memory proportional to the data. When the input is untrusted, run it with a timeout and limit the size of uploaded files and datasets.
