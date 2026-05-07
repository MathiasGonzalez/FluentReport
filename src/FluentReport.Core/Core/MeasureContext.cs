namespace FluentReport.Core;

public class MeasureContext
{
    public float AvailableWidth { get; set; }
    public float AvailableHeight { get; set; }

    /// <summary>Text measurer used by elements that render text. Defaults to a throw-on-use sentinel.</summary>
    public ITextMeasurer Measurer { get; init; } = NullTextMeasurer.Instance;

    /// <summary>Creates a child context with new dimensions, propagating the current measurer.</summary>
    public MeasureContext WithDimensions(float width, float height)
        => new() { AvailableWidth = width, AvailableHeight = height, Measurer = Measurer };
}
