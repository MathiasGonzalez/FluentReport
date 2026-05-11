using FluentReport.Core;

namespace FluentReport.Elements;

/// <summary>
/// An element that delegates rendering to an arbitrary drawing action.
/// Useful for rendering vector graphics (paths, shapes) extracted from external sources.
/// </summary>
public class CanvasElement : ElementBase
{
    private readonly float _width;
    private readonly float _height;
    private readonly Action<IDrawingCanvas, Position, Size> _draw;

    public CanvasElement(float width, float height, Action<IDrawingCanvas, Position, Size> draw)
    {
        _width = width;
        _height = height;
        _draw = draw;
    }

    public override Size Measure(MeasureContext context)
        => new(Math.Min(_width, context.AvailableWidth), _height);

    public override void Render(RenderContext context, Position position, Size size)
        => _draw(context.Canvas, position, size);
}
