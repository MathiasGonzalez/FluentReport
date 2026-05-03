namespace FluentReport.Core;

public readonly record struct Position(float X, float Y)
{
    public static readonly Position Zero = new(0, 0);
    public Position Translate(float dx, float dy) => new(X + dx, Y + dy);
}
