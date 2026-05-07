namespace FluentReport.Styling;

public readonly struct ReportColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public ReportColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static ReportColor Black => new(0, 0, 0);
    public static ReportColor White => new(255, 255, 255);
    public static ReportColor Gray => new(128, 128, 128);
    public static ReportColor LightGray => new(211, 211, 211);
    public static ReportColor Transparent => new(0, 0, 0, 0);

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
