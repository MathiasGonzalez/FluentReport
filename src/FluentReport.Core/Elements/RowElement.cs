namespace FluentReport.Elements;

/// <summary>One item of a <see cref="RowElement"/> and how wide it is.</summary>
public class RowItem
{
    /// <summary>The item's content.</summary>
    public IElement? Element { get; set; }
    /// <summary>Width in points; <c>null</c> makes the item relative.</summary>
    public float? FixedWidth { get; set; }
    /// <summary>Share of the width left by the fixed items, relative to the other relative items. Default 1.</summary>
    public float RelativeWidth { get; set; } = 1;
    /// <summary>True when the item has no fixed width.</summary>
    public bool IsRelative => !FixedWidth.HasValue;
}

/// <summary>Places its items side by side, separated by <see cref="Spacing"/> points.</summary>
public class RowElement : IElement
{
    /// <summary>The items, left to right.</summary>
    public List<RowItem> Items { get; } = new();
    /// <summary>Horizontal gap between items, in points.</summary>
    public float Spacing { get; set; }
}
