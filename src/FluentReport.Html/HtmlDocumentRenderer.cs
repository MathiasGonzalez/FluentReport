using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;
using System.Globalization;
using System.Net;
using System.Text;

namespace FluentReport.Html;

/// <summary>
/// Renders a FluentReport document as email-safe HTML using table-based layout and
/// fully inline CSS. Each document page is rendered sequentially into a single HTML
/// document, separated by a configurable visual divider.
/// </summary>
public class HtmlDocumentRenderer
{
    private readonly DocumentSettings _settings;
    private readonly HtmlRendererOptions _options;

    public HtmlDocumentRenderer(DocumentSettings settings, HtmlRendererOptions? options = null)
    {
        _settings = settings;
        _options = options ?? new HtmlRendererOptions();
    }

    /// <summary>
    /// Returns <c>role="presentation" </c> (with trailing space) when <see cref="HtmlRendererOptions.OutlookCompatible"/> is enabled,
    /// otherwise an empty string. Apply to every layout <c>&lt;table&gt;</c>.
    /// </summary>
    /// <summary>1-based number of the page being rendered; each document page is one HTML page.</summary>
    private int _currentPage = 1;

    private string RoleAttr => _options.OutlookCompatible ? "role=\"presentation\" " : "";

    // ── Public render methods ─────────────────────────────────────────────────

