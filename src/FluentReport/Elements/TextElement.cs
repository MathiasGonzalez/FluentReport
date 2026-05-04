using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Elements;

public class TextSpan
{
    public string? StaticText { get; set; }
    public bool IsCurrentPage { get; set; }
    public bool IsTotalPages { get; set; }
    public TextStyle Style { get; set; } = new();
}

public class TextElement : ElementBase
{
    private readonly List<TextSpan> _spans = new();
    public TextStyle Style { get; } = new();
    public IReadOnlyList<TextSpan> Spans => _spans;

    public TextElement(string text)
    {
        _spans.Add(new TextSpan { StaticText = text, Style = Style });
    }

    public TextElement()
    {
    }

    /// <summary>
    /// Optional override for typeface creation. When non-null, called instead of the default
    /// system font lookup. Intended for test fixtures that need deterministic font rendering.
    /// </summary>
    internal static Func<TextStyle, SKTypeface>? TypefaceFactory { get; set; }

    public void AddSpan(string text, TextStyle? style = null)
    {
        _spans.Add(new TextSpan { StaticText = text, Style = style ?? Style });
    }

    public void AddCurrentPageSpan(TextStyle? style = null)
    {
        _spans.Add(new TextSpan { IsCurrentPage = true, Style = style ?? Style });
    }

    public void AddTotalPagesSpan(TextStyle? style = null)
    {
        _spans.Add(new TextSpan { IsTotalPages = true, Style = style ?? Style });
    }

    private static string ResolveSpanText(TextSpan span, RenderContext? ctx)
    {
        if (span.IsCurrentPage) return ctx?.CurrentPage.ToString() ?? "?";
        if (span.IsTotalPages) return ctx?.TotalPages.ToString() ?? "?";
        return span.StaticText ?? "";
    }

    private static SKTypeface CreateTypeface(TextStyle style)
    {
        if (TypefaceFactory != null)
            return TypefaceFactory(style);

        return SKTypeface.FromFamilyName(
            style.FontFamily,
            style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            style.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
        ) ?? SKTypeface.Default;
    }

    public override Size Measure(MeasureContext context)
    {
        if (_spans.Count == 0) return Size.Zero;

        if (_spans.Count == 1)
        {
            // Single span: full word-wrap support
            var span = _spans[0];
            var text = ResolveSpanText(span, null);
            using var typeface = CreateTypeface(span.Style);
            using var font = new SKFont(typeface, span.Style.FontSize);
            using var paint = new SKPaint { Color = span.Style.Color.ToSkColor(), IsAntialias = true };

            var lines = WrapText(text, font, context.AvailableWidth);
            var lineHeight = span.Style.FontSize * span.Style.LineSpacing;
            float width = 0;
            foreach (var line in lines)
            {
                var w = font.MeasureText(line, paint);
                if (w > width) width = w;
            }
            return new Size(Math.Min(width, context.AvailableWidth), lines.Count * lineHeight);
        }
        else
        {
            // Multiple spans: render inline; measure total width as sum of each span
            float totalWidth = 0;
            float maxLineHeight = 0;
            foreach (var span in _spans)
            {
                var text = ResolveSpanText(span, null);
                using var typeface = CreateTypeface(span.Style);
                using var font = new SKFont(typeface, span.Style.FontSize);
                using var paint = new SKPaint { Color = span.Style.Color.ToSkColor(), IsAntialias = true };

                totalWidth += font.MeasureText(text, paint);
                var lh = span.Style.FontSize * span.Style.LineSpacing;
                if (lh > maxLineHeight) maxLineHeight = lh;
            }
            return new Size(Math.Min(totalWidth, context.AvailableWidth), maxLineHeight);
        }
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (_spans.Count == 0) return;

        if (_spans.Count == 1)
        {
            // Single span: full word-wrap, alignment, and justify support
            var span = _spans[0];
            var text = ResolveSpanText(span, context);
            using var typeface = CreateTypeface(span.Style);
            using var font = new SKFont(typeface, span.Style.FontSize);
            using var paint = new SKPaint { Color = span.Style.Color.ToSkColor(), IsAntialias = true };

            RenderWrappedText(context.Canvas, text, span.Style, font, paint, position, size);
        }
        else
        {
            // Multiple spans: render each span inline with its own style
            float maxFontSize = _spans.Max(s => s.Style.FontSize);
            float x = position.X;
            float y = position.Y + maxFontSize; // baseline

            foreach (var span in _spans)
            {
                var text = ResolveSpanText(span, context);
                using var typeface = CreateTypeface(span.Style);
                using var font = new SKFont(typeface, span.Style.FontSize);
                using var paint = new SKPaint { Color = span.Style.Color.ToSkColor(), IsAntialias = true };

                context.Canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);

                if (span.Style.Underline)
                {
                    var textWidth = font.MeasureText(text, paint);
                    using var underlinePaint = new SKPaint { Color = span.Style.Color.ToSkColor(), StrokeWidth = 1, Style = SKPaintStyle.Stroke };
                    context.Canvas.DrawLine(x, y + 2, x + textWidth, y + 2, underlinePaint);
                }

                x += font.MeasureText(text, paint);
            }
        }
    }

    private static void RenderWrappedText(SKCanvas canvas, string text, TextStyle style, SKFont font, SKPaint paint, Position position, Size size)
    {
        var lines = WrapText(text, font, size.Width);
        var lineHeight = style.FontSize * style.LineSpacing;
        var y = position.Y + style.FontSize;

        foreach (var line in lines)
        {
            var lineWidth = font.MeasureText(line, paint);
            float x = position.X;

            switch (style.Alignment)
            {
                case TextAlignment.Center:
                    x = position.X + (size.Width - lineWidth) / 2f;
                    break;
                case TextAlignment.Right:
                    x = position.X + size.Width - lineWidth;
                    break;
                case TextAlignment.Justify when line != lines[^1]:
                    DrawJustified(canvas, line, font, paint, position.X, y, size.Width);
                    y += lineHeight;
                    continue;
            }

            canvas.DrawText(line, x, y, SKTextAlign.Left, font, paint);

            if (style.Underline)
            {
                using var underlinePaint = new SKPaint { Color = style.Color.ToSkColor(), StrokeWidth = 1, Style = SKPaintStyle.Stroke };
                canvas.DrawLine(x, y + 2, x + lineWidth, y + 2, underlinePaint);
            }

            y += lineHeight;
        }
    }

    private static List<string> WrapText(string text, SKFont font, float maxWidth)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }

        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (maxWidth <= 0 || font.MeasureText(line) <= maxWidth)
            {
                result.Add(line);
                continue;
            }

            var words = line.Split(' ');
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var test = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(test) <= maxWidth)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0) result.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }
            if (current.Length > 0) result.Add(current.ToString());
        }

        return result.Count > 0 ? result : new List<string> { "" };
    }

    private static void DrawJustified(SKCanvas canvas, string line, SKFont font, SKPaint paint, float x, float y, float width)
    {
        var words = line.Split(' ');
        if (words.Length <= 1) { canvas.DrawText(line, x, y, SKTextAlign.Left, font, paint); return; }

        var totalWordWidth = words.Sum(w => font.MeasureText(w));
        var spaceWidth = (width - totalWordWidth) / (words.Length - 1);

        var currentX = x;
        foreach (var word in words)
        {
            canvas.DrawText(word, currentX, y, SKTextAlign.Left, font, paint);
            currentX += font.MeasureText(word) + spaceWidth;
        }
    }
}
