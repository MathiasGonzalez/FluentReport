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
    private readonly IDictionary<string, IEnumerable<object>>? _datasets;
    private readonly IDictionary<string, object>? _parameters;
    private readonly IDictionary<string, object>? _globals;
    private readonly RdlcImportOptions _options;
    private readonly List<string> _warnings = new();

    private RdlcExpressionEvaluator _evaluator = new();
    private XNamespace _ns = XNamespace.None;
    private Dictionary<string, byte[]> _embeddedImages = new(StringComparer.OrdinalIgnoreCase);
    private string? _externalImageRoot;

    public RdlcDocumentFactory(
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null,
        IDictionary<string, object>? globals = null,
        RdlcImportOptions? options = null)
    {
        _datasets = datasets;
        _parameters = parameters;
        _globals = globals;
        _options = options ?? new RdlcImportOptions();
    }

    /// <summary>
    /// Constructs the importer skipped or could not evaluate during the last parse
    /// (unsupported report items, expressions, colors, images…). Empty when everything was imported.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    // ── Public entry points ──────────────────────────────────────────────────

    /// <summary>Parses the <c>.rdlc</c> file at <paramref name="path"/> into a <see cref="Document"/>.</summary>
    public Document ParseFromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        using var stream = File.OpenRead(fullPath);
        return Parse(XDocument.Load(stream), Path.GetDirectoryName(fullPath));
    }

    /// <summary>Parses RDLC XML from a stream.</summary>
    public Document ParseFromStream(Stream stream)
        => Parse(XDocument.Load(stream), reportDirectory: null);

    /// <summary>Parses RDLC XML from a string.</summary>
    public Document ParseFromXml(string xml)
        => Parse(XDocument.Parse(xml), reportDirectory: null);

    // ── Core parse pipeline ──────────────────────────────────────────────────

    private Document Parse(XDocument xdoc, string? reportDirectory)
    {
        _warnings.Clear();
        _ns = DetectNamespace(xdoc);
        var root = xdoc.Root ?? throw new InvalidOperationException("RDLC XML has no root element.");

        _evaluator = new RdlcExpressionEvaluator(
            _parameters, _datasets, _globals,
            _options.Culture ?? ReportCulture(root),
            _warnings.Add,
            _options.StrictExpressions);

        _externalImageRoot = !_options.LoadExternalImages ? null
            : _options.ExternalImageDirectory != null ? Path.GetFullPath(_options.ExternalImageDirectory)
            : reportDirectory;

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

    /// <summary>The report's literal &lt;Language&gt; (e.g. "es-UY"), or invariant when absent or an expression.</summary>
    private CultureInfo ReportCulture(XElement root)
    {
        var language = root.Element(_ns + "Language")?.Value.Trim();
        if (string.IsNullOrEmpty(language) || language.StartsWith('='))
            return CultureInfo.InvariantCulture;
        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            _warnings.Add($"Report language '{language}' is not a known culture; using the invariant culture.");
            return CultureInfo.InvariantCulture;
        }
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
            _         => Unsupported(child)
        };

    private IElement? Unsupported(XElement item)
    {
        var name = item.Attribute("Name")?.Value;
        _warnings.Add($"Report item <{item.Name.LocalName}>{(name != null ? $" '{name}'" : "")} is not supported and was skipped.");
        return null;
    }

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
                width:  ParseUnit(e.Element(_ns + "Width")?.Value),
                height: ParseUnit(e.Element(_ns + "Height")?.Value)
            ))
            .OrderBy(i => i.top)
            .ThenBy(i => i.left)
            .ToList();

        if (positioned.Count == 0) return new SpacerElement();

        // Group items into horizontal bands: items whose Top values are within 2 pt.
        const float topTolerance = 2f;
        var bands = new List<List<(XElement el, float top, float left, float width, float height)>>();

        foreach (var item in positioned)
        {
            var last = bands.Count > 0 ? bands[^1] : null;
            if (last != null && Math.Abs(item.top - last[0].top) <= topTolerance)
                last.Add(item);
            else
                bands.Add([item]);
        }

        // Vertical whitespace between bands smaller than this is treated as layout slack.
        const float minGap = 6f;
        var col = new ColumnElement();
        float previousBottom = bands[0][0].top; // the region's own top offset is not reproduced

        foreach (var band in bands)
        {
            float bandTop = band[0].top;
            if (bandTop - previousBottom >= minGap)
                col.Items.Add(new SpacerElement(bandTop - previousBottom));
            previousBottom = Math.Max(previousBottom, band.Max(i => i.top + i.height));

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

        if (StyleColor(styleEl, "BackgroundColor", row) is { } background)
        {
            result = new BorderElement
            {
                Child = result,
                Border = new BorderStyle { Width = 0 },
                BackgroundColor = background
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
        ApplyTextStyle(text.Style, styleEl, row);
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

            foreach (var runEl in paraEl
                .Elements(_ns + "TextRuns")
                .Elements(_ns + "TextRun"))
            {
                var value      = _evaluator.Evaluate(runEl.Element(_ns + "Value")?.Value, row);
                var runStyleEl = runEl.Element(_ns + "Style");

                var spanStyle = new TextStyle();
                // Textbox defaults first, then the paragraph (where RDLC keeps TextAlign), then the run.
                ApplyTextStyle(spanStyle, boxStyleEl, row);
                ApplyTextStyle(spanStyle, paraStyleEl, row);
                ApplyTextStyle(spanStyle, runStyleEl, row);

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

        if (StyleColor(styleEl, "Color", row: null) is { } color)
            line.Color = color;

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
                : MissingImage($"embedded image '{valueStr}' was not found");
        }
        else if (string.Equals(sourceType, "Database", StringComparison.OrdinalIgnoreCase))
        {
            // Field contains the image bytes, or their base64 text.
            var raw = _evaluator.EvaluateValue(el.Element(_ns + "Value")?.Value, row);
            img = raw switch
            {
                byte[] bytes => new ImageElement(bytes),
                string b64 when TryFromBase64(b64, out var decoded) => new ImageElement(decoded),
                _ => MissingImage("database image value is not image bytes or base64")
            };
        }
        else
        {
            img = LoadExternalImage(valueStr);
        }

        if (img.SourceBytes.Length > 0 && img.MimeType == null)
            _warnings.Add($"Image '{el.Attribute("Name")?.Value}' is not a recognized image format and will not be rendered.");

        var fixedW = ParseUnit(el.Element(_ns + "Width")?.Value);
        var fixedH = ParseUnit(el.Element(_ns + "Height")?.Value);
        if (fixedW > 0) img.FixedWidth = fixedW;
        if (fixedH > 0) img.FixedHeight = fixedH;

        return img;
    }

    private ImageElement MissingImage(string reason)
    {
        _warnings.Add($"Image skipped: {reason}.");
        return new ImageElement([]);
    }

    private static bool TryFromBase64(string text, out byte[] bytes)
    {
        bytes = new byte[text.Length];
        if (Convert.TryFromBase64String(text, bytes, out int written))
        {
            bytes = bytes[..written];
            return true;
        }
        return false;
    }

    /// <summary>
    /// Loads an External image only from inside the allowed directory, so a report definition
    /// cannot pull arbitrary files from the machine into the output.
    /// </summary>
    private ImageElement LoadExternalImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return MissingImage("external image path is empty");
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile)
            return MissingImage($"external image '{path}' is a URL; only local files are supported");
        if (_externalImageRoot == null)
            return MissingImage($"external image '{path}' ignored: set RdlcImportOptions.ExternalImageDirectory to load external images");

        var root = Path.TrimEndingDirectorySeparator(_externalImageRoot) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, path));
        if (!fullPath.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            return MissingImage($"external image '{path}' is outside the allowed directory");
        if (!File.Exists(fullPath))
            return MissingImage($"external image '{path}' was not found");

        return new ImageElement(File.ReadAllBytes(fullPath));
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

        bool seenDetail = false;
        for (int ri = 0; ri < tblRows.Count; ri++)
        {
            var tblRow  = tblRows[ri];
            bool isDetail = hasHierarchy ? IsDetailByHierarchy(ri) : IsDetailByExpression(tblRow);
            seenDetail |= isDetail;

            var cells = tblRow.Element(_ns + "TablixCells")
                ?.Elements(_ns + "TablixCell")
                .ToList() ?? [];

            if (!isDetail)
            {
                // Static row, rendered once: a header before the details, a footer (totals) after them.
                AppendTableCells(seenDetail ? table.FooterCells : table.HeaderCells, cells, dataRow: null);
            }
            else if (dataRows.Count > 0)
            {
                // Detail row — repeated for each data row.
                foreach (var dataRow in dataRows)
                    AppendTableCells(table.DataCells, cells, dataRow);
            }
            else
            {
                // No data — add one empty placeholder row to preserve table structure.
                AppendTableCells(table.DataCells, cells, dataRow: null);
            }
        }

        return table;
    }

    private void AppendTableCells(
        List<TableCell> target,
        List<XElement> cellXElements,
        object? dataRow)
    {
        for (int ci = 0; ci < cellXElements.Count; ci++)
        {
            var cellEl = cellXElements[ci];
            var contentEl = cellEl.Element(_ns + "CellContents");
            IElement content;

            if (contentEl != null)
            {
                // CellContents holds one report item (plus optional ColSpan/RowSpan).
                var innerItems = BuildReportItems(WrapAsReportItems(contentEl), dataRow);
                content = innerItems.Count == 1 ? innerItems[0] : BuildColumn(innerItems);
            }
            else
            {
                content = new SpacerElement();
            }

            // RDL keeps ColSpan inside <CellContents>; older drafts put it on the cell.
            int colSpan = Math.Max(1, TryParseInt(
                contentEl?.Element(_ns + "ColSpan")?.Value
                ?? cellEl.Element(_ns + "ColSpan")?.Value
                ?? cellEl.Attribute("ColSpan")?.Value, 1));

            target.Add(new TableCell { Content = content, ColumnSpan = colSpan });

            // A spanning cell is followed by empty <TablixCell/> placeholders for the columns it covers.
            for (int skip = 1; skip < colSpan && ci + 1 < cellXElements.Count
                               && cellXElements[ci + 1].Element(_ns + "CellContents") == null; skip++)
                ci++;
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
            if (child.Name.LocalName is not ("ColSpan" or "RowSpan"))
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
    /// <summary>Applies the RDLC style properties present in <paramref name="styleEl"/>; absent ones are left untouched.</summary>
    private void ApplyTextStyle(TextStyle style, XElement? styleEl, object? row)
    {
        if (styleEl == null) return;

        var fontSize = StyleValue(styleEl, "FontSize", row);
        if (fontSize != null && ParseUnit(fontSize) is > 0 and var pt)
            style.FontSize = pt;

        if (StyleValue(styleEl, "FontFamily", row) is { } fontFamily)
            style.FontFamily = fontFamily;

        if (StyleValue(styleEl, "FontWeight", row) is { } weight)
            style.Bold = weight.ToLowerInvariant() is "bold" or "semibold" or "extrabold" or "heavy" or "black";

        if (StyleValue(styleEl, "FontStyle", row) is { } fontStyle)
            style.Italic = fontStyle.Equals("Italic", StringComparison.OrdinalIgnoreCase);

        if (StyleValue(styleEl, "TextDecoration", row) is { } decoration)
            style.Underline = decoration.Equals("Underline", StringComparison.OrdinalIgnoreCase);

        if (StyleColor(styleEl, "Color", row) is { } color)
            style.Color = color;

        if (StyleValue(styleEl, "TextAlign", row) is { } align)
        {
            style.Alignment = align.ToLowerInvariant() switch
            {
                "center"            => TextAlignment.Center,
                "right"             => TextAlignment.Right,
                "justify" or "full" => TextAlignment.Justify,
                _                   => TextAlignment.Left
            };
        }
    }

    /// <summary>
    /// Reads a style property, evaluating it when it is an expression (e.g. <c>=IIF(Fields!X.Value &lt; 0, "Red", "Black")</c>).
    /// Returns <c>null</c> when the property is absent or empty.
    /// </summary>
    private string? StyleValue(XElement? styleEl, string name, object? row)
    {
        var raw = styleEl?.Element(_ns + name)?.Value;
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = _evaluator.Evaluate(raw, row).Trim();
        return value.Length == 0 ? null : value;
    }

    private ReportColor? StyleColor(XElement? styleEl, string name, object? row)
    {
        var value = StyleValue(styleEl, name, row);
        if (value == null || value.Equals("Transparent", StringComparison.OrdinalIgnoreCase)) return null;

        var color = ParseColor(value);
        if (color == null)
            _warnings.Add($"Color '{value}' ({name}) is not recognized and was ignored.");
        return color;
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
    internal static float ParseUnit(string? value)
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

    /// <summary>
    /// Parses an RDLC color: <c>#RRGGBB</c>, <c>#AARRGGBB</c> (RDL puts alpha first) or any .NET named
    /// color. Returns <c>null</c> for unknown or fully transparent colors.
    /// </summary>
    internal static ReportColor? ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();

        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length == 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
            {
                var a = (byte)(argb >> 24);
                return a == 0 ? null : new ReportColor((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, a);
            }
            try { return ReportColor.FromHex(value); }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException) { return null; }
        }

        var named = System.Drawing.Color.FromName(value);
        // SSRS designers write "LightGrey", "DimGrey"…; .NET only knows the "Gray" spelling.
        if (!named.IsKnownColor && value.Contains("grey", StringComparison.OrdinalIgnoreCase))
            named = System.Drawing.Color.FromName(value.Replace("grey", "gray", StringComparison.OrdinalIgnoreCase));
        if (!named.IsKnownColor || named.A == 0) return null;
        return new ReportColor(named.R, named.G, named.B, named.A);
    }

    private static int TryParseInt(string? value, int defaultValue)
        => int.TryParse(value, out var result) ? result : defaultValue;
}

