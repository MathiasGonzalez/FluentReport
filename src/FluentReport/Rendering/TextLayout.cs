using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Rendering;

/// <summary>A piece of one span on a line. <see cref="Text"/> is <c>null</c> for page-number spans.</summary>
internal sealed record TextRun(TextSpan Span, TextStyle Style, string? Text, bool SpaceBefore);

internal sealed class TextLine
{
    public List<TextRun> Runs { get; } = new();

    /// <summary>Last line of a paragraph (explicit line break or end of text): never justified.</summary>
    public bool EndsParagraph { get; set; }

    public float Height { get; set; }
    public float Ascent { get; set; }
    public float Descent { get; set; }

    /// <summary>Width measured during layout (page numbers use a placeholder width).</summary>
    public float Width { get; set; }
}

/// <summary>Text laid out into lines for one width. Also used as the fragment of a text split across pages.</summary>
internal sealed class TextBlock(TextElement source, IReadOnlyList<TextLine> lines) : IElement
{
    public TextElement Source { get; } = source;
    public IReadOnlyList<TextLine> Lines { get; } = lines;
    public float Height => Lines.Sum(l => l.Height);
    public float Width => Lines.Count == 0 ? 0 : Lines.Max(l => l.Width);

    public TextAlignment Alignment =>
        Source.Spans.Count > 0 ? Source.Spans[0].Style.Alignment : Source.Style.Alignment;

    public TextBlock Slice(int start, int count) => new(Source, Lines.Skip(start).Take(count).ToList());
}

/// <summary>Breaks the spans of a <see cref="TextElement"/> into lines that fit a width.</summary>
internal static class TextLayout
{
    /// <summary>Width reserved for a page number while breaking lines; the real number is aligned when drawn.</summary>
    public const string PageNumberPlaceholder = "888";

    private sealed record Token(TextSpan Span, TextStyle Style, string? Text, bool SpaceBefore, bool HardBreak = false);

    public static TextBlock Layout(TextElement element, float maxWidth, FontCache fonts)
    {
        var lines = new List<TextLine>();
        var line = new TextLine();
        float x = 0;
        var fallbackStyle = element.Spans.Count > 0 ? element.Spans[0].Style : element.Style;

        void Finish(bool endsParagraph)
        {
            line.EndsParagraph = endsParagraph;
            line.Width = x;
            Measure(line, fallbackStyle, fonts);
            lines.Add(line);
            line = new TextLine();
            x = 0;
        }

        foreach (var token in Tokenize(element))
        {
            if (token.HardBreak)
            {
                Finish(endsParagraph: true);
                continue;
            }

            float width = fonts.Measure(token.Text ?? PageNumberPlaceholder, token.Style);
            float space = token.SpaceBefore && line.Runs.Count > 0 ? fonts.Measure(" ", token.Style) : 0;

            if (line.Runs.Count > 0 && x + space + width > maxWidth + 0.01f)
            {
                Finish(endsParagraph: false);
                space = 0;
            }

            if (line.Runs.Count == 0 && width > maxWidth && token.Text is { Length: > 1 } longWord && maxWidth > 0)
            {
                // A word wider than the line: break it at character level instead of overflowing.
                var rest = longWord;
                while (rest.Length > 0)
                {
                    int fit = FittingPrefix(rest, token.Style, maxWidth, fonts);
                    var chunk = rest[..fit];
                    rest = rest[fit..];
                    line.Runs.Add(new TextRun(token.Span, token.Style, chunk, false));
                    x = fonts.Measure(chunk, token.Style);
                    if (rest.Length > 0) Finish(endsParagraph: false);
                }
                continue;
            }

            line.Runs.Add(new TextRun(token.Span, token.Style, token.Text, space > 0));
            x += space + width;
        }

        Finish(endsParagraph: true);
        return new TextBlock(element, lines);
    }

    private static int FittingPrefix(string text, TextStyle style, float maxWidth, FontCache fonts)
    {
        int lo = 1, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (fonts.Measure(text[..mid], style) <= maxWidth) lo = mid;
            else hi = mid - 1;
        }
        return lo;
    }

    private static void Measure(TextLine line, TextStyle fallback, FontCache fonts)
    {
        IEnumerable<TextStyle> styles = line.Runs.Count > 0 ? line.Runs.Select(r => r.Style) : [fallback];
        foreach (var style in styles)
        {
            var metrics = fonts.Font(style).Metrics;
            line.Height = Math.Max(line.Height, style.FontSize * style.LineSpacing);
            line.Ascent = Math.Max(line.Ascent, -metrics.Ascent);
            line.Descent = Math.Max(line.Descent, metrics.Descent);
        }
    }

    private static IEnumerable<Token> Tokenize(TextElement element)
    {
        bool pendingSpace = false;
        foreach (var span in element.Spans)
        {
            var style = span.Style;
            if (span.IsCurrentPage || span.IsTotalPages)
            {
                yield return new Token(span, style, null, pendingSpace);
                pendingSpace = false;
                continue;
            }

            var text = (span.StaticText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ');
            var paragraphs = text.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0)
                {
                    yield return new Token(span, style, null, false, HardBreak: true);
                    pendingSpace = false;
                }

                var words = paragraphs[p].Split(' ');
                for (int w = 0; w < words.Length; w++)
                {
                    if (w > 0) pendingSpace = true;
                    if (words[w].Length == 0) continue;
                    yield return new Token(span, style, words[w], pendingSpace);
                    pendingSpace = false;
                }
            }
        }
    }
}
