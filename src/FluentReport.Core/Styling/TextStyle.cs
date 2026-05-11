namespace FluentReport.Styling;

public class TextStyle
{
    public float FontSize { get; set; } = 12;
    public string FontFamily { get; set; } = "sans-serif";
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    public bool Underline { get; set; } = false;
    public ReportColor Color { get; set; } = ReportColor.Black;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public float LineSpacing { get; set; } = 1.2f;

    /// <summary>Rotation angle in degrees, counter-clockwise (PDF convention). Default 0 = horizontal.</summary>
    public float Rotation { get; set; } = 0f;

    /// <summary>
    /// Optional delegate that overrides <see cref="Bold"/> at render time.
    /// The delegate receives no arguments; close over any data context you need.
    /// When <c>null</c>, <see cref="Bold"/> is used.
    /// </summary>
    public Func<bool>? BoldResolver { get; set; }

    /// <summary>
    /// Optional delegate that overrides <see cref="Italic"/> at render time.
    /// When <c>null</c>, <see cref="Italic"/> is used.
    /// </summary>
    public Func<bool>? ItalicResolver { get; set; }

    /// <summary>
    /// Optional delegate that overrides <see cref="Color"/> at render time.
    /// When <c>null</c>, <see cref="Color"/> is used.
    /// </summary>
    public Func<ReportColor>? ColorResolver { get; set; }

    /// <summary>Effective bold flag, respecting <see cref="BoldResolver"/> when set.</summary>
    public bool EffectiveBold => BoldResolver?.Invoke() ?? Bold;

    /// <summary>Effective italic flag, respecting <see cref="ItalicResolver"/> when set.</summary>
    public bool EffectiveItalic => ItalicResolver?.Invoke() ?? Italic;

    /// <summary>Effective color, respecting <see cref="ColorResolver"/> when set.</summary>
    public ReportColor EffectiveColor => ColorResolver?.Invoke() ?? Color;

    public TextStyle Clone() => new()
    {
        FontSize = FontSize,
        FontFamily = FontFamily,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Color = Color,
        Alignment = Alignment,
        LineSpacing = LineSpacing,
        Rotation = Rotation,
        BoldResolver = BoldResolver,
        ItalicResolver = ItalicResolver,
        ColorResolver = ColorResolver
    };
}

public enum TextAlignment { Left, Center, Right, Justify }
