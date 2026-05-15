using FluentReport.Core;
using FluentReport.Rendering;
using FluentReport.Styling;
using SkiaSharp;

namespace FluentReport.Tests;

/// <summary>
/// Tests that guard against cross-platform rendering non-determinism.
///
/// Known sources of divergence between OSes:
///   1. LCD sub-pixel antialiasing — Skia infers <see cref="SKPixelGeometry"/> from the
///      physical display (RgbH on Windows, Unknown on Linux/macOS), producing colored fringe
///      pixels on one platform that don't exist on another.
///   2. Sub-pixel text positioning — <see cref="SKFont.Subpixel"/> shifts glyph origins by
///      fractional pixels, so the same character lands on a different pixel grid per OS.
///   3. System-font fallback — when <see cref="SkiaFonts.TypefaceFactory"/> is null, Skia
///      resolves font family names through the OS font catalog, which differs per platform.
///
/// The mitigations applied to the renderer are:
///   • <c>SKSurface</c> created with <c>SKPixelGeometry.Unknown</c>  (grayscale AA, no LCD).
///   • <c>SKFont.Edging = SKFontEdging.Antialias</c>                (grayscale, not sub-pixel).
///   • <c>SKFont.Subpixel = false</c>                               (whole-pixel glyph origins).
///   • <see cref="SnapshotFontFixture"/> embeds DejaVu Sans          (no OS font lookup).
///
/// These tests verify those mitigations hold and will catch any future regression.
/// </summary>
[Collection("VisualSnapshots")]
public class RenderingDeterminismTests : IClassFixture<SnapshotFontFixture>
{
    public RenderingDeterminismTests(SnapshotFontFixture _) { }

    // ── Idempotency ──────────────────────────────────────────────────────────

    /// <summary>
    /// The same document rendered twice must produce byte-identical PNG output.
    /// Failure indicates non-deterministic state in the render pipeline (timers,
    /// random seeds, shared mutation, etc.).
    /// </summary>
    [Fact]
    public void GenerateImages_CalledTwice_ProducesBitIdenticalOutput()
    {
        var first  = RenderSingleTextPage("Idempotency check");
        var second = RenderSingleTextPage("Idempotency check");

        Assert.Equal(first, second);
    }

    /// <summary>
    /// Five independent <c>Document.Create</c> calls with the same inputs must all produce
    /// the same bytes.  Catches any global mutable state that accumulates across document
    /// instances (e.g., a static counter or cached layout from a previous call).
    /// </summary>
    [Fact]
    public void GenerateImages_FiveIndependentDocuments_AllProduceIdenticalBytes()
    {
        var results = Enumerable.Range(0, 5)
            .Select(_ => RenderSingleTextPage("Consistency"))
            .ToList();

        for (int i = 1; i < results.Count; i++)
            Assert.True(
                results[0].SequenceEqual(results[i]),
                $"Render #{i + 1} differs from render #1 — non-deterministic state detected.");
    }

    // ── LCD sub-pixel antialiasing guard ─────────────────────────────────────

    /// <summary>
    /// When LCD sub-pixel AA is active, Skia shifts the R, G, B channels of each glyph
    /// edge differently to exploit the physical sub-pixel layout of the screen.  On a
    /// white background this manifests as colored fringe pixels (R ≠ G or G ≠ B).
    ///
    /// With grayscale AA the coverage value is applied equally to all channels, so every
    /// pixel is a shade of grey: R == G == B.  This test enforces that invariant and will
    /// fail immediately if <c>SKFont.Edging</c> or surface <c>SKPixelGeometry</c> reverts
    /// to an LCD mode.
    /// </summary>
    [Fact]
    public void GenerateImages_BlackTextOnWhiteBackground_ContainsNoColoredAaFringePixels()
    {
        var png = RenderSingleTextPage("LCD fringe check");
        Assert.Equal(0, CountColoredAaFringePixels(png));
    }

    /// <summary>
    /// Same LCD check for bold text, which uses a different typeface path in
    /// <see cref="SkiaFonts"/> and could independently regress.
    /// </summary>
    [Fact]
    public void GenerateImages_BoldTextOnWhiteBackground_ContainsNoColoredAaFringePixels()
    {
        var png = RenderSingleTextPage("Bold LCD check", bold: true);
        Assert.Equal(0, CountColoredAaFringePixels(png));
    }

    // ── Surface dimensions ───────────────────────────────────────────────────

    /// <summary>
    /// A4 is 595.28 × 841.89 points.  The surface must be ⌈595.28⌉ × ⌈841.89⌉ pixels at scale 1×.
    /// A change in the ceiling/rounding strategy would shift every visual element and invalidate
    /// all golden files.
    /// </summary>
    [Fact]
    public void GenerateImages_A4Page_OutputIs596x842Pixels()
    {
        var png = RenderSingleTextPage("Size check");
        using var bmp = SKBitmap.Decode(png)!;

        int expectedW = (int)Math.Ceiling(PageSizes.A4.Width);
        int expectedH = (int)Math.Ceiling(PageSizes.A4.Height);
        Assert.Equal(expectedW, bmp.Width);
        Assert.Equal(expectedH, bmp.Height);
    }

