using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Rendering;

namespace FluentReport.Tests;

public class PaginationTests
{
    private static IReadOnlyList<PageLayout> Pages(Document doc)
    {
        using var renderer = new DocumentRenderer(doc.Settings, TestFonts.Options);
        return renderer.Pages;
    }

    private static Document Doc(Action<PageBuilder> page) => Document.Create(c => c.Page(p =>
    {
        p.Size(PageSizes.A4);
        p.MarginAll(40);
        page(p);
    }));

    private static void Lines(ColumnBuilder col, int count)
    {
        for (int i = 0; i < count; i++) col.Item().Text($"line {i}");
    }

    [Fact]
    public void Column_SplitsAcrossPages()
        => Assert.True(Pages(Doc(p => p.Content().Column(c => Lines(c, 200)))).Count >= 4);

    [Fact]
    public void Column_WithDecorators_StillSplits()
    {
        var plain = Pages(Doc(p => p.Content().Column(c => Lines(c, 200)))).Count;
        var padded = Pages(Doc(p => p.Content().PaddingVertical(10).Background("#EEEEEE").Column(c => Lines(c, 200)))).Count;
        Assert.InRange(padded, plain, plain + 1);
    }

    [Fact]
    public void NestedColumn_SplitsAcrossPages()
        => Assert.True(Pages(Doc(p => p.Content().Column(o => o.Item().Column(c => Lines(c, 200))))).Count >= 4);

    [Fact]
    public void EveryLine_IsPlacedExactlyOnce()
    {
        var pages = Pages(Doc(p => p.Content().Column(c => Lines(c, 200))));
        var texts = pages.SelectMany(pg => Texts(pg.Content!)).ToList();
        Assert.Equal(Enumerable.Range(0, 200).Select(i => $"line {i}"), texts);
    }

    private static IEnumerable<string> Texts(IElement element) => element switch
    {
        TextElement t => [string.Concat(t.Spans.Select(s => s.StaticText))],
        TextBlock b => [string.Join(" ", b.Lines.SelectMany(l => l.Runs).Select(r => r.Text))],
        ColumnElement c => c.Items.SelectMany(Texts),
        PaddingElement p when p.Child != null => Texts(p.Child),
        BorderElement b when b.Child != null => Texts(b.Child),
        _ => []
    };

    [Fact]
    public void LongTable_SplitsByRows_RepeatsHeader_AndKeepsFooterLast()
    {
        var pages = Pages(Doc(p => p.Content().Table(t =>
        {
            t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
            t.Header(h => { h.Cell().Text("Item"); h.Cell().Text("Price"); });
            for (int i = 0; i < 200; i++) { t.Cell().Text($"Row {i}"); t.Cell().Text("9.99"); }
            t.Footer(f => { f.Cell().Text("Total"); f.Cell().Text("1998"); });
        })));

        Assert.True(pages.Count > 1);
        var tables = pages.Select(pg => Assert.IsType<TableElement>(pg.Content)).ToList();
        Assert.All(tables, t => Assert.Equal(2, t.HeaderCells.Count));
        Assert.Equal(400, tables.Sum(t => t.DataCells.Count));
        Assert.All(tables.SkipLast(1), t => Assert.Empty(t.FooterCells));
        Assert.Equal(2, tables[^1].FooterCells.Count);
    }

    [Fact]
    public void LongParagraph_SplitsByLines()
    {
        var text = string.Join(' ', Enumerable.Repeat("lorem ipsum dolor sit amet", 600));
        var pages = Pages(Doc(p => p.Content().Text(text)));

        Assert.True(pages.Count > 1);
        Assert.All(pages, pg => Assert.IsType<TextBlock>(pg.Content));
    }

