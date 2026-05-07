using FluentReport.Core;
using FluentReport.Styling;

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

    public override Size Measure(MeasureContext context)
    {
        if (_spans.Count == 0) return Size.Zero;

        var measurer = context.Measurer;

        if (_spans.Count == 1)
        {
            var span = _spans[0];
            var text = ResolveSpanText(span, null);
            var lines = measurer.WrapText(text, span.Style, context.AvailableWidth);
            var lineHeight = span.Style.FontSize * span.Style.LineSpacing;
            float width = 0;
            foreach (var line in lines)
            {
                var w = measurer.MeasureText(line, span.Style);
                if (w > width) width = w;
            }
            return new Size(Math.Min(width, context.AvailableWidth), lines.Count * lineHeight);
        }
        else
        {
            float totalWidth = 0;
            float maxLineHeight = 0;
            foreach (var span in _spans)
            {
                var text = ResolveSpanText(span, null);
                totalWidth += measurer.MeasureText(text, span.Style);
                var lh = span.Style.FontSize * span.Style.LineSpacing;
                if (lh > maxLineHeight) maxLineHeight = lh;
            }
            return new Size(Math.Min(totalWidth, context.AvailableWidth), maxLineHeight);
        }
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (_spans.Count == 0) return;

        var canvas = context.Canvas;

        if (_spans.Count == 1)
        {
            var span = _spans[0];
            var text = ResolveSpanText(span, context);
            RenderWrappedText(canvas, text, span.Style, position, size);
        }
        else
        {
            float maxFontSize = _spans.Max(s => s.Style.FontSize);
            float x = position.X;
            float y = position.Y + maxFontSize;

            foreach (var span in _spans)
            {
                var text = ResolveSpanText(span, context);
                canvas.DrawText(text, x, y, DrawTextAlign.Left, span.Style);

                if (span.Style.Underline)
                {
                    var textWidth = canvas.MeasureText(text, span.Style);
                    canvas.DrawLine(x, y + 2, x + textWidth, y + 2, span.Style.EffectiveColor, 1);
                }

                x += canvas.MeasureText(text, span.Style);
            }
        }
    }

    private static void RenderWrappedText(IDrawingCanvas canvas, string text, TextStyle style, Position position, Size size)
    {
        var lines = canvas.WrapText(text, style, size.Width);
        var lineHeight = style.FontSize * style.LineSpacing;
        var y = position.Y + style.FontSize;

        foreach (var line in lines)
        {
            var lineWidth = canvas.MeasureText(line, style);
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
                    DrawJustified(canvas, line, style, position.X, y, size.Width);
                    y += lineHeight;
                    continue;
            }

            canvas.DrawText(line, x, y, DrawTextAlign.Left, style);

            if (style.Underline)
                canvas.DrawLine(x, y + 2, x + lineWidth, y + 2, style.EffectiveColor, 1);

            y += lineHeight;
        }
    }

    private static void DrawJustified(IDrawingCanvas canvas, string line, TextStyle style, float x, float y, float width)
    {
        var words = line.Split(' ');
        if (words.Length <= 1) { canvas.DrawText(line, x, y, DrawTextAlign.Left, style); return; }

        var totalWordWidth = words.Sum(w => canvas.MeasureText(w, style));
        var spaceWidth = (width - totalWordWidth) / (words.Length - 1);

        var currentX = x;
        foreach (var word in words)
        {
            canvas.DrawText(word, currentX, y, DrawTextAlign.Left, style);
            currentX += canvas.MeasureText(word, style) + spaceWidth;
        }
    }
}
