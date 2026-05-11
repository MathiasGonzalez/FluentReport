using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>
/// SkiaSharp implementation of <see cref="IDrawingCanvas"/>.
/// Wraps an <see cref="SKCanvas"/> and delegates text measurement to <see cref="SkiaTextMeasurer"/>.
/// </summary>
public sealed class SkiaDrawingCanvas(SKCanvas canvas) : IDrawingCanvas
{
    private readonly SkiaTextMeasurer _measurer = new();

    // ── State management ────────────────────────────────────────────────────

    public void Save() => canvas.Save();
    public void Restore() => canvas.Restore();

    public void ClipRect(float x, float y, float width, float height)
        => canvas.ClipRect(new SKRect(x, y, x + width, y + height));

    // ── Drawing primitives ───────────────────────────────────────────────────

    public void DrawLine(float x0, float y0, float x1, float y1, ReportColor color, float strokeWidth)
    {
        using var paint = new SKPaint
        {
            Color = ToSk(color),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawLine(x0, y0, x1, y1, paint);
    }

    public void DrawFilledRect(float x, float y, float width, float height, ReportColor color)
    {
        using var paint = new SKPaint { Color = ToSk(color), Style = SKPaintStyle.Fill };
        canvas.DrawRect(x, y, width, height, paint);
    }

    public void DrawStrokedRect(float x, float y, float width, float height, ReportColor color, float strokeWidth)
    {
        using var paint = new SKPaint
        {
            Color = ToSk(color),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(x, y, width, height, paint);
    }

    public void DrawText(string text, float x, float y, DrawTextAlign align, TextStyle style)
    {
        using var typeface = SkiaFonts.CreateTypeface(style);
        using var font = new SKFont(typeface, style.FontSize);
        using var paint = new SKPaint { Color = ToSk(style.EffectiveColor), IsAntialias = true };
        var skAlign = align switch
        {
            DrawTextAlign.Center => SKTextAlign.Center,
            DrawTextAlign.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left
        };

        if (style.Rotation != 0f)
        {
            // PDF rotation is CCW (positive = CCW). In Skia (Y-down) the same visual effect
            // requires the opposite sign: rotate CW by the same magnitude.
            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(-style.Rotation);
            canvas.DrawText(text, 0, 0, skAlign, font, paint);
            canvas.Restore();
        }
        else
        {
            canvas.DrawText(text, x, y, skAlign, font, paint);
        }
    }

    public void DrawImageBytes(byte[] bytes, float x, float y, float width, float height)
    {
        var destRect = new SKRect(x, y, x + width, y + height);
        using var paint = new SKPaint { IsAntialias = true };

        // Try bitmap decode first (faster), fall back to SKImage for other encoded formats.
        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap != null)
        {
            canvas.DrawBitmap(bitmap, destRect, paint);
            return;
        }
        using var image = SKImage.FromEncodedData(bytes);
        if (image != null)
            canvas.DrawImage(image, destRect, paint);
    }

    public void DrawCircle(float x, float y, float radius, ReportColor color)
    {
        using var paint = new SKPaint { Color = ToSk(color), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(x, y, radius, paint);
    }

    public void DrawPolyline(IReadOnlyList<(float X, float Y)> points, ReportColor color, float strokeWidth)
    {
        if (points.Count < 2) return;
        using var paint = new SKPaint { Color = ToSk(color), StrokeWidth = strokeWidth, Style = SKPaintStyle.Stroke, IsAntialias = true };
        using var path = new SKPath();
        path.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Count; i++)
            path.LineTo(points[i].X, points[i].Y);
        canvas.DrawPath(path, paint);
    }

    // ── ITextMeasurer (delegates to SkiaTextMeasurer) ────────────────────────

    public float MeasureText(string text, TextStyle style)
        => _measurer.MeasureText(text, style);

    public float MeasureText(string text, float fontSize, string? fontFamily = null)
        => _measurer.MeasureText(text, fontSize, fontFamily);

    public float GetTextAscent(TextStyle style)
        => _measurer.GetTextAscent(style);

    public List<string> WrapText(string text, TextStyle style, float maxWidth)
        => _measurer.WrapText(text, style, maxWidth);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SKColor ToSk(ReportColor c) => new(c.R, c.G, c.B, c.A);
}
