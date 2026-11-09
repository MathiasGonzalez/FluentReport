using FluentReport.Builders;
using FluentReport.Elements;
using SkiaSharp;

namespace FluentReport.Elements
{
    /// <summary>
    /// A fixed-size area drawn with SkiaSharp. Only the PDF renderer draws it; HTML and Excel render
    /// <see cref="Fallback"/>.
    /// </summary>
    public sealed class CanvasElement(float width, float height, Action<SKCanvas, SKRect> draw) : IRendererSpecificElement
    {
        /// <summary>Width of the drawing area, in points.</summary>
        public float Width { get; } = width;
        /// <summary>Height of the drawing area, in points.</summary>
        public float Height { get; } = height;
        /// <summary>Draws inside the given rectangle; the canvas is clipped to it. Called once per render.</summary>
        public Action<SKCanvas, SKRect> Draw { get; } = draw ?? throw new ArgumentNullException(nameof(draw));
        /// <summary>What the HTML and Excel renderers render instead; <c>null</c> renders nothing.</summary>
        public IElement? Fallback { get; init; }
    }
}

namespace FluentReport
{
    /// <summary>Adds custom SkiaSharp drawing to a container.</summary>
    public static class ContainerCanvasExtensions
    {
        /// <summary>
        /// Draws custom vector graphics in a <paramref name="width"/> × <paramref name="height"/> area (points).
        /// The callback receives the canvas and the target rectangle; drawing is clipped to it. PDF only.
        /// </summary>
        public static ContainerBuilder Canvas(this ContainerBuilder container, float width, float height, Action<SKCanvas, SKRect> draw)
            => container.Element(new CanvasElement(width, height, draw));
    }
}
