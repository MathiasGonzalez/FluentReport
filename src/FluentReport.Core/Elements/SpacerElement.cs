namespace FluentReport.Elements;

/// <summary>Empty vertical space of <see cref="Height"/> points.</summary>
public class SpacerElement(float height = 0) : IElement
{
    /// <summary>Height of the space, in points.</summary>
    public float Height { get; } = height;
}