    [Fact]
    public void PageBreaks_StartNewPages_WithoutBlankPages()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().PageBreak();                 // leading: ignored
            c.Item().Text("one");
            c.Item().PageBreak();
            c.Item().PageBreak();                 // repeated: ignored
            c.Item().Column(n => { n.Item().Text("two"); n.Item().PageBreak(); }); // nested, trailing
            c.Item().Text("three");
            c.Item().PageBreak();                 // trailing: ignored
        })));

        Assert.Equal(new[] { "one", "two", "three" }, pages.Select(pg => string.Join("", Texts(pg.Content!))));
    }

    [Fact]
    public void HeaderAndFooter_ReduceTheContentArea()
    {
        var without = Pages(Doc(p => p.Content().Column(c => Lines(c, 300)))).Count;
        var with = Pages(Doc(p =>
        {
            p.Header().Height(120);
            p.Footer().Height(120);
            p.Content().Column(c => Lines(c, 300));
        })).Count;
        Assert.True(with > without);
    }

    [Fact]
    public void ElementTallerThanAPage_IsPlacedAndPaginationContinues()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Row(r => r.RelativeItem().Spacer(2000));
            c.Item().Text("after");
        })));

        Assert.Equal(2, pages.Count);
        Assert.Equal("after", string.Join("", Texts(pages[1].Content!)));
    }

    // ── Page breaks in nested and decorated containers ───────────────────────

    private static string[] PageTexts(IReadOnlyList<PageLayout> pages)
        => pages.Select(pg => string.Join("", Texts(pg.Content!))).ToArray();

    [Fact]
    public void PageBreak_AtTheStartOfANestedColumn_BreaksWhenThePageHasContent()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Text("one");
            c.Item().Column(n => { n.Item().PageBreak(); n.Item().Text("two"); });
        })));

        Assert.Equal(new[] { "one", "two" }, PageTexts(pages));
    }

    [Fact]
    public void PageBreak_AtTheStartOfADecoratedNestedColumn_BreaksWhenThePageHasContent()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Text("one");
            c.Item().Padding(5).Column(n => { n.Item().PageBreak(); n.Item().Text("two"); });
        })));

        Assert.Equal(new[] { "one", "two" }, PageTexts(pages));
    }

    [Fact]
    public void TrailingPageBreak_InsideADecoratedColumn_AddsNoBlankPage()
    {
        static void Content(ColumnBuilder c) { c.Item().Text("one"); c.Item().PageBreak(); }

        Assert.Single(Pages(Doc(p => p.Content().Padding(10).Column(Content))));
        Assert.Single(Pages(Doc(p => p.Content().Background("#EEEEEE").Column(Content))));
        Assert.Single(Pages(Doc(p => p.Content().AlignCenter().Column(Content))));
        Assert.Single(Pages(Doc(p => p.Content().Padding(10).Border(1).Column(o => o.Item().Padding(5).Column(Content)))));
    }

    [Fact]
    public void PageBreak_InsideADecoratedColumn_KeepsTheDecorationOnBothPages()
    {
        var pages = Pages(Doc(p => p.Content().Padding(10).Column(c =>
        {
            c.Item().Text("one");
            c.Item().PageBreak();
            c.Item().Text("two");
        })));

        Assert.Equal(new[] { "one", "two" }, PageTexts(pages));
        Assert.All(pages, pg => Assert.IsType<PaddingElement>(pg.Content));
    }

    [Fact]
    public void DecoratedPageBreak_StillBreaks()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Text("one");
            c.Item().PaddingTop(5).PageBreak();
            c.Item().Text("two");
        })));

        Assert.Equal(new[] { "one", "two" }, PageTexts(pages));
    }

    [Fact]
    public void NestedTrailingPageBreak_LeavesNothingBeforeTheNextItem()
    {
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Spacing(10);
            c.Item().Padding(5).Column(n => { n.Item().Text("one"); n.Item().PageBreak(); });
            c.Item().Text("two");
        })));

        Assert.Equal(new[] { "one", "two" }, PageTexts(pages));
        // Only "two": an empty leftover of the first item would push it down by the column spacing.
        Assert.Single(Assert.IsType<ColumnElement>(pages[1].Content).Items);
    }

    [Fact]
    public void EmptyDecoratedBox_AfterAPageBreak_IsStillDrawn()
    {
        // A box with nothing inside still draws its border and padding: it is not a leftover of the break.
        var pages = Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Text("one");
            c.Item().PageBreak();
            c.Item().Border(1).Padding(10).Column(_ => { });
        })));

        Assert.Equal(2, pages.Count);
        Assert.IsType<BorderElement>(Assert.Single(Assert.IsType<ColumnElement>(pages[1].Content).Items));
    }

    [Fact]
    public void DecoratedColumnOfOnlyAPageBreak_AtTheEnd_AddsNoBlankPage()
        => Assert.Single(Pages(Doc(p => p.Content().Column(c =>
        {
            c.Item().Text("one");
            c.Item().Padding(5).Column(n => n.Item().PageBreak());
        }))));

    // ── Long words ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\U0001F600")]       // surrogate pair
    [InlineData("e\u0301")]          // letter + combining accent
    [InlineData("\U0001F469\u200d\U0001F469\u200d\U0001F467")] // ZWJ sequence (family)
    [InlineData("\U0001F1FA\U0001F1FE")] // flag: two regional indicators
    public void LongWord_IsNeverCutInsideATextElement(string unit)
    {
        var word = string.Concat(Enumerable.Repeat(unit, 60));
        using var fonts = new FontCache(TestFonts.Resolve);

        var block = TextLayout.Layout(new TextElement(word), 50, fonts);
        var chunks = block.Lines.SelectMany(l => l.Runs).Select(r => r.Text!).ToList();

        Assert.True(chunks.Count > 1);
        Assert.Equal(word, string.Concat(chunks));
        Assert.All(chunks, chunk => Assert.Equal(0, chunk.Length % unit.Length));
    }

    [Fact]
    public void EmptyContent_ProducesOnePage()
        => Assert.Single(Pages(Doc(p => p.Header().Text("only a header"))));
}

internal static class TestBuilderExtensions
{
    public static ContainerBuilder Height(this ContainerBuilder container, float height) => container.Spacer(height);
}
