namespace FluentReport.Styling;

/// <summary>Font, color, alignment and spacing of a text.</summary>
public class TextStyle
{
    private float _fontSize = 12;
    private string _fontFamily = "sans-serif";
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private ReportColor _color = ReportColor.Black;
    private TextAlignment _alignment = TextAlignment.Left;
    private float _lineSpacing = 1.2f;
    private float _rotation;
    private Func<bool>? _boldResolver;
    private Func<bool>? _italicResolver;
    private Func<ReportColor>? _colorResolver;

    /// <summary>Counts the property assignments, so a span can tell when a style it derives from, or the one it handed out, has changed.</summary>
    internal int Version { get; private set; }

    /// <summary>Font size in points. Default 12.</summary>
    public float FontSize { get => _fontSize; set { _fontSize = value; Version++; } }
    /// <summary>Font family name. Default <c>sans-serif</c>.</summary>
    public string FontFamily { get => _fontFamily; set { _fontFamily = value; Version++; } }
    /// <summary>Bold text.</summary>
    public bool Bold { get => _bold; set { _bold = value; Version++; } }
    /// <summary>Italic text.</summary>
    public bool Italic { get => _italic; set { _italic = value; Version++; } }
    /// <summary>Underlined text.</summary>
    public bool Underline { get => _underline; set { _underline = value; Version++; } }
    /// <summary>Text color. Default black.</summary>
    public ReportColor Color { get => _color; set { _color = value; Version++; } }
    /// <summary>Alignment of the lines inside the text's width. Default left.</summary>
    public TextAlignment Alignment { get => _alignment; set { _alignment = value; Version++; } }
    /// <summary>Line height as a multiple of the font size. Default 1.2.</summary>
    public float LineSpacing { get => _lineSpacing; set { _lineSpacing = value; Version++; } }

    /// <summary>Rotation in degrees, counter-clockwise. Drawn by the PDF renderer only; layout uses the unrotated box.</summary>
    public float Rotation { get => _rotation; set { _rotation = value; Version++; } }

    /// <summary>
    /// Optional delegate that overrides <see cref="Bold"/> at render time.
    /// The delegate receives no arguments; close over any data context you need.
    /// When <c>null</c>, <see cref="Bold"/> is used.
    /// </summary>
    public Func<bool>? BoldResolver { get => _boldResolver; set { _boldResolver = value; Version++; } }

    /// <summary>
    /// Optional delegate that overrides <see cref="Italic"/> at render time.
    /// When <c>null</c>, <see cref="Italic"/> is used.
    /// </summary>
    public Func<bool>? ItalicResolver { get => _italicResolver; set { _italicResolver = value; Version++; } }

    /// <summary>
    /// Optional delegate that overrides <see cref="Color"/> at render time.
    /// When <c>null</c>, <see cref="Color"/> is used.
    /// </summary>
    public Func<ReportColor>? ColorResolver { get => _colorResolver; set { _colorResolver = value; Version++; } }

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
