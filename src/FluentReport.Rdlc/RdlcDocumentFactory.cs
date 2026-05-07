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
    private Dictionary<string, byte[]> _embeddedImages = new(StringComparer.OrdinalIgnoreCase);

    public RdlcDocumentFactory(
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null)
    {
        _datasets = datasets;
        _evaluator = new RdlcExpressionEvaluator(parameters, datasets);
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

        // Pre-load embedded images so BuildImage can reference them by name.
        _embeddedImages = ParseEmbeddedImages(root);

        // Support both SSRS 2008+ (<ReportSections>) and SSRS 2005 (<Body>/<Page> directly).
        var sections = root.Element(_ns + "ReportSections")
            ?.Elements(_ns + "ReportSection")
            .ToList()
            ?? [root];

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

    /// <summary>
    /// Reads the <c>&lt;EmbeddedImages&gt;</c> section of the RDLC and returns a map from
    /// image name → decoded PNG/JPEG bytes, ready to pass to <see cref="ImageElement"/>.
    /// </summary>
    private Dictionary<string, byte[]> ParseEmbeddedImages(XElement root)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var imgEl in root
            .Elements(_ns + "EmbeddedImages")
            .Elements(_ns + "EmbeddedImage"))
        {
            var name = imgEl.Attribute("Name")?.Value;
            var b64  = imgEl.Element(_ns + "ImageData")?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(b64))
                continue;

            try
            {
                // Base64 data in RDLC files is typically split across multiple lines;
                // remove all whitespace before decoding.
                var clean = b64.ReplaceLineEndings(string.Empty)
                               .Replace(" ", string.Empty)
                               .Replace("\t", string.Empty);
                result[name] = Convert.FromBase64String(clean);
            }
            catch
            {
                // Ignore malformed entries — the image will simply be skipped.
            }
        }

        return result;
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
            float hdrWidth = ParseUnit(headerEl.Element(_ns + "Width")?.Value);
            page.HeaderElement = BuildBodyLayout(headerEl.Element(_ns + "ReportItems"), hdrWidth, null);
        }

        if (footerEl != null)
        {
            float ftrWidth = ParseUnit(footerEl.Element(_ns + "Width")?.Value);
            page.FooterElement = BuildBodyLayout(footerEl.Element(_ns + "ReportItems"), ftrWidth, null);
        }

        // Body content
        var bodyEl = section.Element(_ns + "Body") ?? reportRoot.Element(_ns + "Body");
        float bodyWidth = ParseUnit(bodyEl?.Element(_ns + "Width")?.Value);
        page.ContentElement = BuildBodyLayout(bodyEl?.Element(_ns + "ReportItems"), bodyWidth, null);

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

    /// <summary>
    /// Builds a single RDLC element from its XML node, dispatching by local name.
    /// Returns <c>null</c> for unsupported element types.
    /// </summary>
    private IElement? BuildSingleElement(XElement child, object? dataRow)
        => child.Name.LocalName switch
        {
            "Textbox" => BuildTextbox(child, dataRow),
            "Line"    => BuildLine(child),
            "Image"   => BuildImage(child, dataRow),
            "Tablix"  => BuildTablix(child),
            _         => null
        };

    /// <summary>
    /// Builds a flat vertical list of elements from a <c>&lt;ReportItems&gt;</c> node.
    /// Used for Tablix cell contents where items have no meaningful absolute position.
    /// </summary>
    private List<IElement> BuildReportItems(XElement? reportItemsEl, object? dataRow)
    {
        if (reportItemsEl == null) return [];

        var children = reportItemsEl.Elements()
            .OrderBy(e => ParseUnit(e.Element(_ns + "Top")?.Value))
            .ThenBy(e => ParseUnit(e.Element(_ns + "Left")?.Value))
            .ToList();

        var elements = new List<IElement>();
        foreach (var child in children)
        {
            var el = BuildSingleElement(child, dataRow);
            if (el != null) elements.Add(el);
        }
        return elements;
    }

    /// <summary>
    /// Builds an absolutely-positioned layout from a body/header/footer
    /// <c>&lt;ReportItems&gt;</c> node.
    /// Items that share the same <c>Top</c> value (within a 2 pt tolerance) are grouped
    /// into a <see cref="RowElement"/> so that side-by-side panels are rendered correctly.
    /// </summary>
    private IElement BuildBodyLayout(XElement? reportItemsEl, float containerWidth, object? dataRow)
    {
        if (reportItemsEl == null) return new SpacerElement();

        var positioned = reportItemsEl.Elements()
            .Select(e => (
                el:     e,
                top:    ParseUnit(e.Element(_ns + "Top")?.Value),
                left:   ParseUnit(e.Element(_ns + "Left")?.Value),
                width:  ParseUnit(e.Element(_ns + "Width")?.Value)
            ))
            .OrderBy(i => i.top)
            .ThenBy(i => i.left)
            .ToList();

        if (positioned.Count == 0) return new SpacerElement();

        // Group items into horizontal bands: items whose Top values are within 2 pt.
        const float topTolerance = 2f;
        var bands = new List<List<(XElement el, float top, float left, float width)>>();

        foreach (var item in positioned)
        {
            var last = bands.Count > 0 ? bands[^1] : null;
            if (last != null && Math.Abs(item.top - last[0].top) <= topTolerance)
                last.Add(item);
            else
                bands.Add([item]);
        }

        var col = new ColumnElement();

        foreach (var band in bands)
        {
            if (band.Count == 1)
            {
                // Single item — render inline, no horizontal offset needed.
                var built = BuildSingleElement(band[0].el, dataRow);
                if (built != null) col.Items.Add(built);
                continue;
            }

            // Multiple items in the same band → side-by-side row.
            // Use Left positions to insert spacers between items.
            var row = new RowElement();
            float prevRight = band.Min(i => i.left); // skip leading indent

            foreach (var item in band.OrderBy(i => i.left))
            {
                float gap = item.left - prevRight;
                if (gap > 0.5f)
                    row.Items.Add(new RowItem { Element = new SpacerElement(), FixedWidth = gap });

                var built = BuildSingleElement(item.el, dataRow);
                float w = item.width > 0 ? item.width
                          : containerWidth > 0 ? Math.Max(1f, containerWidth - item.left)
                          : 1f;
                row.Items.Add(new RowItem { Element = built ?? new SpacerElement(), FixedWidth = w });
                prevRight = item.left + w;
            }

            col.Items.Add(row);
        }

        return col;
    }

    // ── Element builders ──────────────────────────────────────────────────────

    private IElement BuildTextbox(XElement el, object? row)
    {
        var styleEl = el.Element(_ns + "Style");

        // RDLC textboxes use either a legacy direct <Value> child
        // or the modern <Paragraphs>/<Paragraph>/<TextRuns>/<TextRun>/<Value> structure.
        var paragraphsEl = el.Element(_ns + "Paragraphs");
        TextElement text = paragraphsEl != null
            ? BuildTextFromParagraphs(paragraphsEl, row, styleEl)
            : BuildTextFromDirectValue(el, row, styleEl);

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

    /// <summary>
    /// Builds a <see cref="TextElement"/> from the legacy <c>&lt;Value&gt;</c> child of a textbox.
    /// </summary>
    private TextElement BuildTextFromDirectValue(XElement el, object? row, XElement? styleEl)
    {
        var value = _evaluator.Evaluate(el.Element(_ns + "Value")?.Value, row);
        var text = new TextElement(value);
        ApplyTextStyle(text.Style, styleEl);
        return text;
    }

    /// <summary>
    /// Builds a <see cref="TextElement"/> from a <c>&lt;Paragraphs&gt;</c> structure, creating
    /// one span per <c>&lt;TextRun&gt;</c> so mixed styles within a single textbox are preserved.
    /// </summary>
    private TextElement BuildTextFromParagraphs(XElement paragraphsEl, object? row, XElement? boxStyleEl)
    {
        var text = new TextElement();

        foreach (var paraEl in paragraphsEl.Elements(_ns + "Paragraph"))
        {
            var paraStyleEl = paraEl.Element(_ns + "Style");
            var textAlignStr = paraStyleEl?.Element(_ns + "TextAlign")?.Value;
            TextAlignment paraAlign = textAlignStr?.ToLowerInvariant() switch
            {
                "center"            => TextAlignment.Center,
                "right"             => TextAlignment.Right,
                "justify" or "full" => TextAlignment.Justify,
                _                   => TextAlignment.Left
            };

            foreach (var runEl in paraEl
                .Elements(_ns + "TextRuns")
                .Elements(_ns + "TextRun"))
            {
                var value      = _evaluator.Evaluate(runEl.Element(_ns + "Value")?.Value, row);
                var runStyleEl = runEl.Element(_ns + "Style");

                var spanStyle = new TextStyle { Alignment = paraAlign };
                // Apply box-level defaults first, then run-level overrides.
                ApplyTextStyle(spanStyle, boxStyleEl);
                ApplyTextStyle(spanStyle, runStyleEl);

                text.AddSpan(value, spanStyle);
            }
        }

        if (text.Spans.Count == 0)
            text.AddSpan(string.Empty);

        return text;
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
            // valueStr is the name of an entry in <EmbeddedImages>.
            img = _embeddedImages.TryGetValue(valueStr, out var embBytes)
                ? new ImageElement(embBytes)
                : new ImageElement([]);
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
                img = new ImageElement([]);
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
            .ToList() ?? [];

        // ── Row hierarchy: distinguish header rows from detail rows ───────────
        var hierarchyMembers = tablixEl
            .Element(_ns + "TablixRowHierarchy")
            ?.Element(_ns + "TablixMembers")
            ?.Elements(_ns + "TablixMember")
            .ToList() ?? [];

        var tblRows = bodyEl.Element(_ns + "TablixRows")
            ?.Elements(_ns + "TablixRow")
            .ToList() ?? [];

        // Flatten the hierarchy into a list of (isDetail) flags, one per TablixRow.
        // When a member has nested TablixMembers, its children map to consecutive rows.
        // A row is "detail" when its closest ancestor member with a <Group> is that group.
        var flatHierarchy = FlattenRowHierarchy(hierarchyMembers);

        // A row is a detail row when its TablixMember has a <Group> child.
        bool IsDetailByHierarchy(int rowIndex)
            => rowIndex < flatHierarchy.Count && flatHierarchy[rowIndex];

        // Fallback: any cell value that starts with =Fields! marks the row as detail.
        bool IsDetailByExpression(XElement row)
            => row.Descendants(_ns + "Value")
                  .Any(v => RdlcExpressionEvaluator.IsFieldExpression(v.Value));

        bool hasHierarchy = flatHierarchy.Count == tblRows.Count;

        // ── Scale column widths to fit the declared tablix Width ─────────────
        // Always use proportional (relative) widths so the table fills whatever
        // rendered space it is given, with columns scaled to their declared proportions.
        // This handles cases where the declared tablix width is wider than the page.
        float tablixDeclaredWidth = ParseUnit(tablixEl.Element(_ns + "Width")?.Value);
        if (tablixDeclaredWidth > 0 && colWidths.Count > 0)
        {
            float totalColWidth = colWidths.Sum();
            if (totalColWidth > tablixDeclaredWidth + 1f)
            {
                float scale = tablixDeclaredWidth / totalColWidth;
                colWidths = colWidths.Select(w => w * scale).ToList();
            }
        }

        // ── Build TableElement ────────────────────────────────────────────────
        var table = new TableElement();

        // Convert to relative (proportional) widths so the table always fits its
        // available container width rather than rendering at its absolute declared width.
        float totalWidth = colWidths.Count > 0 ? colWidths.Sum() : 1f;
        foreach (var w in colWidths)
            table.Columns.Add(new TableColumnDefinition
            {
                RelativeWidth = totalWidth > 0 ? w / totalWidth : 1f
            });

        if (table.Columns.Count == 0)
            table.Columns.Add(new TableColumnDefinition { RelativeWidth = 1 });

        var dataRows = _datasets != null && _datasets.TryGetValue(dataSetName, out var ds)
            ? ds.ToList()
            : [];

        for (int ri = 0; ri < tblRows.Count; ri++)
        {
            var tblRow  = tblRows[ri];
            bool isDetail = hasHierarchy ? IsDetailByHierarchy(ri) : IsDetailByExpression(tblRow);

            var cells = tblRow.Element(_ns + "TablixCells")
                ?.Elements(_ns + "TablixCell")
                .ToList() ?? [];

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

    // ── Row hierarchy flattening ──────────────────────────────────────────────

    /// <summary>
    /// Recursively flattens the TablixRowHierarchy members into a list of <c>isDetail</c> booleans,
    /// one per body row.  When a member has a <c>&lt;Group&gt;</c> element, its leaf descendants
    /// are detail rows; otherwise they are static (header/footer) rows.
    /// </summary>
    private List<bool> FlattenRowHierarchy(List<XElement> members, bool parentIsDetail = false)
    {
        var result = new List<bool>();
        foreach (var member in members)
        {
            bool hasGroup = member.Element(_ns + "Group") != null;
            bool isDetail = parentIsDetail || hasGroup;

            var nested = member.Element(_ns + "TablixMembers")
                ?.Elements(_ns + "TablixMember")
                .ToList();

            if (nested != null && nested.Count > 0)
            {
                // This member groups child rows — recurse and don't emit a row for the parent.
                result.AddRange(FlattenRowHierarchy(nested, isDetail));
            }
            else
            {
                // Leaf member → corresponds to exactly one TablixRow.
                result.Add(isDetail);
            }
        }
        return result;
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
            "red"                          => new(255, 0, 0),
            "green"                        => new(0, 128, 0),
            "lime"                         => new(0, 255, 0),
            "blue"                         => new(0, 0, 255),
            "yellow"                       => new(255, 255, 0),
            "orange"                       => new(255, 165, 0),
            "purple"                       => new(128, 0, 128),
            "fuchsia" or "magenta"         => new(255, 0, 255),
            "aqua" or "cyan"               => new(0, 255, 255),
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

