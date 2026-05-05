using FluentReport.Styling;
using SkiaSharp;
using System.Globalization;

namespace FluentReport.Samples;

/// <summary>
/// Utility methods shared across all sample generators.
/// </summary>
internal static class SampleHelpers
{
    /// <summary>
    /// Formats a decimal value using InvariantCulture for deterministic output across locales.
    /// </summary>
    public static string Fmt(decimal value, string format = "N2") =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the shared TextStyle action for the standard
    /// "Generado con FluentReport – Página N de M" footer
    /// used across all Uruguayan fiscal document samples.
    /// </summary>
    public static Action<TextStyle> UyFooterStyle() =>
        s => { s.FontFamily = FacturaUY.FontPrimary; s.FontSize = FacturaUY.FontSizeLegal; };

    /// <summary>
    /// Creates a checkerboard PNG that mimics a QR code placeholder.
    /// Replace this with an actual QR-code generator (e.g. ZXing.Net) in production.
    /// </summary>
    public static byte[] GenerarQrPlaceholder(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");

        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(new SKColor(240, 240, 240));

        int cellSize = Math.Max(1, size / 10);
        using var darkPaint = new SKPaint { Color = new SKColor(30, 30, 30), IsAntialias = false };
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
                if ((r + c) % 2 == 0)
                    canvas.DrawRect(c * cellSize, r * cellSize, cellSize - 1, cellSize - 1, darkPaint);

        // Simulate QR finder-pattern corners
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = false,
        };
        int p = 3 * cellSize;
        canvas.DrawRect(1,        1,        p - 2, p - 2, borderPaint);
        canvas.DrawRect(size - p, 1,        p - 2, p - 2, borderPaint);
        canvas.DrawRect(1,        size - p, p - 2, p - 2, borderPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
