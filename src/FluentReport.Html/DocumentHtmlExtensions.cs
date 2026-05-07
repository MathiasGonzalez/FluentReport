namespace FluentReport.Html;

/// <summary>
/// Extension methods that add HTML generation capabilities to <see cref="Document"/>.
/// </summary>
public static class DocumentHtmlExtensions
{
    /// <summary>
    /// Generates a full HTML document (<c>&lt;!DOCTYPE html&gt;…&lt;/html&gt;</c>) and
    /// saves it to the given file path.
    /// </summary>
    public static void GenerateHtml(this Document document, string filePath, HtmlRendererOptions? options = null)
    {
        File.WriteAllText(filePath, document.GenerateHtml(options), System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Generates a full HTML document (<c>&lt;!DOCTYPE html&gt;…&lt;/html&gt;</c>) and
    /// writes it to the given stream as UTF-8.
    /// </summary>
    public static void GenerateHtml(this Document document, Stream stream, HtmlRendererOptions? options = null)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(document.GenerateHtml(options));
    }

    /// <summary>
    /// Generates a full HTML document (<c>&lt;!DOCTYPE html&gt;…&lt;/html&gt;</c>) and
    /// returns it as a string.
    /// </summary>
    public static string GenerateHtml(this Document document, HtmlRendererOptions? options = null)
    {
        var renderer = new HtmlDocumentRenderer(document.Settings, options);
        return renderer.RenderFullDocument();
    }

    /// <summary>
    /// Generates an HTML fragment — the outer wrapper table without
    /// <c>&lt;html&gt;</c>, <c>&lt;head&gt;</c> or <c>&lt;body&gt;</c> tags — and
    /// returns it as a string. Suitable for embedding directly in an email body.
    /// </summary>
    public static string GenerateHtmlFragment(this Document document, HtmlRendererOptions? options = null)
    {
        var renderer = new HtmlDocumentRenderer(document.Settings, options);
        return renderer.RenderFragment();
    }
}
