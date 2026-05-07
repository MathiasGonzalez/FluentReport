using FluentReport.Elements;

namespace FluentReport.Builders;

public class RowBuilder
{
    private readonly RowElement _row;

    internal RowBuilder(RowElement row) => _row = row;

    public RowBuilder Spacing(float spacing) { _row.Spacing = spacing; return this; }

    public ContainerBuilder RelativeItem(float width = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        _row.Items.Add(new RowItem { Element = lazy, RelativeWidth = width });
        return cb;
    }

    public ContainerBuilder FixedItem(float width)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        _row.Items.Add(new RowItem { Element = lazy, FixedWidth = width });
        return cb;
    }

    public ContainerBuilder Item() => RelativeItem();
}
