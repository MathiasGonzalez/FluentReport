using SkiaSharp;

namespace FluentReport.Core;

public class RenderContext
{
    public required SKCanvas Canvas { get; init; }
    public float AvailableWidth { get; set; }
    public float AvailableHeight { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public Position CurrentPosition { get; set; }
}
