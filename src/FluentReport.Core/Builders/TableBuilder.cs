using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

public class TableColumnDefinitionBuilder(TableElement table)
{
    public TableColumnDefinitionBuilder RelativeColumn(float width = 1)
    {
        table.Columns.Add(new TableColumnDefinition { RelativeWidth = width });
        return this;
    }

    public TableColumnDefinitionBuilder ConstantColumn(float width)
    {
        table.Columns.Add(new TableColumnDefinition { FixedWidth = width });
        return this;
    }
}

public class TableHeaderBuilder(TableElement table)
{
    public ContainerBuilder Cell(int colSpan = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        table.HeaderCells.Add(new TableCell { Content = lazy, IsHeader = true, ColumnSpan = Math.Max(1, colSpan) });
        return cb;
    }
}

public class TableBuilder(TableElement table)
{
    public TableBuilder ColumnsDefinition(Action<TableColumnDefinitionBuilder> configure)
    {
        configure(new TableColumnDefinitionBuilder(table));
        return this;
    }

    public TableBuilder Header(Action<TableHeaderBuilder> configure)
    {
        configure(new TableHeaderBuilder(table));
        return this;
    }

    public ContainerBuilder Cell(int colSpan = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        table.DataCells.Add(new TableCell { Content = lazy, ColumnSpan = Math.Max(1, colSpan) });
        return cb;
    }

    public TableBuilder BorderEachCell(float width = 1, string? colorHex = null)
    {
        table.BorderWidth = width;
        if (colorHex != null) table.BorderColor = ReportColor.FromHex(colorHex);
        return this;
    }
}
