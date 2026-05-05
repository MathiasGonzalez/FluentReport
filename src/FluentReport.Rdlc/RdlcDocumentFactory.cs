using System.Globalization;
using System.Xml.Linq;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Rdlc;

/// <summary>
/// Parses a <c>.rdlc</c> XML file into a FluentReport <see cref="Document"/>.
/// </summary>
/// <remarks>
/// Supported RDLC elements (Phase 1 MVP):
/// <list type="bullet">
///   <item><c>Textbox</c> → <see cref="TextElement"/></item>
///   <item><c>Line</c> → <see cref="LineElement"/></item>
///   <item><c>Image</c> → <see cref="ImageElement"/></item>
///   <item>
///     <c>Tablix</c> (basic, no row groups) → <see cref="TableElement"/>;
///     detail rows repeat per dataset row.
///   </item>
///   <item>Page dimensions and margins from <c>&lt;Page&gt;</c>.</item>
///   <item><c>PageHeader</c> / <c>PageFooter</c>.</item>
/// </list>
/// Expressions supported: <c>=Fields!X.Value</c>, <c>=Parameters!Y.Value</c>, literals.
/// </remarks>
public sealed class RdlcDocumentFactory
{
    // RDLC XML namespace variants
    private static readonly XNamespace Ns2008 =
        "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

    private static readonly XNamespace Ns2005 =
        "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

    private readonly IDictionary<string, IEnumerable<object>>? _datasets;
    private readonly RdlcExpressionEvaluator _evaluator;
    private XNamespace _ns = XNamespace.None;

    public RdlcDocumentFactory(
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null)
    {
        _datasets = datasets;
        _evaluator = new RdlcExpressionEvaluator(parameters);
    }

    // ── Public entry points ──────────────────────────────────────────────────

