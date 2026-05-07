using FluentReport.Elements;

namespace FluentReport.Builders;

public class RowBuilder(RowElement row)
{
    public RowBuilder Spacing(float spacing) { row.Spacing = spacing; return this; }

    public ContainerBuilder RelativeItem(float width = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        row.Items.Add(new RowItem { Element = lazy, RelativeWidth = width });
        return cb;
    }

    public ContainerBuilder FixedItem(float width)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        row.Items.Add(new RowItem { Element = lazy, FixedWidth = width });
        return cb;
    }

    public ContainerBuilder Item() => RelativeItem();
}
