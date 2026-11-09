using FluentReport.Elements;

namespace FluentReport.Core;

/// <summary>One page definition: size, margins and the header, content and footer elements. All measures are in points (1/72 inch).</summary>
public class PageSettings
{
    /// <summary>Page size. Default A4. With <see cref="ContinuousHeight"/> only the width is used.</summary>
    public PageSize Size { get; set; } = PageSize.A4;
    /// <summary>
    /// When set, the page grows to the height of its content (margins, header and footer included)
    /// instead of breaking at <see cref="PageSize.Height"/>: one page, as long as needed. Used for
    /// receipts on roll paper. Only the PDF and PNG output paginate, so only they read it.
    /// </summary>
    public bool ContinuousHeight { get; set; }

    /// <summary>Top margin. Default 40.</summary>
    public float MarginTop { get; set; } = 40;
    /// <summary>Bottom margin. Default 40.</summary>
    public float MarginBottom { get; set; } = 40;
    /// <summary>Left margin. Default 40.</summary>
    public float MarginLeft { get; set; } = 40;
    /// <summary>Right margin. Default 40.</summary>
    public float MarginRight { get; set; } = 40;

    /// <summary>Repeated at the top of every page.</summary>
    public IElement? HeaderElement { get; set; }
    /// <summary>The body, which flows across as many pages as it needs.</summary>
    public IElement? ContentElement { get; set; }
    /// <summary>Repeated at the bottom of every page.</summary>
    public IElement? FooterElement { get; set; }

    /// <summary>Width between the left and right margins.</summary>
    public float ContentWidth => Size.Width - MarginLeft - MarginRight;
    /// <summary>Height between the top and bottom margins, header and footer included.</summary>
    public float ContentHeight => Size.Height - MarginTop - MarginBottom;
}
