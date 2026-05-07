using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Elements;
using SkiaSharp;

namespace FluentReport.Rendering;

public class DocumentRenderer : IPageImagesProvider
{
    private readonly DocumentSettings _settings;
    private readonly SkiaTextMeasurer _measurer = new();

    public DocumentRenderer(DocumentSettings settings) => _settings = settings;

    /// <summary>
    /// Renders a single logical page (0-based index) to an <see cref="SKImage"/>.
    /// The caller is responsible for disposing the returned image.
    /// </summary>
    public SKImage RenderPageToImage(int pageIndex, float scale = 1f)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be greater than zero.");

        int total = CountTotalPages();

        if (pageIndex < 0 || pageIndex >= total)
            throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index {pageIndex} is out of range (0-{total - 1}).");

        int visitedPageCount = 0;
        int currentPageNumber = 0;

        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight, _measurer);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight, _measurer);
            var contentAreaHeight = contentHeight - headerHeight - footerHeight;

            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight, _measurer);

            if (pages.Count == 0) pages.Add(new List<(IElement, Size)>());

            foreach (var pageContent in pages)
            {
                currentPageNumber++;
                if (visitedPageCount == pageIndex)
                    return RenderPageToImageCore(pageSettings, pageContent, currentPageNumber, total, scale, this);
                visitedPageCount++;
            }
        }

        throw new InvalidOperationException($"Page index {pageIndex} could not be located.");
    }

    /// <summary>Returns the total number of logical pages that would be rendered.</summary>
    public int GetPageCount() => CountTotalPages();

    /// <summary>
    /// Renders all logical pages to PNG byte arrays in a single pagination pass.
    /// </summary>
    internal IReadOnlyList<byte[]> RenderAllPages(float scale)
    {
        int total = CountTotalPages();
        var result = new List<byte[]>();
        int currentPage = 0;

        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight, _measurer);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight, _measurer);
            var contentAreaHeight = contentHeight - headerHeight - footerHeight;

            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight, _measurer);

            if (pages.Count == 0) pages.Add(new List<(IElement, Size)>());

            foreach (var pageContent in pages)
            {
                currentPage++;
                using var image = RenderPageToImageCore(pageSettings, pageContent, currentPage, total, scale, this);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                result.Add(data.ToArray());
            }
        }

        return result;
    }

    private static SKImage RenderPageToImageCore(
        PageSettings pageSettings,
        List<(IElement, Size)> pageContent,
        int logicalPageNumber,
        int totalPages,
        float scale,
        IPageImagesProvider pageImagesProvider)
    {
        int width = (int)Math.Ceiling(pageSettings.Size.Width * scale);
        int height = (int)Math.Ceiling(pageSettings.Size.Height * scale);

        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo)
            ?? throw new InvalidOperationException(
                $"Failed to create SKSurface ({width}x{height}). The requested dimensions may be too large or insufficient memory is available.");
        var skCanvas = surface.Canvas;
        skCanvas.Clear(SKColors.White);

        if (scale != 1f)
            skCanvas.Scale(scale, scale);

        var contentHeight = pageSettings.ContentHeight;
        var canvas = new SkiaDrawingCanvas(skCanvas);
        var renderCtx = new RenderContext
        {
            Canvas = canvas,
            AvailableWidth = pageSettings.ContentWidth,
            AvailableHeight = contentHeight,
            CurrentPage = logicalPageNumber,
            TotalPages = totalPages,
            PageImagesProvider = pageImagesProvider
        };

        float headerHeight = MeasureElement(pageSettings.HeaderElement, pageSettings.ContentWidth, contentHeight, canvas);
        float footerHeight = MeasureElement(pageSettings.FooterElement, pageSettings.ContentWidth, contentHeight, canvas);
        float y = pageSettings.MarginTop;

        if (pageSettings.HeaderElement != null)
        {
            var hs = pageSettings.HeaderElement.Measure(renderCtx.MeasureContextFor(pageSettings.ContentWidth, contentHeight));
            pageSettings.HeaderElement.Render(renderCtx, new Position(pageSettings.MarginLeft, y), new Size(pageSettings.ContentWidth, hs.Height));
            y += hs.Height;
        }

        foreach (var (element, size) in pageContent)
        {
            element.Render(renderCtx, new Position(pageSettings.MarginLeft, y), size);
            y += size.Height;
        }

        if (pageSettings.FooterElement != null)
        {
            var footerY = pageSettings.Size.Height - pageSettings.MarginBottom - footerHeight;
            pageSettings.FooterElement.Render(renderCtx, new Position(pageSettings.MarginLeft, footerY), new Size(pageSettings.ContentWidth, footerHeight));
        }

        return surface.Snapshot();
    }

    public void RenderToStream(Stream stream)
    {
        int totalPages = CountTotalPages();

        using var document = SKDocument.CreatePdf(stream);
        int currentPage = 0;

        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight, _measurer);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight, _measurer);

            var contentAreaHeight = contentHeight - headerHeight - footerHeight;
            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight, _measurer);

            if (pages.Count == 0) pages.Add(new List<(IElement, Size)>());

            foreach (var pageContent in pages)
            {
                currentPage++;

                using var skCanvas = document.BeginPage(pageSettings.Size.Width, pageSettings.Size.Height);
                var canvas = new SkiaDrawingCanvas(skCanvas);

                var renderCtx = new RenderContext
                {
                    Canvas = canvas,
                    AvailableWidth = contentWidth,
                    AvailableHeight = contentHeight,
                    CurrentPage = currentPage,
                    TotalPages = totalPages,
                    PageImagesProvider = this
                };

                float y = pageSettings.MarginTop;

                if (pageSettings.HeaderElement != null)
                {
                    var hs = pageSettings.HeaderElement.Measure(renderCtx.MeasureContextFor(contentWidth, contentHeight));
                    pageSettings.HeaderElement.Render(renderCtx, new Position(pageSettings.MarginLeft, y), new Size(contentWidth, hs.Height));
                    y += hs.Height;
                }

                foreach (var (element, size) in pageContent)
                {
                    element.Render(renderCtx, new Position(pageSettings.MarginLeft, y), size);
                    y += size.Height;
                }

                if (pageSettings.FooterElement != null)
                {
                    var footerY = pageSettings.Size.Height - pageSettings.MarginBottom - footerHeight;
                    pageSettings.FooterElement.Render(renderCtx, new Position(pageSettings.MarginLeft, footerY), new Size(contentWidth, footerHeight));
                }

                document.EndPage();
            }
        }

        document.Close();
    }

    // ── IPageImagesProvider ──────────────────────────────────────────────────

    IReadOnlyList<byte[]> IPageImagesProvider.GetPageImages(DocumentSettings settings, float scale)
        => new DocumentRenderer(settings).RenderAllPages(scale);

    // ── Private helpers ──────────────────────────────────────────────────────

    private static float MeasureElement(IElement? element, float width, float height, ITextMeasurer measurer)
    {
        if (element == null) return 0;
        return element.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = height, Measurer = measurer }).Height;
    }

    private int CountTotalPages()
    {
        int total = 0;
        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight, _measurer);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight, _measurer);
            var contentAreaHeight = contentHeight - headerHeight - footerHeight;

            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight, _measurer);
            total += Math.Max(1, pages.Count);
        }
        return Math.Max(1, total);
    }

    private static List<IElement> GetContentElements(IElement? content)
    {
        if (content == null) return new();
        var resolved = content is LazyElement lazy ? lazy.Built : content;
        if (resolved is ColumnElement column)
        {
            if (column.Spacing <= 0) return column.Items.ToList();
            var items = new List<IElement>();
            bool first = true;
            foreach (var item in column.Items)
            {
                if (!first) items.Add(new SpacerElement(column.Spacing));
                first = false;
                items.Add(item);
            }
            return items;
        }
        return new List<IElement> { content };
    }

    private static List<List<(IElement, Size)>> SplitIntoPages(List<IElement> elements, float width, float pageHeight, ITextMeasurer measurer)
    {
        var pages = new List<List<(IElement, Size)>>();
        var currentPage = new List<(IElement, Size)>();
        float usedHeight = 0;

        foreach (var element in elements)
        {
            var resolved = element is LazyElement lazy ? lazy.Built : element;
            if (resolved is PageBreakElement)
            {
                if (currentPage.Count > 0)
                {
                    pages.Add(currentPage);
                    currentPage = new();
                    usedHeight = 0;
                }
                continue;
            }

            var size = element.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = pageHeight, Measurer = measurer });

            if (usedHeight + size.Height > pageHeight && currentPage.Count > 0)
            {
                pages.Add(currentPage);
                currentPage = new();
                usedHeight = 0;
            }

            currentPage.Add((element, size));
            usedHeight += size.Height;
        }

        if (currentPage.Count > 0 || pages.Count == 0) pages.Add(currentPage);

        return pages;
    }
}
