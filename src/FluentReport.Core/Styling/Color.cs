namespace FluentReport.Styling;

/// <summary>An RGBA color, independent of any rendering library.</summary>
public readonly struct ReportColor
{
    /// <summary>Red, 0–255.</summary>
    public byte R { get; }
    /// <summary>Green, 0–255.</summary>
    public byte G { get; }
    /// <summary>Blue, 0–255.</summary>
    public byte B { get; }
    /// <summary>Opacity, 0 (transparent) to 255 (opaque).</summary>
    public byte A { get; }

    /// <summary>Creates a color from its components. Opaque by default.</summary>
    public ReportColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    /// <summary>Opaque black.</summary>
    public static ReportColor Black => new(0, 0, 0);
    /// <summary>Opaque white.</summary>
    public static ReportColor White => new(255, 255, 255);
    /// <summary>Medium gray (128, 128, 128).</summary>
    public static ReportColor Gray => new(128, 128, 128);
    /// <summary>Light gray (211, 211, 211).</summary>
    public static ReportColor LightGray => new(211, 211, 211);
    /// <summary>Fully transparent.</summary>
    public static ReportColor Transparent => new(0, 0, 0, 0);

    /// <summary>Parses <c>RRGGBB</c> or <c>RRGGBBAA</c>, with or without a leading <c>#</c>. Throws <see cref="ArgumentException"/> for any other length.</summary>
    public static ReportColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return new(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16)
            );
        }
        if (hex.Length == 8)
        {
            return new(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16)
            );
        }
        throw new ArgumentException($"Invalid hex color: #{hex}");
    }
}
