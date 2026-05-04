using FluentReport.Core;
using VerifyXunit;

namespace FluentReport.Tests;

/// <summary>
/// Visual regression tests for PDF rendering.
/// Each test renders a canonical layout to a PNG and compares it against a committed
/// golden image in the Snapshots/ directory using Verify.
///
/// Fonts are provided by <see cref="SnapshotFontFixture"/> (embedded DejaVu Sans TTF)
/// so renders are identical on every OS and CI runner.
///
/// To update goldens after an intentional visual change:
///   1. Delete the affected *.verified.png file(s) in Snapshots/
///   2. Run the tests — Verify will create a new *.received.png file
///   3. Inspect the new image and, if correct, rename it to *.verified.png
///   4. Commit the updated golden
/// </summary>
[Collection("VisualSnapshots")]
public class VisualSnapshotTests : IClassFixture<SnapshotFontFixture>
{
    public VisualSnapshotTests(SnapshotFontFixture _) { }

    [Fact]
    public Task Column_WithSpacingAndLine_MatchesSnapshot()
    {
        var pngBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Section Title").FontSize(16).Bold().FontFamily("DejaVu Sans");
                    col.Item().Line(1);
                    col.Item().Text("Body text below the separator line.").FontFamily("DejaVu Sans");
                    col.Item().Text("Second paragraph of body text.").FontFamily("DejaVu Sans");
                });
            });
        }).GenerateImages()[0];

        return Verifier.Verify(new MemoryStream(pngBytes), "png");
    }

    [Fact]
    public Task Row_WithTwoItems_MatchesSnapshot()
    {
        var pngBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Row(row =>
                {
                    row.Item().Text("Left column content").FontFamily("DejaVu Sans");
                    row.Item().Text("Right column content").FontFamily("DejaVu Sans");
                });
            });
        }).GenerateImages()[0];

        return Verifier.Verify(new MemoryStream(pngBytes), "png");
    }

    [Fact]
    public Task Table_WithHeaderAndRows_MatchesSnapshot()
    {
        var pngBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Background("#CCCCCC").Padding(5).Text("Product").Bold().FontFamily("DejaVu Sans");
                        h.Cell().Background("#CCCCCC").Padding(5).Text("Qty").Bold().FontFamily("DejaVu Sans");
                        h.Cell().Background("#CCCCCC").Padding(5).Text("Price").Bold().FontFamily("DejaVu Sans");
                    });
                    table.Cell().Padding(5).Text("Widget A").FontFamily("DejaVu Sans");
                    table.Cell().Padding(5).Text("10").FontFamily("DejaVu Sans");
                    table.Cell().Padding(5).Text("$5.00").FontFamily("DejaVu Sans");
                    table.Cell().Padding(5).Text("Gadget B").FontFamily("DejaVu Sans");
                    table.Cell().Padding(5).Text("5").FontFamily("DejaVu Sans");
                    table.Cell().Padding(5).Text("$12.50").FontFamily("DejaVu Sans");
                });
            });
        }).GenerateImages()[0];

        return Verifier.Verify(new MemoryStream(pngBytes), "png");
    }

    [Fact]
    public Task HeaderAndFooter_WithPageNumbers_MatchesSnapshot()
    {
        var pngBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Header().Text("Annual Report 2024").FontSize(18).Bold().AlignCenter().FontFamily("DejaVu Sans");
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Executive summary content goes here.").FontFamily("DejaVu Sans");
                    col.Item().Line(1);
                    col.Item().Text("Additional details follow.").FontFamily("DejaVu Sans");
                });
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GenerateImages()[0];

        return Verifier.Verify(new MemoryStream(pngBytes), "png");
    }

    [Fact]
    public Task BorderAndBackground_MatchesSnapshot()
    {
        var pngBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Border(1).Padding(10).Text("Bordered cell").FontFamily("DejaVu Sans");
                    col.Item().Background("#EEEEEE").Padding(8).Text("Shaded background cell").FontFamily("DejaVu Sans");
                    col.Item().Border(2).Background("#DDEEFF").Padding(8).Text("Bordered and shaded").Bold().FontFamily("DejaVu Sans");
                });
            });
        }).GenerateImages()[0];

        return Verifier.Verify(new MemoryStream(pngBytes), "png");
    }
}

[CollectionDefinition("VisualSnapshots", DisableParallelization = true)]
public class VisualSnapshotCollection { }
