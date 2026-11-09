namespace FluentReport.Core;

/// <summary>A page size in points (1/72 inch).</summary>
public record PageSize(float Width, float Height)
{
    /// <summary>ISO A4, 210 × 297 mm.</summary>
    public static readonly PageSize A4 = new(595.28f, 841.89f);
    /// <summary>US Letter, 8.5 × 11 in.</summary>
    public static readonly PageSize Letter = new(612f, 792f);
    /// <summary>US Legal, 8.5 × 14 in.</summary>
    public static readonly PageSize Legal = new(612f, 1008f);
    /// <summary>ISO A3, 297 × 420 mm.</summary>
    public static readonly PageSize A3 = new(841.89f, 1190.55f);
    /// <summary>ISO A5, 148 × 210 mm.</summary>
    public static readonly PageSize A5 = new(419.53f, 595.28f);

    /// <summary>The same size with width and height swapped.</summary>
    public PageSize Landscape() => new(Height, Width);
}

/// <summary>Shortcuts for the standard sizes of <see cref="PageSize"/>.</summary>
public static class PageSizes
{
    /// <inheritdoc cref="PageSize.A4"/>
    public static PageSize A4 => PageSize.A4;
    /// <inheritdoc cref="PageSize.Letter"/>
    public static PageSize Letter => PageSize.Letter;
    /// <inheritdoc cref="PageSize.Legal"/>
    public static PageSize Legal => PageSize.Legal;
    /// <inheritdoc cref="PageSize.A3"/>
    public static PageSize A3 => PageSize.A3;
    /// <inheritdoc cref="PageSize.A5"/>
    public static PageSize A5 => PageSize.A5;
}
