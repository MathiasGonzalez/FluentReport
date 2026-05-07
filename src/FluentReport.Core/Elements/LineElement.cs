using FluentReport.Core;
using FluentReport.Styling;

namespace FluentReport.Elements;

public enum LineDirection { Horizontal, Vertical }

public class LineElement : ElementBase
{
    public float Thickness { get; set; } = 1;
    public ReportColor Color { get; set; } = ReportColor.Black;
    public LineDirection Direction { get; set; } = LineDirection.Horizontal;

    public override Size Measure(MeasureContext context)
        => Direction == LineDirection.Horizontal
            ? new(context.AvailableWidth, Thickness)
            : new(Thickness, context.AvailableHeight);

    public override void Render(RenderContext context, Position position, Size size)
    {
        if (Direction == LineDirection.Horizontal)
            context.Canvas.DrawLine(position.X, position.Y, position.X + size.Width, position.Y, Color, Thickness);
        else
            context.Canvas.DrawLine(position.X, position.Y, position.X, position.Y + size.Height, Color, Thickness);
    }
}
