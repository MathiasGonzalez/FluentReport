using System.Text;
using FluentReport.Core;
using FluentReport.Rendering;
using SkiaSharp;

namespace FluentReport.Tests;

public class FontSubsettingTests
{
    private const string Sample = "Total: áé 12";

    private static SKTypeface Subset(SKTypeface full, string text)
    {
        var file = FontSubsetter.Subset(full, full.GetGlyphs(text));
        Assert.NotNull(file);
        using var data = SKData.CreateCopy(file);
        return SKTypeface.FromData(data);
    }

    [Fact]
    public void Subset_IsMuchSmaller_AndKeepsGlyphIdsAndMetrics()
    {
        var full = BundledFonts.Regular;
        var file = FontSubsetter.Subset(full, full.GetGlyphs(Sample))!;
        using var subset = Subset(full, Sample);

        Assert.True(file.Length < 150_000, $"subset is {file.Length} bytes");
        Assert.Equal(full.GlyphCount, subset.GlyphCount);
        Assert.Equal(full.UnitsPerEm, subset.UnitsPerEm);
        Assert.Equal(full.GetGlyphs(Sample), subset.GetGlyphs(Sample));

        using var fullFont = new SKFont(full, 12);
        using var subsetFont = new SKFont(subset, 12);
        Assert.Equal(fullFont.MeasureText(Sample), subsetFont.MeasureText(Sample));
        Assert.Equal(fullFont.Metrics.Ascent, subsetFont.Metrics.Ascent);
        Assert.Equal(fullFont.Metrics.Descent, subsetFont.Metrics.Descent);
    }

    [Fact]
    public void Subset_KeepsUsedOutlines_IncludingCompositeParts_AndEmptiesTheRest()
    {
        var full = BundledFonts.Regular;
        using var subset = Subset(full, Sample);
        using var fullFont = new SKFont(full, 100);
        using var subsetFont = new SKFont(subset, 100);

        // "á" is a composite glyph in DejaVu Sans: its outline needs "a" and the accent.
        foreach (ushort glyph in full.GetGlyphs(Sample).Distinct())
        {
            using var expected = fullFont.GetGlyphPath(glyph);
            using var actual = subsetFont.GetGlyphPath(glyph);
            Assert.Equal(expected?.ToSvgPathData(), actual?.ToSvgPathData());
        }

        ushort unused = full.GetGlyphs("W")[0];
        using var kept = fullFont.GetGlyphPath(unused);
        using var dropped = subsetFont.GetGlyphPath(unused);
        Assert.False(kept == null || kept.IsEmpty);
        Assert.True(dropped == null || dropped.IsEmpty);
        // The advance of a dropped glyph is unchanged, so it could only ever draw as a gap.
        Assert.Equal(fullFont.MeasureText("W"), subsetFont.MeasureText("W"));
    }

    [Fact]
    public void Subset_IsDeterministic()
    {
        var glyphs = BundledFonts.Bold.GetGlyphs(Sample);
        Assert.Equal(FontSubsetter.Subset(BundledFonts.Bold, glyphs), FontSubsetter.Subset(BundledFonts.Bold, glyphs));
    }

    private static Document Invoice() => Document.Create(c => c.Page(p =>
    {
        p.Size(PageSizes.A4);
        p.Header().Text("Factura Nº 1").Bold();
        p.Content().Column(col =>
        {
            col.Item().Text("Servicio de consultoría: 200,00");
            col.Item().Text("TOTAL: 244,00").Bold();
        });
        p.Footer().AlignRight().Text(t => { t.Span("Página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); });
    }));

    [Fact]
    public void GeneratePdf_EmbedsOnlyTheGlyphsUsed()
    {
        var doc = Invoice();
        var subset = doc.GeneratePdf(TestFonts.Options);
        var whole = doc.GeneratePdf(new PdfOptions { FontResolver = TestFonts.Resolve, SubsetFonts = false });

        Assert.Equal("%PDF", Encoding.ASCII.GetString(subset, 0, 4));
        Assert.True(whole.Length > 500_000, $"whole fonts: {whole.Length} bytes");
        Assert.True(subset.Length < 100_000, $"subset fonts: {subset.Length} bytes");
    }

    [Fact]
    public void GeneratePdf_WithSubsetting_IsDeterministic_AndCanRunTwice()
    {
        var doc = Invoice();
        Assert.Equal(doc.GeneratePdf(TestFonts.Options).Length, doc.GeneratePdf(TestFonts.Options).Length);
    }

    [Fact]
    public void CustomCanvas_IsDrawnOnce()
    {
        int calls = 0;
        var doc = Document.Create(c => c.Page(p => p.Content().Column(col =>
        {
            col.Item().Text("text");
            col.Item().Canvas(50, 20, (_, _) => calls++);
        })));
        doc.GeneratePdf(TestFonts.Options);
        Assert.Equal(1, calls);
    }
}
