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

    public TextElement(string text)
    {
        _spans.Add(new TextSpan { StaticText = text, Style = Style });
    }

    public TextElement()
    {
    }

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

    private string GetFullText(RenderContext? ctx = null)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var span in _spans)
        {
            if (span.IsCurrentPage) sb.Append(ctx?.CurrentPage.ToString() ?? "?");
            else if (span.IsTotalPages) sb.Append(ctx?.TotalPages.ToString() ?? "?");
            else sb.Append(span.StaticText ?? "");
        }
        return sb.ToString();
    }

    private static SKPaint CreatePaint(TextStyle style)
    {
        var typeface = SKTypeface.FromFamilyName(
            style.FontFamily,
            style.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            style.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright
        ) ?? SKTypeface.Default;

        return new SKPaint
        {
            Typeface = typeface,
            TextSize = style.FontSize,
            Color = style.Color.ToSkColor(),
            IsAntialias = true,
        };
    }

    public override Size Measure(MeasureContext context)
    {
        var text = GetFullText();
        var style = _spans.Count > 0 ? _spans[0].Style : Style;
        using var paint = CreatePaint(style);

        var lines = WrapText(text, paint, context.AvailableWidth);
        var lineHeight = style.FontSize * style.LineSpacing;
        float width = 0;
        foreach (var line in lines)
        {
            var w = paint.MeasureText(line);
            if (w > width) width = w;
        }
        width = Math.Min(width, context.AvailableWidth);
        return new Size(width, lines.Count * lineHeight);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        var text = GetFullText(context);
        var style = _spans.Count > 0 ? _spans[0].Style : Style;
        using var paint = CreatePaint(style);

        var lines = WrapText(text, paint, size.Width);
        var lineHeight = style.FontSize * style.LineSpacing;
        var y = position.Y + style.FontSize;

        foreach (var line in lines)
        {
            var lineWidth = paint.MeasureText(line);
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
                    DrawJustified(context.Canvas, line, paint, position.X, y, size.Width);
                    y += lineHeight;
                    continue;
            }

            context.Canvas.DrawText(line, x, y, paint);

            if (style.Underline)
            {
                using var underlinePaint = new SKPaint { Color = style.Color.ToSkColor(), StrokeWidth = 1 };
                context.Canvas.DrawLine(x, y + 2, x + lineWidth, y + 2, underlinePaint);
            }

            y += lineHeight;
        }
    }

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) { result.Add(""); return result; }

        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (maxWidth <= 0 || paint.MeasureText(line) <= maxWidth)
            {
                result.Add(line);
                continue;
            }

            var words = line.Split(' ');
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var test = current.Length == 0 ? word : current + " " + word;
                if (paint.MeasureText(test) <= maxWidth)
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

    private static void DrawJustified(SKCanvas canvas, string line, SKPaint paint, float x, float y, float width)
    {
        var words = line.Split(' ');
        if (words.Length <= 1) { canvas.DrawText(line, x, y, paint); return; }

        var totalWordWidth = words.Sum(w => paint.MeasureText(w));
        var spaceWidth = (width - totalWordWidth) / (words.Length - 1);

        var currentX = x;
        foreach (var word in words)
        {
            canvas.DrawText(word, currentX, y, paint);
            currentX += paint.MeasureText(word) + spaceWidth;
        }
    }
}
