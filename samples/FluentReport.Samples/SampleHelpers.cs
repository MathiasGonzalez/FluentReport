using FluentReport.Styling;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Globalization;

namespace FluentReport.Samples;

/// <summary>
/// Utility methods shared across all sample generators.
/// </summary>
internal static class SampleHelpers
{
    /// <summary>
    /// Formats a decimal value using InvariantCulture for deterministic output across locales.
    /// </summary>
    public static string Fmt(decimal value, string format = "N2") =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the shared TextStyle action for the standard
    /// "Generado con FluentReport – Página N de M" footer
    /// used across all Uruguayan fiscal document samples.
    /// </summary>
    public static Action<TextStyle> UyFooterStyle() =>
        s => { s.FontFamily = FacturaUY.FontPrimary; s.FontSize = FacturaUY.FontSizeLegal; };

    /// <summary>
    /// Creates a checkerboard PNG that mimics a QR code placeholder.
    /// Replace this with an actual QR-code generator (e.g. ZXing.Net) in production.
    /// <para>
    /// <paramref name="size"/> must be at least 10 so that the 10x10 cell grid and the
    /// three finder-pattern corner squares fit.
    /// </para>
    /// </summary>
    public static byte[] GenerarQrPlaceholder(int size)
    {
        if (size < 10)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 10.");

        int cell = size / 10;
        int finder = 3 * cell;
        var gray = new byte[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool dark = (x / cell + y / cell) % 2 == 0 && x % cell != cell - 1 && y % cell != cell - 1;
            bool inFinder = (x < finder && y < finder) || (x >= size - finder && y < finder) || (x < finder && y >= size - finder);
            if (inFinder)
            {
                int fx = x < finder ? x : x - (size - finder);
                int fy = y < finder ? y : y - (size - finder);
                dark = fx < 3 || fy < 3 || fx >= finder - 3 || fy >= finder - 3;
            }
            gray[y * size + x] = dark ? (byte)30 : (byte)240;
        }

        return EncodeGrayscalePng(size, size, gray);
    }

    /// <summary>Minimal 8-bit grayscale PNG encoder (no external imaging dependency).</summary>
    private static byte[] EncodeGrayscalePng(int width, int height, byte[] pixels)
    {
        var raw = new byte[height * (width + 1)];
        for (int y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, y * width, raw, y * (width + 1) + 1, width); // filter byte 0 = None

        using var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            z.Write(raw);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 0; // color type: grayscale

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", ihdr);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream png, string type, byte[] data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(len, data.Length);
        png.Write(len);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        png.Write(typeBytes);
        png.Write(data);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeBytes, data));
        png.Write(crc);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in type.Concat(data))
        {
            c ^= b;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c ^ 0xFFFFFFFF;
    }
}
