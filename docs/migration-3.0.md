# Migrating to FluentReport 3.0

3.0 moves the `FluentReport` (PDF) package from SkiaSharp 3.119 to **SkiaSharp 4.154**. The fluent API, the document model and the HTML, Excel, RDLC and Core packages do not change. They do not depend on SkiaSharp.

## Who is affected

Only code that uses SkiaSharp types directly with FluentReport:

- `PdfOptions.FontResolver` / `SkiaFonts.TypefaceFactory` (returns `SKTypeface`)
- `.Canvas(width, height, (SKCanvas canvas, SKRect rect) => …)`

If your project references SkiaSharp itself, update it (and its native assets package) to 4.x as well; mixing SkiaSharp 3 and 4 in one app is not supported.

## SkiaSharp 4 changes that show up in Canvas code

| SkiaSharp 3 | SkiaSharp 4 |
|-------------|-------------|
| Text members on `SKPaint` (`TextSize`, `Typeface`, `MeasureText`, …) | Removed: use `SKFont` (`new SKFont(typeface, size)`) and `canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint)` |
| `new SKFont()` used the default system typeface | Uses an empty typeface: always pass a typeface |
| `SKTypeface.FromFamilyName` returned `null` for unknown families | Falls back to the default typeface |
| RAW / DNG image decoding | Removed |

## Fonts

On Linux, SkiaSharp 4 with `SkiaSharp.NativeAssets.Linux.NoDependencies` does not find system fonts. When neither `FontResolver` nor the system provides a usable font, FluentReport 3.0 now uses its bundled **DejaVu Sans** (regular and bold; italic is synthesized) instead of rendering empty text. Reports that ask for fonts like Calibri therefore print in DejaVu Sans on such servers. Pass your own fonts with `FontResolver` for the same output everywhere.
