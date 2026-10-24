using FluentReport.Styling;

namespace FluentReport.Elements;

public class TextSpan
{
    private TextStyle? _style;
    private Func<TextStyle>? _derivedStyle;

    public string? StaticText { get; set; }
    public bool IsCurrentPage { get; set; }
    public bool IsTotalPages { get; set; }

    /// <summary>
    /// The span's effective style. Spans added through <see cref="TextElement.AddSpan(string, Action{TextStyle})"/>
    /// derive it on every read from the element's current style plus their own overrides, so styling
    /// applied to the element afterwards still reaches them. Assigning a style fixes it.
    /// </summary>
    public TextStyle Style
    {
        get => _style ?? _derivedStyle?.Invoke() ?? (_style = new TextStyle());
        set { _style = value; _derivedStyle = null; }
    }

    internal static TextSpan Derived(Func<TextStyle> style) => new() { _derivedStyle = style };
}

public class TextElement : IElement
{
    private readonly List<TextSpan> _spans = new();

    /// <summary>Element-level style. Spans created without an explicit style share this instance.</summary>
    public TextStyle Style { get; } = new();

    public IReadOnlyList<TextSpan> Spans => _spans;

    public TextElement(string text)
    {
        _spans.Add(new TextSpan { StaticText = text, Style = Style });
    }

    public TextElement() { }

    public void AddSpan(string text, TextStyle? style = null)
        => _spans.Add(new TextSpan { StaticText = text, Style = style ?? Style });

    public void AddCurrentPageSpan(TextStyle? style = null)
        => _spans.Add(new TextSpan { IsCurrentPage = true, Style = style ?? Style });

    public void AddTotalPagesSpan(TextStyle? style = null)
        => _spans.Add(new TextSpan { IsTotalPages = true, Style = style ?? Style });

    /// <summary>Adds a span whose style is this element's style with <paramref name="configure"/> applied on top.</summary>
    public void AddSpan(string text, Action<TextStyle> configure)
    {
        var span = DerivedSpan(configure);
        span.StaticText = text;
        _spans.Add(span);
    }

    public void AddCurrentPageSpan(Action<TextStyle> configure)
    {
        var span = DerivedSpan(configure);
        span.IsCurrentPage = true;
        _spans.Add(span);
    }

    public void AddTotalPagesSpan(Action<TextStyle> configure)
    {
        var span = DerivedSpan(configure);
        span.IsTotalPages = true;
        _spans.Add(span);
    }

    private TextSpan DerivedSpan(Action<TextStyle> configure) => TextSpan.Derived(() =>
    {
        var style = Style.Clone();
        configure(style);
        return style;
    });
}
