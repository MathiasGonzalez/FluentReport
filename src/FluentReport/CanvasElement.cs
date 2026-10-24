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
        public float Width { get; } = width;
        public float Height { get; } = height;
        public Action<SKCanvas, SKRect> Draw { get; } = draw ?? throw new ArgumentNullException(nameof(draw));
        public IElement? Fallback { get; init; }
    }
}

namespace FluentReport
{
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
