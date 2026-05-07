using FluentReport.Elements;

namespace FluentReport.Builders;

public class ColumnBuilder(ColumnElement column)
{
    public ColumnBuilder Spacing(float spacing) { column.Spacing = spacing; return this; }

    public ContainerBuilder Item()
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        column.Items.Add(lazy);
        return cb;
    }
}
