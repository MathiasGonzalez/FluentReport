using FluentReport.Elements;

namespace FluentReport.Builders;

/// <summary>
/// Placeholder for a <see cref="ContainerBuilder"/> that may still be configured after it was
/// attached to its parent. <see cref="ElementTree.Resolve"/> replaces every placeholder with the
/// built element when the document is created, so renderers never see this type.
/// </summary>
internal sealed class LazyElement(ContainerBuilder builder) : IElement
{
    public IElement Build() => builder.Build();
}

internal static class ElementTree
{
    /// <summary>Returns <paramref name="element"/> with every <see cref="LazyElement"/> in its subtree built.</summary>
    public static IElement Resolve(IElement element)
    {
        var resolved = element is LazyElement lazy ? lazy.Build() : element;
        switch (resolved)
        {
            case LazyElement nested:
                return Resolve(nested);
            case ColumnElement column:
                for (int i = 0; i < column.Items.Count; i++)
                    column.Items[i] = Resolve(column.Items[i]);
                break;
            case RowElement row:
                foreach (var item in row.Items)
                    if (item.Element != null) item.Element = Resolve(item.Element);
                break;
            case TableElement table:
                foreach (var cell in table.HeaderCells.Concat(table.DataCells).Concat(table.FooterCells))
                    if (cell.Content != null) cell.Content = Resolve(cell.Content);
                break;
            case PaddingElement padding when padding.Child != null:
                padding.Child = Resolve(padding.Child);
                break;
            case BorderElement border when border.Child != null:
                border.Child = Resolve(border.Child);
                break;
            case AlignElement align when align.Child != null:
                align.Child = Resolve(align.Child);
                break;
        }
        return resolved;
    }
}
