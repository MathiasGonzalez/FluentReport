using FluentReport.Styling;

namespace FluentReport.Elements;

/// <summary>A run of text with one style: fixed text or a page number.</summary>
public class TextSpan
{
    private TextStyle? _style;
    private Func<TextStyle>? _derivedStyle;

    /// <summary>The text; <c>null</c> for a page-number span.</summary>
    public string? StaticText { get; set; }
    /// <summary>The span shows the number of the page being drawn.</summary>
    public bool IsCurrentPage { get; set; }
    /// <summary>The span shows the total number of pages.</summary>
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

/// <summary>A paragraph made of one or more spans. Wraps to the available width and splits between lines across pages.</summary>
public class TextElement : IElement
{
    private readonly List<TextSpan> _spans = new();

    /// <summary>Element-level style. Spans created without an explicit style share this instance.</summary>
    public TextStyle Style { get; } = new();

    /// <summary>The spans, in reading order.</summary>
    public IReadOnlyList<TextSpan> Spans => _spans;

    /// <summary>Creates a text with one span that uses the element's style.</summary>
    public TextElement(string text)
    {
        _spans.Add(new TextSpan { StaticText = text, Style = Style });
    }

    /// <summary>Creates an empty text; add spans to it.</summary>
    public TextElement() { }

    /// <summary>Adds text. A <c>null</c> style shares the element's style.</summary>
    public void AddSpan(string text, TextStyle? style = null)
        => _spans.Add(new TextSpan { StaticText = text, Style = style ?? Style });

    /// <summary>Adds the current page number. A <c>null</c> style shares the element's style.</summary>
    public void AddCurrentPageSpan(TextStyle? style = null)
        => _spans.Add(new TextSpan { IsCurrentPage = true, Style = style ?? Style });

    /// <summary>Adds the total page count. A <c>null</c> style shares the element's style.</summary>
    public void AddTotalPagesSpan(TextStyle? style = null)
        => _spans.Add(new TextSpan { IsTotalPages = true, Style = style ?? Style });

    /// <summary>Adds a span whose style is this element's style with <paramref name="configure"/> applied on top.</summary>
    public void AddSpan(string text, Action<TextStyle> configure)
    {
        var span = DerivedSpan(configure);
        span.StaticText = text;
        _spans.Add(span);
    }

    /// <summary>Adds the current page number, styled as the element with <paramref name="configure"/> applied on top.</summary>
    public void AddCurrentPageSpan(Action<TextStyle> configure)
    {
        var span = DerivedSpan(configure);
        span.IsCurrentPage = true;
        _spans.Add(span);
    }

    /// <summary>Adds the total page count, styled as the element with <paramref name="configure"/> applied on top.</summary>
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
