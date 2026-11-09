namespace FluentReport.Elements;

/// <summary>Stacks its items vertically, separated by <see cref="Spacing"/> points.</summary>
public class ColumnElement : IElement
{
    /// <summary>The stacked elements, top to bottom.</summary>
    public List<IElement> Items { get; } = new();
    /// <summary>Vertical gap between items, in points.</summary>
    public float Spacing { get; set; }
}
