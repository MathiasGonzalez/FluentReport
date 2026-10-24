namespace FluentReport.Elements;

/// <summary>Empty vertical space of <see cref="Height"/> points.</summary>
public class SpacerElement(float height = 0) : IElement
{
    public float Height { get; } = height;
}
