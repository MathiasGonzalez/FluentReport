namespace FluentReport.Core;

public record PageSize(float Width, float Height)
{
    public static readonly PageSize A4 = new(595.28f, 841.89f);
    public static readonly PageSize Letter = new(612f, 792f);
    public static readonly PageSize Legal = new(612f, 1008f);
    public static readonly PageSize A3 = new(841.89f, 1190.55f);
    public static readonly PageSize A5 = new(419.53f, 595.28f);

    public PageSize Landscape() => new(Height, Width);
}

public static class PageSizes
{
    public static PageSize A4 => PageSize.A4;
    public static PageSize Letter => PageSize.Letter;
    public static PageSize Legal => PageSize.Legal;
    public static PageSize A3 => PageSize.A3;
    public static PageSize A5 => PageSize.A5;
}
