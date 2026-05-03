using FluentReport.Core;
using FluentReport.Elements;

namespace FluentReport.Tests;

public class LayoutTests
{
    [Fact]
    public void TextElement_Measure_ReturnsNonZeroSize()
    {
        var text = new TextElement("Hello World");
        var size = text.Measure(new MeasureContext { AvailableWidth = 500, AvailableHeight = 1000 });
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void SpacerElement_Measure_ReturnsCorrectHeight()
    {
        var spacer = new SpacerElement(20);
        var size = spacer.Measure(new MeasureContext { AvailableWidth = 500, AvailableHeight = 1000 });
        Assert.Equal(20, size.Height);
    }

    [Fact]
    public void PaddingElement_Measure_AddsCorrectPadding()
    {
        var inner = new SpacerElement(10);
        var padding = new PaddingElement
        {
            Child = inner,
            Top = 5, Bottom = 5, Left = 10, Right = 10
        };
        var size = padding.Measure(new MeasureContext { AvailableWidth = 200, AvailableHeight = 500 });
        Assert.Equal(20, size.Height); // 10 + 5 + 5
    }

    [Fact]
    public void ColumnElement_Measure_StacksItemsVertically()
    {
        var col = new ColumnElement();
        col.Items.Add(new SpacerElement(10));
        col.Items.Add(new SpacerElement(20));
        col.Items.Add(new SpacerElement(30));

        var size = col.Measure(new MeasureContext { AvailableWidth = 500, AvailableHeight = 1000 });
        Assert.Equal(60, size.Height);
    }

    [Fact]
    public void ColumnElement_Measure_IncludesSpacing()
    {
        var col = new ColumnElement { Spacing = 5 };
        col.Items.Add(new SpacerElement(10));
        col.Items.Add(new SpacerElement(10));

        var size = col.Measure(new MeasureContext { AvailableWidth = 500, AvailableHeight = 1000 });
        Assert.Equal(25, size.Height); // 10 + 5 + 10
    }

    [Fact]
    public void TableElement_Measure_CalculatesCorrectSize()
    {
        var table = new TableElement();
        table.Columns.Add(new TableColumnDefinition { RelativeWidth = 1 });
        table.Columns.Add(new TableColumnDefinition { RelativeWidth = 1 });

        var cell1 = new SpacerElement(30);
        var cell2 = new SpacerElement(30);
        table.DataCells.Add(new TableCell { Content = cell1 });
        table.DataCells.Add(new TableCell { Content = cell2 });

        var size = table.Measure(new MeasureContext { AvailableWidth = 400, AvailableHeight = 1000 });
        Assert.Equal(400, size.Width);
        Assert.Equal(30, size.Height);
    }

    [Fact]
    public void PageSize_A4_HasCorrectDimensions()
    {
        Assert.Equal(595.28f, PageSizes.A4.Width);
        Assert.Equal(841.89f, PageSizes.A4.Height);
    }

    [Fact]
    public void PageSize_Landscape_SwapsDimensions()
    {
        var landscape = PageSizes.A4.Landscape();
        Assert.Equal(841.89f, landscape.Width);
        Assert.Equal(595.28f, landscape.Height);
    }

    [Fact]
    public void LineElement_Measure_HorizontalHasThicknessHeight()
    {
        var line = new LineElement { Thickness = 2, Direction = LineDirection.Horizontal };
        var size = line.Measure(new MeasureContext { AvailableWidth = 300, AvailableHeight = 500 });
        Assert.Equal(300, size.Width);
        Assert.Equal(2, size.Height);
    }
}
