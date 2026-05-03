namespace FluentReport.Core;

public readonly record struct Size(float Width, float Height)
{
    public static readonly Size Zero = new(0, 0);
    public static readonly Size Infinite = new(float.MaxValue, float.MaxValue);

    public Size WithWidth(float width) => new(width, Height);
    public Size WithHeight(float height) => new(Width, height);
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
