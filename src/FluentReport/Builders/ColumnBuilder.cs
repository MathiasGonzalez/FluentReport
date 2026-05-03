using FluentReport.Elements;

namespace FluentReport.Builders;

public class ColumnBuilder
{
    private readonly ColumnElement _column;

    internal ColumnBuilder(ColumnElement column) => _column = column;

    public ColumnBuilder Spacing(float spacing) { _column.Spacing = spacing; return this; }

    public ContainerBuilder Item()
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        _column.Items.Add(lazy);
        return cb;
    }
}
