using FluentReport.Elements;

namespace FluentReport.Builders;

/// <summary>Adds items to a column: each one goes below the previous.</summary>
public class ColumnBuilder(ColumnElement column)
{
    /// <summary>Vertical gap between items, in points.</summary>
    public ColumnBuilder Spacing(float spacing) { column.Spacing = spacing; return this; }

    /// <summary>Adds an item and returns its container.</summary>
    public ContainerBuilder Item()
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        column.Items.Add(lazy);
        return cb;
    }
}
