using System.Text;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Html;
using FluentReport.Rendering;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

public class PdfRenderingTests
{
    private static Document Simple(string text = "Hello") =>
        Document.Create(c => c.Page(p => { p.Size(PageSizes.A4); p.Content().Text(text); }));

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality = 90)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }

    private static SKBitmap Noise(int width, int height, int seed)
    {
        var rnd = new Random(seed);
        var bitmap = new SKBitmap(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 255 / width), (byte)(y * 255 / height), (byte)rnd.Next(256)));
        return bitmap;
    }

    // ── Output ───────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePdf_ProducesAPdf()
    {
        var pdf = Simple().GeneratePdf(TestFonts.Options);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void GeneratePdf_ToFile_WritesTheFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fr-{Guid.NewGuid():N}.pdf");
        try
        {
            Simple().GeneratePdf(path, TestFonts.Options);
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void GenerateImages_ReturnsOnePngPerPage()
    {
        var doc = Document.Create(c => c.Page(p => p.Content().Column(col =>
        {
            col.Item().Text("one");
            col.Item().PageBreak();
            col.Item().Text("two");
        })));

        var images = doc.GenerateImages(1f, TestFonts.Options);
        Assert.Equal(2, images.Count);
        Assert.Equal(2, doc.GetPdfPageCount(TestFonts.Options));
        Assert.All(images, png => Assert.Equal(0x89, png[0]));
    }

    [Fact]
    public void GenerateImages_IsDeterministic()
    {
        var doc = Simple("Deterministic output");
        Assert.Equal(doc.GenerateImages(1f, TestFonts.Options)[0], doc.GenerateImages(1f, TestFonts.Options)[0]);
    }

    [Fact]
    public void GenerateImages_RejectsNonPositiveScale()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Simple().GenerateImages(0));

    [Fact]
    public void PdfMetadata_IsWritten()
    {
        var pdf = Simple().GeneratePdf(new PdfOptions { FontResolver = TestFonts.Resolve, Title = "Quarterly report" });
        Assert.Contains("Quarterly report", Encoding.Latin1.GetString(pdf));
    }

    // ── Fonts ────────────────────────────────────────────────────────────────

    [Fact]
    public void ResolverTypefaces_AreNotDisposed()
    {
        Simple("first").GeneratePdf(TestFonts.Options);
        var second = Simple("second").GeneratePdf(TestFonts.Options);
        Assert.NotEmpty(second);
        Assert.Equal("DejaVu Sans", TestFonts.Resolve(new TextStyle()).FamilyName);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SystemFont_KeepsBoldAndItalic(bool bold, bool italic)
    {
        using var fonts = new FontCache(null);
        var font = fonts.Font(new TextStyle { Bold = bold, Italic = italic });

        if (bold) Assert.True(font.Typeface.FontWeight >= 600 || font.Embolden);
        if (italic) Assert.True(font.Typeface.FontSlant != SKFontStyleSlant.Upright || font.SkewX != 0);
    }

    [Fact]
    public void SystemFont_HasGlyphs()
    {
        using var fonts = new FontCache(null);
        var glyphs = fonts.Font(new TextStyle()).GetGlyphs("Hola");

        Assert.DoesNotContain((ushort)0, glyphs);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NoSystemFonts_UsesBundledFont(bool bold, bool italic)
    {
        // What SkiaSharp 4 returns on Linux when it finds no fonts: an empty typeface.
        using var fonts = new FontCache(null, (_, _) => SKTypeface.Empty);
        var font = fonts.Font(new TextStyle { Bold = bold, Italic = italic });

        Assert.Same(BundledFonts.For(bold), font.Typeface);
        Assert.DoesNotContain((ushort)0, font.GetGlyphs("Hola ñandú 123"));
        Assert.False(font.Embolden);
        Assert.Equal(italic, font.SkewX != 0);
    }

    [Fact]
    public void NoSystemFonts_PdfStillHasText()
    {
        using var fonts = new FontCache(null, (_, _) => SKTypeface.Empty);
        using var stream = new MemoryStream();
        using (var pdf = SKDocument.CreatePdf(stream))
        {
            var canvas = pdf.BeginPage(200, 100);
            using var paint = new SKPaint { Color = SKColors.Black };
            canvas.DrawText("Hola", 10, 50, SKTextAlign.Left, fonts.Font(new TextStyle()), paint);
            pdf.EndPage();
        }

        Assert.Contains("DejaVuSans", Encoding.Latin1.GetString(stream.ToArray()));
    }

    [Fact]
    public void BundledFonts_DoNotOutliveRender()
    {
        // Bundled typefaces are shared by the process: disposing a cache must leave them usable.
        using (var fonts = new FontCache(null, (_, _) => SKTypeface.Empty))
            fonts.Font(new TextStyle());

        Assert.True(BundledFonts.HasGlyphs(BundledFonts.Regular));
    }

    // ── Images ───────────────────────────────────────────────────────────────

    [Fact]
    public void Jpeg_IsEmbeddedWithoutRecompression()
    {
        using var photo = Noise(800, 600, 1);
        var jpeg = Encode(photo, SKEncodedImageFormat.Jpeg, 80);
        var pdf = Document.Create(c => c.Page(p => p.Content().Image(jpeg))).GeneratePdf(TestFonts.Options);

        Assert.True(pdf.Length < jpeg.Length * 1.5, $"PDF {pdf.Length} bytes for a {jpeg.Length}-byte JPEG");
    }

    [Fact]
    public void ImageAboveThePixelLimit_ThrowsBeforeDecoding()
    {
        // 389 KB on disk, 10,000 x 11,000 pixels once decoded.
        var bomb = BlankPng(10_000, 11_000);
        var doc = Document.Create(c => c.Page(p => p.Content().Image(bomb)));

        var ex = Assert.Throws<InvalidOperationException>(() => doc.GeneratePdf(TestFonts.Options));
        Assert.Contains("MaxImagePixels", ex.Message);
        Assert.Throws<InvalidOperationException>(() => doc.GeneratePdf());
    }

    [Fact]
    public void MaxImagePixels_IsConfigurable()
    {
        using var bitmap = Noise(200, 200, 3);
        var doc = Document.Create(c => c.Page(p => p.Content().Image(Encode(bitmap, SKEncodedImageFormat.Png))));

        Assert.Throws<InvalidOperationException>(() => doc.GeneratePdf(new PdfOptions { FontResolver = TestFonts.Resolve, MaxImagePixels = 10_000 }));
        Assert.True(doc.GeneratePdf(new PdfOptions { FontResolver = TestFonts.Resolve, MaxImagePixels = null }).Length > 0);
    }

    [Fact]
    public void DeepElementTree_ThrowsInsteadOfOverflowingTheStack()
    {
        IElement deep = new TextElement("x");
        for (int i = 0; i < 100_000; i++) deep = new PaddingElement { Child = deep };

        // The fluent API resolves the tree first...
        Assert.Throws<InsufficientExecutionStackException>(() => Document.Create(c => c.Page(p => p.Content().Element(deep))));

        // ...and translation layers that build settings directly reach the renderers.
        var settings = new DocumentSettings();
        settings.Pages.Add(new PageSettings { ContentElement = deep });
        var doc = Document.FromSettings(settings);
        Assert.Throws<InsufficientExecutionStackException>(() => doc.GeneratePdf(TestFonts.Options));
        Assert.Throws<InsufficientExecutionStackException>(() => doc.GenerateHtml());
    }

    /// <summary>A grayscale PNG of zeros: tiny compressed, huge decoded.</summary>
    private static byte[] BlankPng(int width, int height)
    {
        static void Chunk(Stream output, string type, byte[] data)
        {
            var typeBytes = Encoding.ASCII.GetBytes(type);
            output.Write(BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(data.Length)));
            output.Write(typeBytes);
            output.Write(data);
            var crc = Crc32([.. typeBytes, .. data]);
            output.Write(BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(crc)));
        }

        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // 8-bit grayscale

        using var pixels = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(pixels, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
        {
            var row = new byte[width + 1];
            for (int y = 0; y < height; y++) zlib.Write(row);
        }

        static uint Crc32(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            foreach (var b in bytes)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
            return ~crc;
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", pixels.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    [Fact]
    public void ImageRepeatedOnEveryPage_IsEmbeddedOnce()
    {
        using var logo = Noise(200, 200, 2);
        var png = Encode(logo, SKEncodedImageFormat.Png);
        byte[] Pdf(int pages) => Document.Create(c => c.Page(p =>
        {
            p.Header().Image(png);
            p.Content().Column(col =>
            {
                for (int i = 0; i < pages; i++)
                {
                    col.Item().Text($"page {i}");
                    if (i < pages - 1) col.Item().PageBreak();
                }
            });
        })).GeneratePdf(TestFonts.Options);

        var one = Pdf(1).Length;
        var twenty = Pdf(20).Length;
        Assert.True(twenty < one * 2, $"1 page: {one} bytes, 20 pages: {twenty} bytes");
    }

    [Fact]
    public void Image_WithOnlyFixedHeight_KeepsAspectRatio()
    {
        using var bitmap = Noise(200, 100, 3);
        var doc = Document.Create(c => c.Page(p => p.Content().Element(new ImageElement(Encode(bitmap, SKEncodedImageFormat.Png)) { FixedHeight = 50 })));
        using var engine = new LayoutEngine(TestFonts.Options);
        var image = (ImageElement)doc.Settings.Pages[0].ContentElement!;

        Assert.Equal(50, engine.Measure(image, 500));
        var align = new AlignElement { Child = image, Alignment = HorizontalAlignment.Right };
        Assert.Equal(50, engine.Measure(align, 500));
    }

    [Fact]
    public void WebP_IsRendered()
    {
        using var bitmap = Noise(100, 50, 4);
        var webp = Encode(bitmap, SKEncodedImageFormat.Webp);
        using var engine = new LayoutEngine(TestFonts.Options);

        Assert.Equal(25, engine.Measure(new ImageElement(webp) { FixedWidth = 50 }, 500));
    }

    // ── Text ─────────────────────────────────────────────────────────────────

    [Fact]
    public void RichText_WrapsAcrossLines()
    {
        var text = new TextElement();
        text.AddSpan(string.Join(' ', Enumerable.Repeat("palabra", 80)));
        text.AddSpan(" fin", new TextStyle { Bold = true });
        using var engine = new LayoutEngine(TestFonts.Options);

        Assert.True(engine.Measure(text, 300) > 5 * 14);
    }

    [Fact]
    public void WordWiderThanTheLine_IsBroken()
    {
        using var fonts = new FontCache(TestFonts.Resolve);
        var block = TextLayout.Layout(new TextElement(new string('W', 200)), 100, fonts);

        Assert.True(block.Lines.Count > 1);
        Assert.All(block.Lines, l => Assert.True(l.Width <= 100.5f));
    }

    [Fact]
    public void WindowsLineEndings_BecomeLineBreaks()
    {
        using var fonts = new FontCache(TestFonts.Resolve);
        var block = TextLayout.Layout(new TextElement("one\r\ntwo"), 500, fonts);

        Assert.Equal(new[] { "one", "two" }, block.Lines.Select(l => l.Runs.Single().Text));
    }

    [Fact]
    public void SpansWithoutSpace_StayGlued()
    {
        var text = new TextElement();
        text.AddSpan("Page ");
        text.AddCurrentPageSpan();
        text.AddSpan("/");
        text.AddTotalPagesSpan();
        using var fonts = new FontCache(TestFonts.Resolve);
        var line = TextLayout.Layout(text, 500, fonts).Lines.Single();

        Assert.Equal(new[] { false, true, false, false }, line.Runs.Select(r => r.SpaceBefore));
    }

    // ── Other elements ───────────────────────────────────────────────────────

    [Fact]
    public void Subreport_IsVector_AndRendersEverywhere()
    {
        var annex = Document.Create(c => c.Page(p => p.Content().Text("ANNEX-TEXT")));
        var doc = Document.Create(c => c.Page(p => p.Content().Column(col =>
        {
            col.Item().Text("Main");
            col.Item().Subreport(annex);
        })));

        Assert.DoesNotContain("/Subtype /Image", Encoding.Latin1.GetString(doc.GeneratePdf(TestFonts.Options)));
        Assert.Contains("ANNEX-TEXT", doc.GenerateHtml());
    }

    [Fact]
    public void Chart_DrawsInPdf_AndHtmlUsesTheFallback()
    {
        var doc = Document.Create(c => c.Page(p => p.Content().Chart()
            .Type(ChartType.Bar).Title("Sales")
            .Categories(["Q1", "Q2"])
            .AddSeries("Revenue", [100, -20])
            .Fallback("CHART-FALLBACK")));

        Assert.NotEmpty(doc.GeneratePdf(TestFonts.Options));
        Assert.Contains("CHART-FALLBACK", doc.GenerateHtml());
    }

    [Fact]
    public void Canvas_CallbackReceivesItsRectangle()
    {
        SKRect received = default;
        var doc = Document.Create(c => c.Page(p =>
        {
            p.MarginAll(40);
            p.Content().Canvas(120, 30, (canvas, rect) => received = rect);
        }));

        doc.GeneratePdf(TestFonts.Options);
        Assert.Equal(SKRect.Create(40, 40, 120, 30), received);
        Assert.DoesNotContain("<img", doc.GenerateHtml());
    }

    [Fact]
    public void UnknownElement_Throws()
    {
        var settings = new DocumentSettings();
        settings.Pages.Add(new PageSettings { ContentElement = new CustomElement() });
        Assert.Throws<NotSupportedException>(() => Document.FromSettings(settings).GeneratePdf(TestFonts.Options));
    }

    private sealed class CustomElement : IElement;
}
