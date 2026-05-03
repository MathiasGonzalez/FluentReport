using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

public class TableColumnDefinitionBuilder
{
    private readonly TableElement _table;

    internal TableColumnDefinitionBuilder(TableElement table) => _table = table;

    public TableColumnDefinitionBuilder RelativeColumn(float width = 1)
    {
        _table.Columns.Add(new TableColumnDefinition { RelativeWidth = width });
        return this;
    }

    public TableColumnDefinitionBuilder ConstantColumn(float width)
    {
        _table.Columns.Add(new TableColumnDefinition { FixedWidth = width });
        return this;
    }
}

public class TableHeaderBuilder
{
    private readonly TableElement _table;

    internal TableHeaderBuilder(TableElement table) => _table = table;

    public ContainerBuilder Cell()
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        _table.HeaderCells.Add(new TableCell { Content = lazy, IsHeader = true });
        return cb;
    }
}

public class TableBuilder
{
    private readonly TableElement _table;

    internal TableBuilder(TableElement table) => _table = table;

    public TableBuilder ColumnsDefinition(Action<TableColumnDefinitionBuilder> configure)
    {
        configure(new TableColumnDefinitionBuilder(_table));
        return this;
    }

    public TableBuilder Header(Action<TableHeaderBuilder> configure)
    {
        configure(new TableHeaderBuilder(_table));
        return this;
    }

    public ContainerBuilder Cell()
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        _table.DataCells.Add(new TableCell { Content = lazy });
        return cb;
    }

    public TableBuilder BorderEachCell(float width = 1, string? colorHex = null)
    {
        _table.BorderWidth = width;
        if (colorHex != null) _table.BorderColor = ReportColor.FromHex(colorHex);
        return this;
    }
}
