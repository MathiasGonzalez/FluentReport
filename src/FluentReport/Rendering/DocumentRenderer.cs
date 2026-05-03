using FluentReport.Builders;
using FluentReport.Core;
using FluentReport.Elements;
using SkiaSharp;

namespace FluentReport.Rendering;

public class DocumentRenderer
{
    private readonly DocumentSettings _settings;

    public DocumentRenderer(DocumentSettings settings) => _settings = settings;

    public void RenderToStream(Stream stream)
    {
        int totalPages = CountTotalPages();

        using var document = SKDocument.CreatePdf(stream);
        int currentPage = 0;

        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight);

            var contentAreaHeight = contentHeight - headerHeight - footerHeight;
            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight);

            if (pages.Count == 0) pages.Add(new List<(IElement, Size)>());

            foreach (var pageContent in pages)
            {
                currentPage++;

                using var canvas = document.BeginPage(pageSettings.Size.Width, pageSettings.Size.Height);

                var renderCtx = new RenderContext
                {
                    Canvas = canvas,
                    AvailableWidth = contentWidth,
                    AvailableHeight = contentHeight,
                    CurrentPage = currentPage,
                    TotalPages = totalPages
                };

                float y = pageSettings.MarginTop;

                if (pageSettings.HeaderElement != null)
                {
                    var hs = pageSettings.HeaderElement.Measure(new MeasureContext { AvailableWidth = contentWidth, AvailableHeight = contentHeight });
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

    private static float MeasureElement(IElement? element, float width, float height)
    {
        if (element == null) return 0;
        return element.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = height }).Height;
    }

    private int CountTotalPages()
    {
        int total = 0;
        foreach (var pageSettings in _settings.Pages)
        {
            var contentWidth = pageSettings.ContentWidth;
            var contentHeight = pageSettings.ContentHeight;

            float headerHeight = MeasureElement(pageSettings.HeaderElement, contentWidth, contentHeight);
            float footerHeight = MeasureElement(pageSettings.FooterElement, contentWidth, contentHeight);
            var contentAreaHeight = contentHeight - headerHeight - footerHeight;

            var contentElements = GetContentElements(pageSettings.ContentElement);
            var pages = SplitIntoPages(contentElements, contentWidth, contentAreaHeight);
            total += Math.Max(1, pages.Count);
        }
        return Math.Max(1, total);
    }

    private static List<IElement> GetContentElements(IElement? content)
    {
        if (content == null) return new();
        var resolved = content is LazyElement lazy ? lazy.Built : content;
        if (resolved is ColumnElement column) return column.Items.ToList();
        return new List<IElement> { content };
    }

    private static List<List<(IElement, Size)>> SplitIntoPages(List<IElement> elements, float width, float pageHeight)
    {
        var pages = new List<List<(IElement, Size)>>();
        var currentPage = new List<(IElement, Size)>();
        float usedHeight = 0;

        foreach (var element in elements)
        {
            var resolved = element is LazyElement lazy ? lazy.Built : element;
            if (resolved is PageBreakElement)
            {
                pages.Add(currentPage);
                currentPage = new();
                usedHeight = 0;
                continue;
            }

            var size = element.Measure(new MeasureContext { AvailableWidth = width, AvailableHeight = pageHeight });

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
