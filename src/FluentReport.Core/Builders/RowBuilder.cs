using FluentReport.Elements;

namespace FluentReport.Builders;

/// <summary>Adds items to a row: each one goes to the right of the previous. Every item takes the height of the tallest.</summary>
public class RowBuilder(RowElement row)
{
    /// <summary>Horizontal gap between items, in points.</summary>
    public RowBuilder Spacing(float spacing) { row.Spacing = spacing; return this; }

    /// <summary>Adds an item that shares the width left by the fixed items, in proportion to <paramref name="width"/>.</summary>
    public ContainerBuilder RelativeItem(float width = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        row.Items.Add(new RowItem { Element = lazy, RelativeWidth = width });
        return cb;
    }

    /// <summary>Adds an item <paramref name="width"/> points wide.</summary>
    public ContainerBuilder FixedItem(float width)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        row.Items.Add(new RowItem { Element = lazy, FixedWidth = width });
        return cb;
    }

    /// <summary>Adds a relative item of weight 1.</summary>
    public ContainerBuilder Item() => RelativeItem();
}
