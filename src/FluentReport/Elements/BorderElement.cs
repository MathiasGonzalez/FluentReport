using FluentReport.Core;
using FluentReport.Styling;
using SkiaSharp;

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
        if (BackgroundColor.HasValue)
        {
            using var bgPaint = new SKPaint { Color = BackgroundColor.Value.ToSkColor() };
            context.Canvas.DrawRect(position.X, position.Y, size.Width, size.Height, bgPaint);
        }

        Child?.Render(context, position, size);

        if (Border.Width > 0)
        {
            using var borderPaint = new SKPaint
            {
                Color = Border.Color.ToSkColor(),
                StrokeWidth = Border.Width,
                IsStroke = true,
                IsAntialias = true
            };

            if (Border.Sides.HasFlag(BorderSide.Top))
                context.Canvas.DrawLine(position.X, position.Y, position.X + size.Width, position.Y, borderPaint);
            if (Border.Sides.HasFlag(BorderSide.Bottom))
                context.Canvas.DrawLine(position.X, position.Y + size.Height, position.X + size.Width, position.Y + size.Height, borderPaint);
            if (Border.Sides.HasFlag(BorderSide.Left))
                context.Canvas.DrawLine(position.X, position.Y, position.X, position.Y + size.Height, borderPaint);
            if (Border.Sides.HasFlag(BorderSide.Right))
                context.Canvas.DrawLine(position.X + size.Width, position.Y, position.X + size.Width, position.Y + size.Height, borderPaint);
        }
    }
}
