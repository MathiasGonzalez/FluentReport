using FluentReport.Core;
using FluentReport.Styling;

namespace FluentReport.Elements;

public class BorderElement : ElementBase
{
    public IElement? Child { get; set; }
    public BorderStyle Border { get; set; } = new();
    public ReportColor? BackgroundColor { get; set; }

    public override Size Measure(MeasureContext context)
    {
        if (Child == null) return Size.Zero;
        return Child.Measure(context);
    }

    public override void Render(RenderContext context, Position position, Size size)
    {
        var canvas = context.Canvas;

        if (BackgroundColor.HasValue)
            canvas.DrawFilledRect(position.X, position.Y, size.Width, size.Height, BackgroundColor.Value);

        Child?.Render(context, position, size);

        if (Border.Width > 0)
        {
            if (Border.Sides.HasFlag(BorderSide.Top))
                canvas.DrawLine(position.X, position.Y, position.X + size.Width, position.Y, Border.Color, Border.Width);
            if (Border.Sides.HasFlag(BorderSide.Bottom))
                canvas.DrawLine(position.X, position.Y + size.Height, position.X + size.Width, position.Y + size.Height, Border.Color, Border.Width);
            if (Border.Sides.HasFlag(BorderSide.Left))
                canvas.DrawLine(position.X, position.Y, position.X, position.Y + size.Height, Border.Color, Border.Width);
            if (Border.Sides.HasFlag(BorderSide.Right))
                canvas.DrawLine(position.X + size.Width, position.Y, position.X + size.Width, position.Y + size.Height, Border.Color, Border.Width);
        }
    }
}
