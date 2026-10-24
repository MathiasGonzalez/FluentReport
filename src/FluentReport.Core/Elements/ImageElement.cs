namespace FluentReport.Elements;

public class ImageElement : IElement
{
    public float? FixedWidth { get; set; }
    public float? FixedHeight { get; set; }

    /// <summary>Encoded image bytes (PNG, JPEG, GIF, WebP, BMP). May be empty when no image is available.</summary>
    public byte[] SourceBytes { get; }

    /// <summary>
    /// MIME type detected from the file signature, or <c>null</c> when the bytes are empty
    /// or not a recognized image format. Renderers must not emit bytes whose type is <c>null</c>.
    /// </summary>
    public string? MimeType { get; }

    /// <summary>Reads the image from <paramref name="path"/>. Throws when the file does not exist.</summary>
    public ImageElement(string path) : this(File.ReadAllBytes(path)) { }

    public ImageElement(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        SourceBytes = imageBytes;
        MimeType = DetectMimeType(imageBytes);
    }

    /// <summary>Returns the MIME type of a known image signature, or <c>null</c>.</summary>
    public static string? DetectMimeType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return "image/png";
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return "image/jpeg";
        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8)) return "image/gif";
        if (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        if (bytes.StartsWith("BM"u8)) return "image/bmp";
        return null;
    }
}
