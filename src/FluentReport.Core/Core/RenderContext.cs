namespace FluentReport.Core;

public class RenderContext
{
    public required IDrawingCanvas Canvas { get; init; }
    public float AvailableWidth { get; set; }
    public float AvailableHeight { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public Position CurrentPosition { get; set; }

    /// <summary>Provides page images for subreport rendering. Set by the PDF renderer.</summary>
    public IPageImagesProvider? PageImagesProvider { get; init; }

    /// <summary>Creates a <see cref="MeasureContext"/> backed by this canvas's text measurer.</summary>
    public MeasureContext MeasureContextFor(float width, float height)
        => new() { AvailableWidth = width, AvailableHeight = height, Measurer = Canvas };
}
