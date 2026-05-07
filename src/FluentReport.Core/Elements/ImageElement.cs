using FluentReport.Core;

namespace FluentReport.Elements;

public enum ImageFit { Contain, Cover, Fill, FitWidth, FitHeight }

public class ImageElement : ElementBase
{
    private readonly float _naturalWidth;
    private readonly float _naturalHeight;

    public float? FixedWidth { get; set; }
    public float? FixedHeight { get; set; }
    public ImageFit Fit { get; set; } = ImageFit.Contain;

    /// <summary>Raw image bytes as provided to the constructor. Useful for non-Skia renderers (e.g. HTML).</summary>
    public byte[]? SourceBytes { get; private set; }

    /// <summary>Source file path when constructed from a path. Useful for non-Skia renderers.</summary>
    public string? SourcePath { get; private set; }

    public ImageElement(string path)
    {
        SourcePath = path;
        if (File.Exists(path))
        {
            SourceBytes = File.ReadAllBytes(path);
            (_naturalWidth, _naturalHeight) = ReadImageDimensions(SourceBytes);
        }
    }

    public ImageElement(byte[] imageBytes)
    {
        if (imageBytes.Length > 0)
        {
            SourceBytes = imageBytes;
            (_naturalWidth, _naturalHeight) = ReadImageDimensions(imageBytes);
        }
    }

    public override Size Measure(MeasureContext context)
    {
        if (SourceBytes == null || _naturalWidth <= 0) return Size.Zero;
        var w = FixedWidth ?? Math.Min(_naturalWidth, context.AvailableWidth);
        var h = FixedHeight ?? (_naturalHeight * w / _naturalWidth);
        return new(w, h);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (SourceBytes == null) return;
        context.Canvas.DrawImageBytes(SourceBytes, position.X, position.Y, size.Width, size.Height);
    }

    /// <summary>
    /// Reads image dimensions from the header bytes of PNG and JPEG files without
    /// requiring an external library. Internal for use by SubreportElement.
    /// </summary>
    internal static (float W, float H) ReadDimensionsPublic(byte[] bytes) => ReadImageDimensions(bytes);

    /// <summary>
    /// Reads image dimensions from the header bytes of PNG and JPEG files without
    /// requiring an external library.
    /// </summary>
    private static (float W, float H) ReadImageDimensions(byte[] bytes)
    {
        if (bytes.Length < 24) return (0, 0);

        // PNG: 8-byte signature (137 80 78 71 13 10 26 10), then IHDR chunk.
        // Width at offset 16, height at offset 20 (big-endian int32).
        if (bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71)
        {
            if (bytes.Length >= 24)
            {
                int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                return (w, h);
            }
        }

        // JPEG: starts with FF D8. Scan for SOF marker (C0–C3, C5–C7, C9–CB, CD–CF).
        if (bytes[0] == 0xFF && bytes[1] == 0xD8)
        {
            int pos = 2;
            while (pos + 4 < bytes.Length)
            {
                if (bytes[pos] != 0xFF) break;
                byte marker = bytes[pos + 1];
                bool isSof = (marker >= 0xC0 && marker <= 0xC3)
                          || (marker >= 0xC5 && marker <= 0xC7)
                          || (marker >= 0xC9 && marker <= 0xCB)
                          || (marker >= 0xCD && marker <= 0xCF);
                if (isSof && pos + 9 < bytes.Length)
                {
                    int h = (bytes[pos + 5] << 8) | bytes[pos + 6];
                    int w = (bytes[pos + 7] << 8) | bytes[pos + 8];
                    return (w, h);
                }
                if (pos + 3 < bytes.Length)
                {
                    int segLen = (bytes[pos + 2] << 8) | bytes[pos + 3];
                    pos += 2 + segLen;
                }
                else break;
            }
        }

        return (0, 0);
    }
}
