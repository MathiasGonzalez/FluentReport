using FluentReport.Core;
using FluentReport.Rendering;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

/// <summary>
/// Tests for <see cref="SkiaFonts.TypefaceFactory"/> isolation, correct honoring across
/// all code paths, and absence of resource-leak / disposal bugs.
/// All tests in this class run sequentially to avoid interference with the global factory.
/// </summary>
[Collection("SkiaFontsTests")]
public class SkiaFontsTests : IDisposable
{
    private readonly Func<TextStyle, SKTypeface>? _factoryBefore;

    public SkiaFontsTests()
    {
        // Save the global factory so each test starts clean and restores state on teardown.
        _factoryBefore = SkiaFonts.TypefaceFactory;
        SkiaFonts.TypefaceFactory = null;
    }

    public void Dispose()
    {
        SkiaFonts.TypefaceFactory = _factoryBefore;
    }

    // ── TypefaceFactory honored in all code paths ────────────────────────────

    [Fact]
    public void TypefaceFactory_IsCalledByMeasureText_WithTextStyle()
    {
        int calls = 0;
        SkiaFonts.TypefaceFactory = style => { calls++; return SKTypeface.Default; };

        var measurer = new SkiaTextMeasurer();
        measurer.MeasureText("hello", new TextStyle { FontSize = 12 });

        Assert.True(calls > 0, "TypefaceFactory should be called when measuring text with a TextStyle.");
    }

    [Fact]
    public void TypefaceFactory_IsCalledByWrapText()
    {
        int calls = 0;
        SkiaFonts.TypefaceFactory = style => { calls++; return SKTypeface.Default; };

        var measurer = new SkiaTextMeasurer();
        measurer.WrapText("hello world", new TextStyle { FontSize = 12 }, 100f);

        Assert.True(calls > 0, "TypefaceFactory should be called by WrapText.");
    }

    [Fact]
    public void TypefaceFactory_IsCalledByMeasureText_WithFontFamilyOverload()
    {
        int calls = 0;
        SkiaFonts.TypefaceFactory = style => { calls++; return SKTypeface.Default; };

        var measurer = new SkiaTextMeasurer();
        measurer.MeasureText("hello", 12f, "Arial");

        Assert.True(calls > 0,
            "TypefaceFactory should be called by the (string, float, string?) MeasureText overload too.");
    }

    // ── No disposal issues across repeated calls ─────────────────────────────

    [Fact]
    public void MeasureText_CalledRepeatedly_ProducesConsistentResults()
    {
        // Regression test: a shared SKTypeface that gets disposed on the first call
        // would produce different widths on subsequent calls (falling back to system font).
        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontSize = 12 };

        float first = measurer.MeasureText("Hello World", style);
        float second = measurer.MeasureText("Hello World", style);
        float third = measurer.MeasureText("Hello World", style);

        Assert.True(first > 0);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void MeasureText_WithFactory_CalledRepeatedly_ProducesConsistentResults()
    {
        // Same regression test but with TypefaceFactory active.
        // The factory must create a fresh instance on every call because callers
        // wrap the returned typeface in a 'using' block.
        SkiaFonts.TypefaceFactory = _ => SKTypeface.Default;

        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontSize = 12 };

        float first = measurer.MeasureText("Hello World", style);
        float second = measurer.MeasureText("Hello World", style);
        float third = measurer.MeasureText("Hello World", style);

        Assert.True(first > 0);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void WrapText_WithFactory_CalledRepeatedly_ProducesConsistentResults()
    {
        SkiaFonts.TypefaceFactory = _ => SKTypeface.Default;

        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontSize = 12 };

        var first = measurer.WrapText("Hello World Test", style, 50f);
        var second = measurer.WrapText("Hello World Test", style, 50f);

        Assert.Equal(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
            Assert.Equal(first[i], second[i]);
    }

    // ── SnapshotFontFixture isolation ────────────────────────────────────────

    [Fact]
    public void SnapshotFontFixture_Dispose_RestoresPreviousFactory()
    {
        // Arrange: install a sentinel factory before creating the fixture.
        int sentinelCalls = 0;
        Func<TextStyle, SKTypeface> sentinel = _ => { sentinelCalls++; return SKTypeface.Default; };
        SkiaFonts.TypefaceFactory = sentinel;

        // Act: create and immediately dispose the fixture.
        var fixture = new SnapshotFontFixture();
        Assert.NotSame(sentinel, SkiaFonts.TypefaceFactory); // fixture should have replaced it
        fixture.Dispose();

        // Assert: the sentinel factory is restored.
        Assert.Same(sentinel, SkiaFonts.TypefaceFactory);
    }

    [Fact]
    public void SnapshotFontFixture_Dispose_RestoresNullWhenNoFactoryWasSet()
    {
        SkiaFonts.TypefaceFactory = null;

        var fixture = new SnapshotFontFixture();
        Assert.NotNull(SkiaFonts.TypefaceFactory); // fixture installs its own factory
        fixture.Dispose();

        Assert.Null(SkiaFonts.TypefaceFactory); // back to null
    }

    [Fact]
    public void SnapshotFontFixture_MeasureText_ReturnsSameResultAcrossMultipleCalls()
    {
        // The fixture must provide a fresh SKTypeface per call; if it shared one and it
        // got disposed, subsequent measurements would produce different widths.
        using var fixture = new SnapshotFontFixture();

        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontSize = 14 };

        float w1 = measurer.MeasureText("Snapshot test", style);
        float w2 = measurer.MeasureText("Snapshot test", style);
        float w3 = measurer.MeasureText("Snapshot test", style);

        Assert.True(w1 > 0);
        Assert.Equal(w1, w2);
        Assert.Equal(w1, w3);
    }

    // ── Full render pipeline with custom factory ─────────────────────────────

    [Fact]
    public void GenerateImages_WithTypefaceFactory_ProducesNonEmptyPng()
    {
        int factoryCalls = 0;
        SkiaFonts.TypefaceFactory = style => { factoryCalls++; return SKTypeface.Default; };

        var png = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Text("Factory test").FontSize(14);
            });
        }).GenerateImages()[0];

        Assert.NotEmpty(png);
        Assert.True(factoryCalls > 0, "TypefaceFactory should have been called during PNG rendering.");
    }

    [Fact]
    public void GeneratePdf_WithTypefaceFactory_ProducesValidPdf()
    {
        int factoryCalls = 0;
        SkiaFonts.TypefaceFactory = style => { factoryCalls++; return SKTypeface.Default; };

        var pdf = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Text("Factory test PDF").FontSize(14);
            });
        }).GeneratePdf();

        Assert.NotEmpty(pdf);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
        Assert.True(factoryCalls > 0, "TypefaceFactory should have been called during PDF rendering.");
    }

    [Fact]
    public void GenerateImages_CalledRepeatedly_ProducesByteLevelIdenticalOutput()
    {
        // With the same TypefaceFactory active, two identical documents must produce
        // byte-for-byte identical PNGs.  Any disposal bug would produce divergent output
        // on the second call.
        SkiaFonts.TypefaceFactory = _ => SKTypeface.Default;

        byte[] Render() => Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Repeated render test").FontSize(14);
                    col.Item().Text("Second line of text").FontSize(12);
                });
            });
        }).GenerateImages()[0];

        var first = Render();
        var second = Render();

        Assert.Equal(first.Length, second.Length);
        Assert.Equal(first, second);
    }
}

[CollectionDefinition("SkiaFontsTests", DisableParallelization = true)]
public class SkiaFontsTestsCollection { }
