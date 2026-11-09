namespace FluentReport.Styling;

/// <summary>Font, color, alignment and spacing of a text.</summary>
public class TextStyle
{
    /// <summary>Font size in points. Default 12.</summary>
    public float FontSize { get; set; } = 12;
    /// <summary>Font family name. Default <c>sans-serif</c>.</summary>
    public string FontFamily { get; set; } = "sans-serif";
    /// <summary>Bold text.</summary>
    public bool Bold { get; set; } = false;
    /// <summary>Italic text.</summary>
    public bool Italic { get; set; } = false;
    /// <summary>Underlined text.</summary>
    public bool Underline { get; set; } = false;
    /// <summary>Text color. Default black.</summary>
    public ReportColor Color { get; set; } = ReportColor.Black;
    /// <summary>Alignment of the lines inside the text's width. Default left.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    /// <summary>Line height as a multiple of the font size. Default 1.2.</summary>
    public float LineSpacing { get; set; } = 1.2f;

    /// <summary>Rotation in degrees, counter-clockwise. Drawn by the PDF renderer only; layout uses the unrotated box.</summary>
    public float Rotation { get; set; }

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

    /// <summary>A copy with the same values and resolvers.</summary>
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

/// <summary>Alignment of the lines of a text inside its width.</summary>
public enum TextAlignment
{
    /// <summary>Lines start at the left edge.</summary>
    Left,
    /// <summary>Lines are centered.</summary>
    Center,
    /// <summary>Lines end at the right edge.</summary>
    Right,
    /// <summary>Lines fill the width, except the last of each paragraph.</summary>
    Justify
}
