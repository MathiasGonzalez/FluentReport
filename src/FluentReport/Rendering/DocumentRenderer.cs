using FluentReport.Core;
using FluentReport.Elements;
using SkiaSharp;

namespace FluentReport.Rendering;

/// <summary>One output page: the page settings it uses and the content fragment that fits on it.</summary>
internal sealed record PageLayout(PageSettings Settings, IElement? Content, float HeaderHeight, float FooterHeight);

/// <summary>Paginates a document once and draws the pages to PDF or PNG.</summary>
internal sealed class DocumentRenderer : IDisposable
{
    private readonly DocumentSettings _settings;
    private readonly PdfOptions? _options;
    private readonly LayoutEngine _engine;
    private List<PageLayout>? _pages;

    public DocumentRenderer(DocumentSettings settings, PdfOptions? options = null)
    {
        _settings = settings;
        _options = options;
        _engine = new LayoutEngine(options);
    }

    public IReadOnlyList<PageLayout> Pages => _pages ??= Paginate();

    private List<PageLayout> Paginate()
    {
        var pages = new List<PageLayout>();
        foreach (var page in _settings.Pages)
        {
            float width = page.ContentWidth;
            float header = page.HeaderElement is { } h ? _engine.Measure(h, width) : 0;
            float footer = page.FooterElement is { } f ? _engine.Measure(f, width) : 0;
            float area = Math.Max(0, page.ContentHeight - header - footer);

            var remaining = page.ContentElement;
            if (remaining == null)
            {
                pages.Add(new PageLayout(page, null, header, footer));
                continue;
            }

            while (remaining != null)
            {
                var (head, tail) = _engine.Split(remaining, width, area, pageIsEmpty: true);
                pages.Add(new PageLayout(page, head, header, footer));
                remaining = IsEmpty(tail) ? null : tail;
            }
        }
        _engine.TotalPages = pages.Count;
        return pages;
    }

    /// <summary>A column left over by a trailing page break carries nothing to draw.</summary>
    private static bool IsEmpty(IElement? element) => element switch
    {
        null => true,
        ColumnElement column => column.Items.All(i => i is PageBreakElement || IsEmpty(i) && i is ColumnElement),
        _ => false
    };

    private void DrawPage(SKCanvas canvas, int index)
    {
        var page = Pages[index];
        var s = page.Settings;
        _engine.CurrentPage = index + 1;
        _engine.TotalPages = Pages.Count;

        float width = s.ContentWidth;
        float top = s.MarginTop;

        if (s.HeaderElement is { } header)
            _engine.Draw(header, canvas, s.MarginLeft, top, width, page.HeaderHeight);

        if (page.Content is { } content)
        {
            float contentTop = top + page.HeaderHeight;
            float area = Math.Max(0, s.ContentHeight - page.HeaderHeight - page.FooterHeight);
            canvas.Save();
            // Content taller than an empty page (an oversized row or image) is cut at the footer.
            canvas.ClipRect(SKRect.Create(s.MarginLeft, contentTop, width, area));
            _engine.Draw(content, canvas, s.MarginLeft, contentTop, width, _engine.Measure(content, width));
            canvas.Restore();
        }

        if (s.FooterElement is { } footer)
            _engine.Draw(footer, canvas, s.MarginLeft, s.Size.Height - s.MarginBottom - page.FooterHeight, width, page.FooterHeight);
    }

    public void RenderPdf(Stream stream)
    {
        var metadata = SKDocumentPdfMetadata.Default;
        metadata.Title = _options?.Title;
        metadata.Author = _options?.Author;
        metadata.Producer = "FluentReport";

        using var document = SKDocument.CreatePdf(stream, metadata)
            ?? throw new InvalidOperationException("SkiaSharp could not create a PDF document.");
        for (int i = 0; i < Pages.Count; i++)
        {
            var size = Pages[i].Settings.Size;
            var canvas = document.BeginPage(size.Width, size.Height);
            DrawPage(canvas, i);
            document.EndPage();
        }
        document.Close();
    }

    public IReadOnlyList<byte[]> RenderPng(float scale)
    {
        var result = new List<byte[]>(Pages.Count);
        for (int i = 0; i < Pages.Count; i++)
        {
            var size = Pages[i].Settings.Size;
            var info = new SKImageInfo((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale),
                SKColorType.Rgba8888, SKAlphaType.Premul);
            // Unknown pixel geometry disables LCD anti-aliasing, which depends on the display and the OS.
            var props = new SKSurfaceProperties(SKSurfacePropsFlags.None, SKPixelGeometry.Unknown);
            using var surface = SKSurface.Create(info, props)
                ?? throw new InvalidOperationException($"Could not create a {info.Width}x{info.Height} surface.");
            surface.Canvas.Clear(SKColors.White);
            surface.Canvas.Scale(scale);
            DrawPage(surface.Canvas, i);
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            result.Add(data.ToArray());
        }
        return result;
    }

    public void Dispose() => _engine.Dispose();
}
