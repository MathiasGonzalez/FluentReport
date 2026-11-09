using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Builders;

/// <summary>Declares the columns of a table, left to right.</summary>
public class TableColumnDefinitionBuilder(TableElement table)
{
    /// <summary>Adds a column that shares the width left by the constant columns, in proportion to <paramref name="width"/>.</summary>
    public TableColumnDefinitionBuilder RelativeColumn(float width = 1)
    {
        table.Columns.Add(new TableColumnDefinition { RelativeWidth = width });
        return this;
    }

    /// <summary>Adds a column <paramref name="width"/> points wide.</summary>
    public TableColumnDefinitionBuilder ConstantColumn(float width)
    {
        table.Columns.Add(new TableColumnDefinition { FixedWidth = width });
        return this;
    }
}

/// <summary>Adds header cells to a table. Header rows repeat on every page the table continues on.</summary>
public class TableHeaderBuilder(TableElement table)
{
    /// <summary>Adds a header cell spanning <paramref name="colSpan"/> columns and returns its container.</summary>
    public ContainerBuilder Cell(int colSpan = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        table.HeaderCells.Add(new TableCell { Content = lazy, ColumnSpan = Math.Max(1, colSpan) });
        return cb;
    }
}

/// <summary>Adds footer cells to a table. Footer rows are drawn once, after the last data row.</summary>
public class TableFooterBuilder(TableElement table)
{
    /// <summary>Adds a footer cell spanning <paramref name="colSpan"/> columns and returns its container.</summary>
    public ContainerBuilder Cell(int colSpan = 1)
    {
        var cb = new ContainerBuilder();
        table.FooterCells.Add(new TableCell { Content = new LazyElement(cb), ColumnSpan = Math.Max(1, colSpan) });
        return cb;
    }
}

/// <summary>Builds a table: columns first, then cells left to right; a row ends when its columns are filled.</summary>
public class TableBuilder(TableElement table)
{
    /// <summary>Declares the columns. Required before adding cells.</summary>
    public TableBuilder ColumnsDefinition(Action<TableColumnDefinitionBuilder> configure)
    {
        configure(new TableColumnDefinitionBuilder(table));
        return this;
    }

    /// <summary>Adds the header rows, repeated on every page the table continues on.</summary>
    public TableBuilder Header(Action<TableHeaderBuilder> configure)
    {
        configure(new TableHeaderBuilder(table));
        return this;
    }

    /// <summary>Adds rows rendered after the data rows (totals, notes).</summary>
    public TableBuilder Footer(Action<TableFooterBuilder> configure)
    {
        configure(new TableFooterBuilder(table));
        return this;
    }

    /// <summary>Adds a data cell spanning <paramref name="colSpan"/> columns and returns its container.</summary>
    public ContainerBuilder Cell(int colSpan = 1)
    {
        var cb = new ContainerBuilder();
        var lazy = new LazyElement(cb);
        table.DataCells.Add(new TableCell { Content = lazy, ColumnSpan = Math.Max(1, colSpan) });
        return cb;
    }

    /// <summary>Draws a border around every cell. Default color black.</summary>
    public TableBuilder BorderEachCell(float width = 1, string? colorHex = null)
    {
        table.BorderWidth = width;
        if (colorHex != null) table.BorderColor = ReportColor.FromHex(colorHex);
        return this;
    }
}