    /// <summary>
    /// Returns a complete HTML document (<c>&lt;!DOCTYPE html&gt;…&lt;/html&gt;</c>)
    /// containing all pages of the report.
    /// </summary>
    public string RenderFullDocument()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Document</title>");
        if (_options.OutlookCompatible)
        {
            sb.AppendLine("  <!--[if mso]><xml><o:OfficeDocumentSettings xmlns:o=\"urn:schemas-microsoft-com:office:office\"><o:PixelsPerInch>96</o:PixelsPerInch><o:AllowPNG/></o:OfficeDocumentSettings></xml><![endif]-->");
        }
        sb.AppendLine("</head>");
        sb.AppendLine($"<body style=\"margin: 0; padding: 20px 0; background-color: #f5f5f5; font-family: {SanitizeFontFamily(_options.FontFamily)};\">");
        sb.AppendLine(RenderFragment());
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    /// <summary>
    /// Returns an HTML fragment — the outer wrapper table containing all pages —
    /// without <c>&lt;html&gt;</c>, <c>&lt;head&gt;</c> or <c>&lt;body&gt;</c> tags.
    /// Suitable for embedding directly in an email body.
    /// </summary>
    public string RenderFragment()
    {
        var sb = new StringBuilder();

        string widthAttr = _options.MaxWidth.HasValue
            ? $" width=\"{_options.MaxWidth}\""
            : " width=\"100%\"";
        string maxWidthStyle = _options.MaxWidth.HasValue
            ? $" max-width: {_options.MaxWidth}px;"
            : "";
        string bgcolorAttr = _options.OutlookCompatible ? " bgcolor=\"#ffffff\"" : "";

        sb.AppendLine($"<table {RoleAttr}{widthAttr}{bgcolorAttr} cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background-color: #ffffff;{maxWidthStyle} margin: 0 auto; border-collapse: collapse;\">");

        bool first = true;
        _currentPage = 0;
        foreach (var page in _settings.Pages)
        {
            _currentPage++;
            if (!first)
            {
                sb.AppendLine("  <tr>");
                sb.AppendLine($"    <td style=\"{_options.PageDividerStyle}; font-size: 0; line-height: 0;\">&nbsp;</td>");
                sb.AppendLine("  </tr>");
            }
            first = false;
            RenderPage(sb, page);
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ── Page ──────────────────────────────────────────────────────────────────

    private void RenderPage(StringBuilder sb, PageSettings page)
    {
        if (page.HeaderElement != null)
        {
            sb.AppendLine("  <tr>");
            sb.AppendLine($"    <td style=\"padding: {N(page.MarginTop)}px {N(page.MarginRight)}px 0 {N(page.MarginLeft)}px;\">");
            sb.AppendLine(RenderElement(page.HeaderElement));
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
        }

        if (page.ContentElement != null)
        {
            float topPad = page.HeaderElement != null ? 10f : page.MarginTop;
            float botPad = page.FooterElement != null ? 10f : page.MarginBottom;
            sb.AppendLine("  <tr>");
            sb.AppendLine($"    <td style=\"padding: {N(topPad)}px {N(page.MarginRight)}px {N(botPad)}px {N(page.MarginLeft)}px;\">");
            sb.AppendLine(RenderElement(page.ContentElement));
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
        }

        if (page.FooterElement != null)
        {
            sb.AppendLine("  <tr>");
            sb.AppendLine($"    <td style=\"padding: 0 {N(page.MarginRight)}px {N(page.MarginBottom)}px {N(page.MarginLeft)}px;\">");
            sb.AppendLine(RenderElement(page.FooterElement));
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
        }
    }

    // ── Element dispatcher ────────────────────────────────────────────────────

    private string RenderElement(IElement element)
    {
        return element switch
        {
            ColumnElement col     => RenderColumn(col),
            RowElement row        => RenderRow(row),
            TableElement table    => RenderTable(table),
            TextElement text      => RenderText(text),
            BorderElement border  => RenderBorder(border),
            PaddingElement pad    => RenderPadding(pad),
            AlignElement align    => RenderAlign(align),
            LineElement line      => RenderLine(line),
            SpacerElement spacer  => RenderSpacer(spacer),
            ImageElement image    => RenderImage(image),
            PageBreakElement      => RenderPageBreak(),
            _ => throw new NotSupportedException($"The HTML renderer does not support {element.GetType().Name}.")
        };
    }

    // ── Column ────────────────────────────────────────────────────────────────

    private string RenderColumn(ColumnElement col)
    {
        if (col.Items.Count == 0) return "";
        var sb = new StringBuilder();
        sb.AppendLine($"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse: collapse;\">");
        bool first = true;
        foreach (var item in col.Items)
        {
            if (!first && col.Spacing > 0)
                sb.AppendLine($"<tr><td style=\"height: {(int)col.Spacing}px; font-size: 0; line-height: 0; padding: 0;\">&nbsp;</td></tr>");
            first = false;
            sb.AppendLine("<tr><td style=\"padding: 0;\">");
            sb.AppendLine(RenderElement(item));
            sb.AppendLine("</td></tr>");
        }
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ── Row ───────────────────────────────────────────────────────────────────

    private string RenderRow(RowElement row)
    {
        if (row.Items.Count == 0) return "";

        float relativeTotal = row.Items.Where(i => i.IsRelative).Sum(i => i.RelativeWidth);

        var sb = new StringBuilder();
        sb.AppendLine($"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border-collapse: collapse;\">");
        sb.AppendLine("<tr>");

        for (int i = 0; i < row.Items.Count; i++)
        {
            var item = row.Items[i];
            string widthAttr;
            string widthStyle;

            if (!item.IsRelative)
            {
                int px = (int)(item.FixedWidth ?? 0f);
                widthAttr = $" width=\"{px}\"";
                widthStyle = $"width: {px}px;";
            }
            else
            {
                float pct = relativeTotal > 0 ? (item.RelativeWidth / relativeTotal) * 100f : 100f;
                widthAttr = $" width=\"{N(pct, "F0")}%\"";
                widthStyle = $"width: {N(pct, "F0")}%;";
            }

            string spacingStyle = (row.Spacing > 0 && i < row.Items.Count - 1)
                ? $" padding-right: {(int)row.Spacing}px;"
                : "";

            sb.AppendLine($"<td{widthAttr} valign=\"top\" style=\"padding: 0; {widthStyle}{spacingStyle}\">");
            if (item.Element != null)
                sb.AppendLine(RenderElement(item.Element));
            sb.AppendLine("</td>");
        }

        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    private string RenderTable(TableElement table)
    {
        if (table.Columns.Count == 0) return "";

        string borderAttr = table.BorderWidth > 0
            ? $" border=\"{(int)Math.Ceiling(table.BorderWidth)}\""
            : "";

        var sb = new StringBuilder();
        // Data table: intentionally no role="presentation" as it carries semantic meaning.
        sb.AppendLine($"<table width=\"100%\"{borderAttr} cellpadding=\"4\" cellspacing=\"0\" style=\"border-collapse: collapse; width: 100%;\">");
        sb.AppendLine(RenderColumnGroup(table));

        RenderTableSection(sb, table, table.HeaderCells, "thead", "th");
        RenderTableSection(sb, table, table.DataCells, "tbody", "td");
        RenderTableSection(sb, table, table.FooterCells, "tfoot", "td");

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Fixed columns keep their width in px; relative columns share the rest proportionally
    /// (as percentages when the table has no fixed columns, otherwise left to the browser).
    /// </summary>
    private static string RenderColumnGroup(TableElement table)
    {
        bool anyFixed = table.Columns.Any(c => !c.IsRelative);
        float relativeTotal = table.Columns.Where(c => c.IsRelative).Sum(c => c.RelativeWidth);

        var sb = new StringBuilder("<colgroup>");
        foreach (var c in table.Columns)
        {
            if (!c.IsRelative)
                sb.Append($"<col width=\"{N(c.FixedWidth!.Value, "0")}\" style=\"width: {N(c.FixedWidth.Value)}px;\">");
            else if (!anyFixed && relativeTotal > 0)
                sb.Append($"<col width=\"{N(c.RelativeWidth / relativeTotal * 100f, "F1")}%\" style=\"width: {N(c.RelativeWidth / relativeTotal * 100f, "F1")}%;\">");
            else
                sb.Append("<col>");
        }
        sb.Append("</colgroup>");
        return sb.ToString();
    }

    private void RenderTableSection(StringBuilder sb, TableElement table, List<TableCell> cells, string section, string tag)
    {
        if (cells.Count == 0) return;

        sb.AppendLine($"<{section}>");
        foreach (var row in table.PartitionIntoRows(cells))
        {
            sb.AppendLine("<tr>");
            foreach (var (cell, _, span) in row)
            {
                string colspanAttr = span > 1 ? $" colspan=\"{span}\"" : "";

                var style = new StringBuilder("vertical-align: top; padding: 4px;");
                if (tag == "th") style.Append(" font-weight: bold;");
                if (table.BorderWidth > 0)
                    style.Append($" border: {N(table.BorderWidth)}px solid {ColorToHex(table.BorderColor)};");

                var bg = ExtractBackgroundColor(cell.Content);
                if (bg.HasValue) style.Append($" background-color: {ColorToHex(bg.Value)};");

                var align = ExtractCellAlignment(cell.Content);
                if (align != null) style.Append($" text-align: {align};");

                sb.AppendLine($"<{tag}{colspanAttr} style=\"{style}\">");
                if (cell.Content != null)
                    sb.AppendLine(RenderCellContent(cell.Content));
                sb.AppendLine($"</{tag}>");
            }
            sb.AppendLine("</tr>");
        }
        sb.AppendLine($"</{section}>");
    }

    /// <summary>Renders cell content preferring inline text over block-level wrappers.</summary>
    private string RenderCellContent(IElement element)
    {
        return element switch
        {
            TextElement text          => RenderTextInline(text),
            PaddingElement pad        => pad.Child != null ? RenderCellContent(pad.Child) : "",
            BorderElement border      => border.Child != null ? RenderCellContent(border.Child) : "",
            AlignElement align        => align.Child != null ? RenderCellContent(align.Child) : "",
            _                        => RenderElement(element)
        };
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    private string RenderText(TextElement text)
    {
        if (text.Spans.Count == 0) return "";

        if (text.Spans.Count == 1)
        {
            var span = text.Spans[0];
            return $"<p style=\"margin: 0; padding: 0; {BuildTextStyle(span.Style)}\">{Encode(SpanText(span))}</p>";
        }

        var sb = new StringBuilder();
        sb.Append($"<p style=\"margin: 0; padding: 0; {BuildTextStyle(text.Style)}\">");
        foreach (var span in text.Spans)
        {
            string content = Encode(SpanText(span));
            sb.Append($"<span style=\"{BuildTextStyle(span.Style)}\">{content}</span>");
        }
        sb.Append("</p>");
        return sb.ToString();
    }

    private string RenderTextInline(TextElement text)
    {
        if (text.Spans.Count == 0) return "";

        if (text.Spans.Count == 1)
        {
            var span = text.Spans[0];
            return $"<span style=\"{BuildTextStyle(span.Style)}\">{Encode(SpanText(span))}</span>";
        }

        var sb = new StringBuilder();
        foreach (var span in text.Spans)
        {
            string content = Encode(SpanText(span));
            sb.Append($"<span style=\"{BuildTextStyle(span.Style)}\">{content}</span>");
        }
        return sb.ToString();
    }

    private string SpanText(TextSpan span) =>
        span.IsCurrentPage ? _currentPage.ToString(CultureInfo.InvariantCulture)
        : span.IsTotalPages ? _settings.Pages.Count.ToString(CultureInfo.InvariantCulture)
        : span.StaticText ?? "";

    // ── Decorator elements ────────────────────────────────────────────────────

    private string RenderBorder(BorderElement border)
    {
        var style = new StringBuilder("border-collapse: collapse;");
        if (border.BackgroundColor.HasValue)
            style.Append($" background-color: {ColorToHex(border.BackgroundColor.Value)};");

        if (border.Border.Width > 0)
        {
            var b = border.Border;
            // Build per-side border styles for maximum email client compatibility
            string bVal = $"{N(b.Width)}px solid {ColorToHex(b.Color)}";
            if (b.Sides.HasFlag(BorderSide.Top))    style.Append($" border-top: {bVal};");
            if (b.Sides.HasFlag(BorderSide.Right))  style.Append($" border-right: {bVal};");
            if (b.Sides.HasFlag(BorderSide.Bottom)) style.Append($" border-bottom: {bVal};");
            if (b.Sides.HasFlag(BorderSide.Left))   style.Append($" border-left: {bVal};");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"{style}\">");
        sb.AppendLine("<tr><td style=\"padding: 0;\">");
        if (border.Child != null)
            sb.AppendLine(RenderElement(border.Child));
        sb.AppendLine("</td></tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private string RenderPadding(PaddingElement pad)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">");
        sb.AppendLine($"<tr><td style=\"padding: {N(pad.Top)}px {N(pad.Right)}px {N(pad.Bottom)}px {N(pad.Left)}px;\">");
        if (pad.Child != null)
            sb.AppendLine(RenderElement(pad.Child));
        sb.AppendLine("</td></tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private string RenderAlign(AlignElement align)
    {
        string textAlign = align.Alignment switch
        {
            HorizontalAlignment.Center => "center",
            HorizontalAlignment.Right  => "right",
            _                         => "left"
        };

        var sb = new StringBuilder();
        sb.AppendLine($"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">");
        sb.AppendLine($"<tr><td align=\"{textAlign}\" style=\"text-align: {textAlign}; padding: 0;\">");
        if (align.Child != null)
            sb.AppendLine(RenderElement(align.Child));
        sb.AppendLine("</td></tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ── Visual elements ───────────────────────────────────────────────────────

    private string RenderLine(LineElement line)
    {
        string color = ColorToHex(line.Color);
        return $"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">" +
               $"<tr><td style=\"border-top: {N(line.Thickness)}px solid {color}; height: 0; font-size: 0; line-height: 0; padding: 0;\">&nbsp;</td></tr>" +
               "</table>";
    }

    private string RenderSpacer(SpacerElement spacer)
    {
        float h = spacer.Height;
        return $"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">" +
               $"<tr><td style=\"height: {(int)h}px; font-size: 0; line-height: 0; padding: 0;\">&nbsp;</td></tr>" +
               "</table>";
    }

    private static string RenderImage(ImageElement image)
    {
        if (image.MimeType is not { } mime)
            return "<!-- image not available -->";

        string base64 = Convert.ToBase64String(image.SourceBytes);

        var style = new StringBuilder("display: block; max-width: 100%; height: auto;");
        if (image.FixedWidth.HasValue)  style.Append($" width: {(int)image.FixedWidth.Value}px;");
        if (image.FixedHeight.HasValue) style.Append($" height: {(int)image.FixedHeight.Value}px;");

        return $"<img src=\"data:{mime};base64,{base64}\" alt=\"\" style=\"{style}\">";
    }

    private string RenderPageBreak() =>
        $"<table {RoleAttr}width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse: collapse;\">" +
        $"<tr><td style=\"{_options.PageDividerStyle}; font-size: 0; line-height: 0;\">&nbsp;</td></tr>" +
        "</table>";

    // ── Style helpers ─────────────────────────────────────────────────────────

    private string BuildTextStyle(TextStyle style)
    {
        string family = !string.IsNullOrEmpty(style.FontFamily) && style.FontFamily != "sans-serif"
            ? style.FontFamily
            : _options.FontFamily;

        var sb = new StringBuilder();
        sb.Append($"font-family: {SanitizeFontFamily(family)}; ");
        sb.Append($"font-size: {N(style.FontSize)}px; ");
        sb.Append($"color: {ColorToHex(style.EffectiveColor)}; ");
        if (style.EffectiveBold)  sb.Append("font-weight: bold; ");
        if (style.EffectiveItalic) sb.Append("font-style: italic; ");
        if (style.Underline)       sb.Append("text-decoration: underline; ");
        sb.Append($"text-align: {MapTextAlignment(style.Alignment)}; ");
        sb.Append($"line-height: {N(style.LineSpacing)};");
        return sb.ToString();
    }

    private static string MapTextAlignment(TextAlignment a) => a switch
    {
        TextAlignment.Center  => "center",
        TextAlignment.Right   => "right",
        TextAlignment.Justify => "justify",
        _                    => "left"
    };

    /// <summary>
    /// Keeps only characters valid in a CSS font-family list, so a family name can never
    /// close the style attribute or inject another declaration.
    /// </summary>
    private static string SanitizeFontFamily(string family)
    {
        var clean = new string(family.Where(c => char.IsLetterOrDigit(c) || c is ' ' or ',' or '-' or '_' or '\'').ToArray()).Trim();
        return clean.Length > 0 ? clean : "sans-serif";
    }

    /// <summary>Formats a CSS number. CSS always uses '.' as decimal separator, whatever the current culture.</summary>
    private static string N(float value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);

    private static string ColorToHex(ReportColor c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string Encode(string s) => WebUtility.HtmlEncode(s);

    // ── Content extraction helpers ────────────────────────────────────────────

    private static ReportColor? ExtractBackgroundColor(IElement? element)
    {
        if (element == null) return null;
        return element switch
        {
            BorderElement b  => b.BackgroundColor,
            PaddingElement p => ExtractBackgroundColor(p.Child),
            AlignElement a   => ExtractBackgroundColor(a.Child),
            _                => null
        };
    }

    /// <summary>
    /// CSS text-align for a table cell: a container alignment (<c>.AlignRight()</c> on the cell)
    /// wins; otherwise the alignment of a text content. Text inside a cell is rendered inline,
    /// where its own text-align would have no effect.
    /// </summary>
    private static string? ExtractCellAlignment(IElement? element)
    {
        var container = ExtractHorizontalAlignment(element);
        if (container.HasValue) return container.Value.ToString().ToLowerInvariant();

        var text = ExtractText(element);
        return text != null && text.Style.Alignment != TextAlignment.Left
            ? MapTextAlignment(text.Style.Alignment)
            : null;
    }

    private static TextElement? ExtractText(IElement? element) => element switch
    {
        TextElement t    => t,
        PaddingElement p => ExtractText(p.Child),
        BorderElement b  => ExtractText(b.Child),
        AlignElement a   => ExtractText(a.Child),
        _                => null
    };

    private static HorizontalAlignment? ExtractHorizontalAlignment(IElement? element)
    {
        if (element == null) return null;
        return element switch
        {
            AlignElement a   => a.Alignment,
            PaddingElement p => ExtractHorizontalAlignment(p.Child),
            BorderElement b  => ExtractHorizontalAlignment(b.Child),
            _                => null
        };
    }
}
