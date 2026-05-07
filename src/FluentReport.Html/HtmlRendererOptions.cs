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
}
