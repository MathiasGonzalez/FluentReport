namespace FluentReport.Elements;

/// <summary>Horizontal position of an element inside its container.</summary>
public enum HorizontalAlignment
{
    /// <summary>At the left edge.</summary>
    Left,
    /// <summary>Centered.</summary>
    Center,
    /// <summary>At the right edge.</summary>
    Right
}

/// <summary>
/// Positions its child horizontally inside the available width. Text (also inside padding, a
/// background or a border) is aligned line by line; other content is shrunk to its width and moved.
/// </summary>
public class AlignElement : IElement
{
    /// <summary>The aligned element.</summary>
    public IElement? Child { get; set; }
    /// <summary>Where the child goes. Default left.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
}
