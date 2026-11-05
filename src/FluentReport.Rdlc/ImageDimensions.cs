using System.Buffers.Binary;

namespace FluentReport.Rdlc;

/// <summary>
/// Reads the pixel size and resolution of PNG, JPEG, GIF and BMP images from their headers, so the
/// importer can size images the way SSRS does without decoding them.
/// </summary>
internal static class ImageDimensions
{
    private const float DefaultDpi = 96f;

    /// <summary>The image's natural size in points, or <c>null</c> when the format is not recognized.</summary>
    public static (float Width, float Height)? SizeInPoints(byte[] bytes)
    {
        try
        {
            var info = Png(bytes) ?? Jpeg(bytes) ?? Gif(bytes) ?? Bmp(bytes);
            if (info is not { } i || i.Width <= 0 || i.Height <= 0) return null;
            float dpiX = i.DpiX > 1 ? i.DpiX : DefaultDpi;
            float dpiY = i.DpiY > 1 ? i.DpiY : DefaultDpi;
            return (i.Width * 72f / dpiX, i.Height * 72f / dpiY);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            return null; // truncated header
        }
    }

    private readonly record struct Info(int Width, int Height, float DpiX, float DpiY);

    private static Info? Png(byte[] b)
    {
        if (b.Length < 24 || ImageFormat(b) != "png") return null;
        int width = BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16));
        int height = BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20));
        float dpiX = 0, dpiY = 0;

        // pHYs chunk: pixels per unit, unit 1 = metre.
        for (int pos = 8; pos + 8 <= b.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(pos));
            var type = System.Text.Encoding.ASCII.GetString(b, pos + 4, 4);
            if (type == "pHYs" && length >= 9 && pos + 17 <= b.Length && b[pos + 16] == 1)
            {
                dpiX = BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos + 8)) * 0.0254f;
                dpiY = BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(pos + 12)) * 0.0254f;
                break;
            }
            if (type is "IDAT" or "IEND" || length < 0) break;
            pos += 12 + length;
        }
        return new Info(width, height, dpiX, dpiY);
    }

    private static Info? Jpeg(byte[] b)
    {
        if (ImageFormat(b) != "jpeg") return null;
        float dpiX = 0, dpiY = 0;
        int pos = 2;
        while (pos + 4 <= b.Length)
        {
            if (b[pos] != 0xFF) { pos++; continue; }
            byte marker = b[pos + 1];
            if (marker is 0xD8 or 0x01 || marker is >= 0xD0 and <= 0xD7) { pos += 2; continue; }
            if (marker == 0xFF) { pos++; continue; }
            int length = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(pos + 2));

            // APP0 JFIF: density units 1 = dpi, 2 = dots per cm.
            if (marker == 0xE0 && length >= 14 && pos + 16 <= b.Length && b.AsSpan(pos + 4, 5).SequenceEqual("JFIF\0"u8))
            {
                byte units = b[pos + 11];
                float x = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(pos + 12));
                float y = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(pos + 14));
                if (units == 1) (dpiX, dpiY) = (x, y);
                else if (units == 2) (dpiX, dpiY) = (x * 2.54f, y * 2.54f);
            }

            // SOFn (except DHT, JPG and DAC): precision, height, width.
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC && pos + 9 <= b.Length)
            {
                int height = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(pos + 5));
                int width = BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(pos + 7));
                return new Info(width, height, dpiX, dpiY);
            }
            pos += 2 + length;
        }
        return null;
    }

    private static Info? Gif(byte[] b)
        => b.Length >= 10 && ImageFormat(b) == "gif"
            ? new Info(BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(6)), BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(8)), 0, 0)
            : null;

    private static Info? Bmp(byte[] b)
    {
        if (b.Length < 26 || ImageFormat(b) != "bmp") return null;
        int width = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(18));
        int height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(22))); // negative = top-down
        float dpiX = 0, dpiY = 0;
        if (b.Length >= 46)
        {
            dpiX = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(38)) * 0.0254f; // pixels per metre
            dpiY = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(42)) * 0.0254f;
        }
        return new Info(width, height, dpiX, dpiY);
    }

    private static string? ImageFormat(byte[] b) => FluentReport.Elements.ImageElement.DetectMimeType(b)?["image/".Length..];
}
