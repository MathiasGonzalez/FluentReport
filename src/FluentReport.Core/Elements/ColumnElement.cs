namespace FluentReport.Elements;

/// <summary>Stacks its items vertically, separated by <see cref="Spacing"/> points.</summary>
public class ColumnElement : IElement
{
    public List<IElement> Items { get; } = new();
    public float Spacing { get; set; }
}
