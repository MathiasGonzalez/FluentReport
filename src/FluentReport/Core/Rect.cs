namespace FluentReport.Core;

public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Position TopLeft => new(X, Y);
    public Size Size => new(Width, Height);

    public static Rect FromPositionAndSize(Position pos, Size size)
        => new(pos.X, pos.Y, size.Width, size.Height);
}
