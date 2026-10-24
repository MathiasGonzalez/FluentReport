namespace FluentReport.Elements;

/// <summary>
/// An element only some renderers can draw (charts, raw drawing). Renderers that do not support it
/// render <see cref="Fallback"/> instead, or nothing when it is <c>null</c>, rather than failing.
/// </summary>
public interface IRendererSpecificElement : IElement
{
    IElement? Fallback { get; }
}
