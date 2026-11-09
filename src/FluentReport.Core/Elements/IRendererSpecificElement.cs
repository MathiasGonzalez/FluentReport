namespace FluentReport.Elements;

/// <summary>
/// An element only some renderers can draw (charts, raw drawing). Renderers that do not support it
/// render <see cref="Fallback"/> instead, or nothing when it is <c>null</c>, rather than failing.
/// </summary>
public interface IRendererSpecificElement : IElement
{
    /// <summary>What renderers that cannot draw this element render instead; <c>null</c> renders nothing.</summary>
    IElement? Fallback { get; }
}