    /// <summary>Parses the <c>.rdlc</c> file at <paramref name="path"/> into a <see cref="Document"/>.</summary>
    public Document ParseFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return ParseFromStream(stream);
    }

    /// <summary>Parses RDLC XML from a stream.</summary>
    public Document ParseFromStream(Stream stream)
        => Parse(XDocument.Load(stream));

    /// <summary>Parses RDLC XML from a string.</summary>
    public Document ParseFromXml(string xml)
        => Parse(XDocument.Parse(xml));

    // ── Core parse pipeline ──────────────────────────────────────────────────

    private Document Parse(XDocument xdoc)
    {
        _ns = DetectNamespace(xdoc);
        var root = xdoc.Root ?? throw new InvalidOperationException("RDLC XML has no root element.");

        // Support both SSRS 2008+ (<ReportSections>) and SSRS 2005 (<Body>/<Page> directly).
        var sections = root.Element(_ns + "ReportSections")
            ?.Elements(_ns + "ReportSection")
            .ToList()
            ?? new List<XElement> { root };

        var settings = new DocumentSettings();
        foreach (var section in sections)
            settings.Pages.Add(BuildPageSettings(section, root));

        return Document.FromSettings(settings);
    }

    private static XNamespace DetectNamespace(XDocument xdoc)
    {
        var rootNs = xdoc.Root?.Name.Namespace ?? XNamespace.None;
        return rootNs; // accept any namespace (2005, 2008, or future)
    }

    // ── Page configuration ────────────────────────────────────────────────────

    private PageSettings BuildPageSettings(XElement section, XElement reportRoot)
    {
        var page = new PageSettings();

        // Page settings live in <Page> inside the section, or at report root for 2005 format.
        var pageEl = section.Element(_ns + "Page") ?? reportRoot.Element(_ns + "Page");
        ApplyPageDimensions(page, pageEl);

        // PageHeader / PageFooter
        var headerEl = pageEl?.Element(_ns + "PageHeader") ?? reportRoot.Element(_ns + "PageHeader");
        var footerEl = pageEl?.Element(_ns + "PageFooter") ?? reportRoot.Element(_ns + "PageFooter");

        if (headerEl != null)
        {
            var items = BuildReportItems(headerEl.Element(_ns + "ReportItems"), null);
            if (items.Count > 0)
                page.HeaderElement = BuildColumn(items);
        }

        if (footerEl != null)
        {
            var items = BuildReportItems(footerEl.Element(_ns + "ReportItems"), null);
            if (items.Count > 0)
                page.FooterElement = BuildColumn(items);
        }

        // Body content
        var bodyEl = section.Element(_ns + "Body") ?? reportRoot.Element(_ns + "Body");
        var bodyItems = BuildReportItems(bodyEl?.Element(_ns + "ReportItems"), null);

        page.ContentElement = bodyItems.Count > 0
            ? BuildColumn(bodyItems)
            : new SpacerElement();

        return page;
    }

    private void ApplyPageDimensions(PageSettings page, XElement? pageEl)
    {
        if (pageEl == null) return;

        float pageW = ParseUnit(pageEl.Element(_ns + "PageWidth")?.Value);
        float pageH = ParseUnit(pageEl.Element(_ns + "PageHeight")?.Value);
        if (pageW > 0 && pageH > 0)
            page.Size = new PageSize(pageW, pageH);

        float mt = ParseUnit(pageEl.Element(_ns + "TopMargin")?.Value);
        float mb = ParseUnit(pageEl.Element(_ns + "BottomMargin")?.Value);
        float ml = ParseUnit(pageEl.Element(_ns + "LeftMargin")?.Value);
        float mr = ParseUnit(pageEl.Element(_ns + "RightMargin")?.Value);

        if (mt > 0) page.MarginTop = mt;
        if (mb > 0) page.MarginBottom = mb;
        if (ml > 0) page.MarginLeft = ml;
        if (mr > 0) page.MarginRight = mr;
    }

    // ── ReportItems dispatcher ────────────────────────────────────────────────

    private List<IElement> BuildReportItems(XElement? reportItemsEl, object? dataRow)
    {
        if (reportItemsEl == null) return new List<IElement>();

        // Sort children by Top position so the vertical layout matches the design-time order.
        var children = reportItemsEl.Elements()
            .OrderBy(e => ParseUnit(e.Element(_ns + "Top")?.Value))
            .ThenBy(e => ParseUnit(e.Element(_ns + "Left")?.Value))
            .ToList();

        var elements = new List<IElement>();

        foreach (var child in children)
        {
            IElement? el = child.Name.LocalName switch
            {
                "Textbox" => BuildTextbox(child, dataRow),
                "Line"    => BuildLine(child),
                "Image"   => BuildImage(child, dataRow),
                "Tablix"  => BuildTablix(child),
                _         => null
            };

            if (el != null) elements.Add(el);
        }

        return elements;
    }

    // ── Element builders ──────────────────────────────────────────────────────

    private IElement BuildTextbox(XElement el, object? row)
    {
        var value = _evaluator.Evaluate(el.Element(_ns + "Value")?.Value, row);
        var styleEl = el.Element(_ns + "Style");

        var text = new TextElement(value);
        ApplyTextStyle(text.Style, styleEl);

        float paddingTop    = ParseUnit(styleEl?.Element(_ns + "PaddingTop")?.Value);
        float paddingBottom = ParseUnit(styleEl?.Element(_ns + "PaddingBottom")?.Value);
        float paddingLeft   = ParseUnit(styleEl?.Element(_ns + "PaddingLeft")?.Value);
        float paddingRight  = ParseUnit(styleEl?.Element(_ns + "PaddingRight")?.Value);

        IElement result = text;

        bool hasPadding = paddingTop != 0 || paddingBottom != 0 || paddingLeft != 0 || paddingRight != 0;
        if (hasPadding)
        {
            result = new PaddingElement
            {
                Child = result,
                Top = paddingTop, Bottom = paddingBottom,
                Left = paddingLeft, Right = paddingRight
            };
        }

        var bgColor = styleEl?.Element(_ns + "BackgroundColor")?.Value;
        if (!string.IsNullOrWhiteSpace(bgColor))
        {
            result = new BorderElement
            {
                Child = result,
                Border = new BorderStyle { Width = 0 },
                BackgroundColor = ParseColor(bgColor)
            };
        }

        return result;
    }

    private IElement BuildLine(XElement el)
    {
        var styleEl = el.Element(_ns + "Style");
        var line = new LineElement();

        var colorStr = styleEl?.Element(_ns + "Color")?.Value;
        if (!string.IsNullOrWhiteSpace(colorStr))
            line.Color = ParseColor(colorStr);

        var thicknessStr = styleEl?.Element(_ns + "BorderWidth")?.Value;
        if (!string.IsNullOrWhiteSpace(thicknessStr))
        {
            var t = ParseUnit(thicknessStr);
            if (t > 0) line.Thickness = t;
        }

        return line;
    }

    private IElement BuildImage(XElement el, object? row)
    {
        var sourceType = el.Element(_ns + "Source")?.Value ?? "External";
        var valueStr   = _evaluator.Evaluate(el.Element(_ns + "Value")?.Value, row);

        ImageElement img;

        if (string.Equals(sourceType, "Embedded", StringComparison.OrdinalIgnoreCase))
        {
            // Embedded images reference a key in <EmbeddedImages>; resolve if possible.
            img = new ImageElement(Array.Empty<byte>());
        }
        else if (string.Equals(sourceType, "Database", StringComparison.OrdinalIgnoreCase) && row != null)
        {
            // Field contains base64-encoded bytes.
            try
            {
                img = new ImageElement(Convert.FromBase64String(valueStr));
            }
            catch
            {
                img = new ImageElement(Array.Empty<byte>());
            }
        }
        else
        {
            // External: value is a file path.
            img = new ImageElement(valueStr);
        }

        var fixedW = ParseUnit(el.Element(_ns + "Width")?.Value);
        var fixedH = ParseUnit(el.Element(_ns + "Height")?.Value);
        if (fixedW > 0) img.FixedWidth = fixedW;
        if (fixedH > 0) img.FixedHeight = fixedH;

        return img;
    }

    private IElement BuildTablix(XElement tablixEl)
    {
        var dataSetName = tablixEl.Element(_ns + "DataSetName")?.Value ?? string.Empty;
        var bodyEl      = tablixEl.Element(_ns + "TablixBody");
        if (bodyEl == null) return new SpacerElement();

        // ── Column widths ─────────────────────────────────────────────────────
        var colWidths = bodyEl.Element(_ns + "TablixColumns")
            ?.Elements(_ns + "TablixColumn")
            .Select(c => ParseUnit(c.Element(_ns + "Width")?.Value))
            .ToList() ?? new List<float>();

        // ── Row hierarchy: distinguish header rows from detail rows ───────────
        var hierarchyMembers = tablixEl
            .Element(_ns + "TablixRowHierarchy")
            ?.Element(_ns + "TablixMembers")
            ?.Elements(_ns + "TablixMember")
            .ToList() ?? new List<XElement>();

        var tblRows = bodyEl.Element(_ns + "TablixRows")
            ?.Elements(_ns + "TablixRow")
            .ToList() ?? new List<XElement>();

        // A row is a detail row when its TablixMember has a <Group> child.
        bool IsDetailByHierarchy(int rowIndex)
            => rowIndex < hierarchyMembers.Count
               && hierarchyMembers[rowIndex].Element(_ns + "Group") != null;

        // Fallback: any cell value that starts with =Fields! marks the row as detail.
        bool IsDetailByExpression(XElement row)
            => row.Descendants(_ns + "Value")
                  .Any(v => RdlcExpressionEvaluator.IsFieldExpression(v.Value));

        bool hasHierarchy = hierarchyMembers.Count == tblRows.Count;

        // ── Build TableElement ─────────────────────────────────────────────────
        var table = new TableElement();

        foreach (var w in colWidths)
            table.Columns.Add(new TableColumnDefinition { FixedWidth = Math.Max(1f, w) });

        if (table.Columns.Count == 0)
            table.Columns.Add(new TableColumnDefinition { RelativeWidth = 1 });

        var dataRows = _datasets != null && _datasets.TryGetValue(dataSetName, out var ds)
            ? ds.ToList()
            : new List<object>();

        for (int ri = 0; ri < tblRows.Count; ri++)
        {
            var tblRow  = tblRows[ri];
            bool isDetail = hasHierarchy ? IsDetailByHierarchy(ri) : IsDetailByExpression(tblRow);

            var cells = tblRow.Element(_ns + "TablixCells")
                ?.Elements(_ns + "TablixCell")
                .ToList() ?? new List<XElement>();

            if (!isDetail)
            {
                // Static row (header / footer) — rendered once.
                AppendTableCells(table.HeaderCells, cells, dataRow: null, isHeader: true);
            }
            else if (dataRows.Count > 0)
            {
                // Detail row — repeated for each data row.
                foreach (var dataRow in dataRows)
                    AppendTableCells(table.DataCells, cells, dataRow, isHeader: false);
            }
            else
            {
                // No data — add one empty placeholder row to preserve table structure.
                AppendTableCells(table.DataCells, cells, dataRow: null, isHeader: false);
            }
        }

        return table;
    }

    private void AppendTableCells(
        List<TableCell> target,
        List<XElement> cellXElements,
        object? dataRow,
        bool isHeader)
    {
        foreach (var cellEl in cellXElements)
        {
            var contentEl = cellEl.Element(_ns + "CellContents");
            IElement content;

            if (contentEl != null)
            {
                // CellContents may contain a single Textbox or other element directly.
                var innerItems = BuildReportItems(WrapAsReportItems(contentEl), dataRow);
                content = innerItems.Count == 1 ? innerItems[0] : BuildColumn(innerItems);
            }
            else
            {
                content = new SpacerElement();
            }

            int colSpan = TryParseInt(
                cellEl.Attribute("ColSpan")?.Value ?? cellEl.Element(_ns + "ColSpan")?.Value, 1);

            target.Add(new TableCell
            {
                Content = content,
                ColumnSpan = Math.Max(1, colSpan),
                IsHeader = isHeader
            });
        }
    }

    /// <summary>
    /// Wraps the direct children of <paramref name="cellContents"/> in a synthetic
    /// <c>&lt;ReportItems&gt;</c> element so they can be dispatched through
    /// <see cref="BuildReportItems"/>.
    /// </summary>
    private XElement WrapAsReportItems(XElement cellContents)
    {
        var wrapper = new XElement(_ns + "ReportItems");
        foreach (var child in cellContents.Elements())
            wrapper.Add(new XElement(child));
        return wrapper;
    }

    // ── Style helpers ─────────────────────────────────────────────────────────

    private void ApplyTextStyle(TextStyle style, XElement? styleEl)
    {
        if (styleEl == null) return;

        var fontSizeStr = styleEl.Element(_ns + "FontSize")?.Value;
        if (!string.IsNullOrWhiteSpace(fontSizeStr))
        {
            var pt = ParseUnit(fontSizeStr);
            if (pt > 0) style.FontSize = pt;
        }

        var fontFamily = styleEl.Element(_ns + "FontFamily")?.Value;
        if (!string.IsNullOrWhiteSpace(fontFamily))
            style.FontFamily = fontFamily;

        var fontWeight = styleEl.Element(_ns + "FontWeight")?.Value;
        if (string.Equals(fontWeight, "Bold", StringComparison.OrdinalIgnoreCase))
            style.Bold = true;

        var fontStyle = styleEl.Element(_ns + "FontStyle")?.Value;
        if (string.Equals(fontStyle, "Italic", StringComparison.OrdinalIgnoreCase))
            style.Italic = true;

        var textDecoration = styleEl.Element(_ns + "TextDecoration")?.Value;
        if (string.Equals(textDecoration, "Underline", StringComparison.OrdinalIgnoreCase))
            style.Underline = true;

        var colorStr = styleEl.Element(_ns + "Color")?.Value;
        if (!string.IsNullOrWhiteSpace(colorStr))
            style.Color = ParseColor(colorStr);

        var textAlignStr = styleEl.Element(_ns + "TextAlign")?.Value;
        style.Alignment = textAlignStr?.ToLowerInvariant() switch
        {
            "center"             => TextAlignment.Center,
            "right"              => TextAlignment.Right,
            "justify" or "full"  => TextAlignment.Justify,
            _                    => TextAlignment.Left
        };
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static ColumnElement BuildColumn(List<IElement> items)
    {
        var col = new ColumnElement();
        foreach (var item in items)
            col.Items.Add(item);
        return col;
    }

    // ── Unit parser ───────────────────────────────────────────────────────────

    /// <summary>Converts an RDLC length string (e.g. "2.5in", "6cm", "12pt") to points (pt).</summary>
    public static float ParseUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0f;
        value = value.Trim();

        if (value.EndsWith("in", StringComparison.OrdinalIgnoreCase)
            && float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var inVal))
            return inVal * 72f;

        if (value.EndsWith("cm", StringComparison.OrdinalIgnoreCase)
            && float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var cmVal))
            return cmVal * 28.3465f;

        if (value.EndsWith("mm", StringComparison.OrdinalIgnoreCase)
            && float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var mmVal))
            return mmVal * 2.83465f;

        if (value.EndsWith("pt", StringComparison.OrdinalIgnoreCase)
            && float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var ptVal))
            return ptVal;

        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            && float.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pxVal))
            return pxVal * 0.75f; // 96 dpi → 72 pt

        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw) ? raw : 0f;
    }

    // ── Color parser ──────────────────────────────────────────────────────────

    /// <summary>Parses an RDLC color string (named color or hex) to a <see cref="ReportColor"/>.</summary>
    public static ReportColor ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ReportColor.Black;
        value = value.Trim();

        if (value.StartsWith('#'))
        {
            try { return ReportColor.FromHex(value); }
            catch { return ReportColor.Black; }
        }

        return value.ToLowerInvariant() switch
        {
            "black"                        => ReportColor.Black,
            "white"                        => ReportColor.White,
            "red"                          => new ReportColor(255, 0, 0),
            "green"                        => new ReportColor(0, 128, 0),
            "lime"                         => new ReportColor(0, 255, 0),
            "blue"                         => new ReportColor(0, 0, 255),
            "yellow"                       => new ReportColor(255, 255, 0),
            "orange"                       => new ReportColor(255, 165, 0),
            "purple"                       => new ReportColor(128, 0, 128),
            "fuchsia" or "magenta"         => new ReportColor(255, 0, 255),
            "aqua" or "cyan"               => new ReportColor(0, 255, 255),
            "gray" or "grey"               => ReportColor.Gray,
            "silver" or "lightgray" or "lightgrey" => ReportColor.LightGray,
            "navy"                         => new ReportColor(0, 0, 128),
            "teal"                         => new ReportColor(0, 128, 128),
            "maroon"                       => new ReportColor(128, 0, 0),
            "olive"                        => new ReportColor(128, 128, 0),
            _                              => ReportColor.Black
        };
    }

    private static int TryParseInt(string? value, int defaultValue)
        => int.TryParse(value, out var result) ? result : defaultValue;
}

