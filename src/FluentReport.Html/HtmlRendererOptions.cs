namespace FluentReport.Html;

/// <summary>
/// Options for controlling how <see cref="HtmlDocumentRenderer"/> generates HTML output.
/// </summary>
public class HtmlRendererOptions
{
    /// <summary>
    /// Maximum width (in pixels) of the outer email wrapper table.
    /// Set to <c>null</c> for unconstrained width (100 %).
    /// Default is <c>600</c>.
    /// </summary>
    public int? MaxWidth { get; set; } = 600;

    /// <summary>
    /// CSS font-family stack applied to the body wrapper and used as fallback
    /// when an element does not specify its own font family.
    /// Default is <c>"Arial, Helvetica, sans-serif"</c>.
    /// </summary>
    public string FontFamily { get; set; } = "Arial, Helvetica, sans-serif";

    /// <summary>
    /// Inline CSS applied to the spacer cell that visually separates pages in
    /// a multi-page document.
    /// Default is a dashed top border in a neutral gray.
    /// </summary>
    public string PageDividerStyle { get; set; } =
        "border-top: 2px dashed #cccccc; padding-top: 16px; padding-bottom: 16px";

    /// <summary>
    /// When <c>true</c>, enables Outlook desktop (2016/2019/365) compatibility tweaks:
    /// <list type="bullet">
    ///   <item><description>Adds <c>role="presentation"</c> to all layout tables.</description></item>
    ///   <item><description>Adds a <c>bgcolor</c> attribute as a fallback for <c>background-color</c> CSS on the outer wrapper.</description></item>
    ///   <item><description>Injects an <c>&lt;o:OfficeDocumentSettings&gt;</c> block in the <c>&lt;head&gt;</c> (full document only) to fix Outlook DPI scaling.</description></item>
    /// </list>
    /// Default is <c>false</c>.
    /// </summary>
    public bool OutlookCompatible { get; set; } = false;
}
