namespace FluentReport.Elements;

/// <summary>Adds empty space around its child. All values are in points.</summary>
public class PaddingElement : IElement
{
    /// <summary>The padded element.</summary>
    public IElement? Child { get; set; }
    /// <summary>Space above the child.</summary>
    public float Top { get; set; }
    /// <summary>Space below the child.</summary>
    public float Bottom { get; set; }
    /// <summary>Space to the left of the child.</summary>
    public float Left { get; set; }
    /// <summary>Space to the right of the child.</summary>
    public float Right { get; set; }
}