    /// <summary>
    /// At scale 2 the surface must be ⌈595.28 × 2⌉ × ⌈841.89 × 2⌉ pixels.
    /// The formula is <c>ceil(points × scale)</c> — scaling is applied to the float point
    /// dimension before the ceiling, not to the already-ceiled pixel size.
    /// Changing either the formula or the A4 constant would shift every rendered element
    /// relative to golden snapshots on every OS.
    /// </summary>
    [Fact]
    public void GenerateImages_A4PageAtScale2_OutputIs1191x1684Pixels()
    {
        const float scale = 2f;
        var png2x = RenderSingleTextPage("Scale 2x", scale: scale);
        using var bmp = SKBitmap.Decode(png2x)!;

        int expectedW = (int)Math.Ceiling(PageSizes.A4.Width * scale);
        int expectedH = (int)Math.Ceiling(PageSizes.A4.Height * scale);
        Assert.Equal(expectedW, bmp.Width);
        Assert.Equal(expectedH, bmp.Height);
    }

    // ── Text measurement determinism ─────────────────────────────────────────

    /// <summary>
    /// <see cref="SkiaTextMeasurer.MeasureText(string, TextStyle)"/> must return
    /// an identical width across repeated calls with the embedded DejaVu Sans font.
    /// A regression here (different values per call) would shift text positions and break
    /// layout determinism even if the surface settings are correct.
    /// </summary>
    [Fact]
    public void MeasureText_WithEmbeddedFont_ReturnsIdenticalValueAcrossRepeatedCalls()
    {
        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontFamily = "DejaVu Sans", FontSize = 12 };
        const string text = "Cross-platform measurement";

        float w1 = measurer.MeasureText(text, style);
        float w2 = measurer.MeasureText(text, style);
        float w3 = measurer.MeasureText(text, style);

        Assert.True(w1 > 0);
        Assert.Equal(w1, w2, 5);
        Assert.Equal(w1, w3, 5);
    }

    /// <summary>
    /// Text ascent (used for vertical glyph placement) must be identical across calls.
    /// A non-deterministic ascent value would cause vertical drift of text baselines.
    /// </summary>
    [Fact]
    public void GetTextAscent_WithEmbeddedFont_ReturnsIdenticalValueAcrossRepeatedCalls()
    {
        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontFamily = "DejaVu Sans", FontSize = 14 };

        float a1 = measurer.GetTextAscent(style);
        float a2 = measurer.GetTextAscent(style);
        float a3 = measurer.GetTextAscent(style);

        Assert.True(a1 > 0);
        Assert.Equal(a1, a2, 5);
        Assert.Equal(a1, a3, 5);
    }

    /// <summary>
    /// Text wrapping must produce the same line-break points on every call.
    /// A non-deterministic wrap would shift vertical layout and invalidate goldens.
    /// </summary>
    [Fact]
    public void WrapText_WithEmbeddedFont_ReturnsIdenticalBreakpointsAcrossRepeatedCalls()
    {
        var measurer = new SkiaTextMeasurer();
        var style = new TextStyle { FontFamily = "DejaVu Sans", FontSize = 12 };
        const string text = "The quick brown fox jumps over the lazy dog near the river bank.";

        var lines1 = measurer.WrapText(text, style, 200f);
        var lines2 = measurer.WrapText(text, style, 200f);
        var lines3 = measurer.WrapText(text, style, 200f);

        Assert.Equal(lines1, lines2);
        Assert.Equal(lines1, lines3);
    }

    // ── Background pixels ─────────────────────────────────────────────────────

    /// <summary>
    /// All margin / empty areas must be pure white (255, 255, 255).
    /// A surface not cleared to white, or one that inherits a dirty buffer, would
    /// produce non-white background pixels and pollute golden comparisons.
    /// </summary>
    [Fact]
    public void GenerateImages_EmptyPageMargins_AreExactlyPureWhite()
    {
        // Render a page with large margins and no content so the corners are guaranteed empty.
        var png = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(200); // very large margin → corners are pure background
                page.Content().Text(".").FontFamily("DejaVu Sans"); // minimal content
            });
        }).GenerateImages()[0];

        using var bmp = SKBitmap.Decode(png)!;

        // Sample four corner regions (20×20 each) — guaranteed to be empty margin.
        var corners = new (int X, int Y)[]
        {
            (0, 0),
            (bmp.Width - 20, 0),
            (0, bmp.Height - 20),
            (bmp.Width - 20, bmp.Height - 20),
        };

        foreach (var (cx, cy) in corners)
        {
            for (int dy = 0; dy < 20; dy++)
            for (int dx = 0; dx < 20; dx++)
            {
                var p = bmp.GetPixel(cx + dx, cy + dy);
                Assert.True(
                    p.Red == 255 && p.Green == 255 && p.Blue == 255 && p.Alpha == 255,
                    $"Non-white pixel at ({cx + dx},{cy + dy}): RGBA=({p.Red},{p.Green},{p.Blue},{p.Alpha})");
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[] RenderSingleTextPage(string text, float scale = 1f, bool bold = false)
        => Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginAll(40);
                var t = page.Content().Text(text).FontFamily("DejaVu Sans");
                if (bold) t.Bold();
            });
        }).GenerateImages(scale)[0];

    /// <summary>
    /// Counts pixels where R ≠ G or G ≠ B, which indicates LCD sub-pixel AA fringes.
    /// Fully transparent pixels are excluded because they don't affect composited colour.
    /// </summary>
    private static int CountColoredAaFringePixels(byte[] png)
    {
        using var bmp = SKBitmap.Decode(png)!;
        int count = 0;
        for (int y = 0; y < bmp.Height; y++)
        for (int x = 0; x < bmp.Width; x++)
        {
            var p = bmp.GetPixel(x, y);
            if (p.Alpha == 0) continue;
            if (p.Red != p.Green || p.Green != p.Blue)
                count++;
        }
        return count;
    }
}
