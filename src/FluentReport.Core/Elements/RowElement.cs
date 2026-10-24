namespace FluentReport.Elements;

public class RowItem
{
    public IElement? Element { get; set; }
    public float? FixedWidth { get; set; }
    public float RelativeWidth { get; set; } = 1;
    public bool IsRelative => !FixedWidth.HasValue;
}

/// <summary>Places its items side by side, separated by <see cref="Spacing"/> points.</summary>
public class RowElement : IElement
{
    public List<RowItem> Items { get; } = new();
    public float Spacing { get; set; }
}
