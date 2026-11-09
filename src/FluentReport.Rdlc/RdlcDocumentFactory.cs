using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;

namespace FluentReport.Rdlc;

/// <summary>
/// Parses a <c>.rdlc</c> XML file into a FluentReport <see cref="Document"/>.
/// </summary>
/// <remarks>
/// Supported report items: <c>Textbox</c>, <c>Line</c>, <c>Image</c>, <c>Rectangle</c> and
/// <c>Tablix</c> (tables and lists, with row groups, sorting and filters). See
/// <c>docs/rdlc-limitations.md</c> for the full list and what is approximated.
/// </remarks>
public sealed class RdlcDocumentFactory
{
    private readonly IDictionary<string, IEnumerable<object>>? _datasets;
    private readonly IDictionary<string, object>? _parameters;
    private readonly IDictionary<string, object>? _globals;
    private readonly RdlcImportOptions _options;
    private readonly List<string> _warnings = new();
    private readonly HashSet<string> _warningSet = new(StringComparer.Ordinal);
    private IDictionary<string, IEnumerable<object>>? _effectiveDatasets;

    private RdlcExpressionEvaluator _evaluator = new();
    private XNamespace _ns = XNamespace.None;
    private Dictionary<string, byte[]> _embeddedImages = new(StringComparer.OrdinalIgnoreCase);
    private string? _externalImageRoot;

    /// <summary>Creates a factory with the data the report reads: datasets by name, report parameters and globals.</summary>
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

    /// <summary>Records a warning once; the same construct in every row is reported a single time.</summary>
    private void Warn(string warning)
    {
        if (_warningSet.Add(warning))
            _warnings.Add(warning);
    }

    // ── Public entry points ──────────────────────────────────────────────────

    /// <summary>Parses the <c>.rdlc</c> file at <paramref name="path"/> into a <see cref="Document"/>.</summary>
    public Document ParseFromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        using var stream = File.OpenRead(fullPath);
        return Parse(LoadXml(stream), Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath));
    }

    /// <summary>Parses RDLC XML from a stream.</summary>
    public Document ParseFromStream(Stream stream)
        => Parse(LoadXml(stream), reportDirectory: null, reportName: null);

    /// <summary>Parses RDLC XML from a string.</summary>
    public Document ParseFromXml(string xml)
    {
        using var reader = new StringReader(xml);
        return Parse(LoadXml(reader), reportDirectory: null, reportName: null);
    }

    /// <summary>
    /// Deepest element nesting accepted. Real reports stay well below it (a tablix inside a rectangle
    /// is about 30 levels); a deeper document is rejected because building it recurses once per level
    /// and running out of stack ends the process.
    /// </summary>
    internal const int MaxXmlDepth = 256;

    // RDLC never declares a DTD: prohibiting it rules out entity expansion and external entities
    // regardless of the runtime's defaults.
    private static XmlReaderSettings XmlSettings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
    };

    private static XDocument LoadXml(Stream stream)
    {
        using var reader = XmlReader.Create(stream, XmlSettings());
        return CheckDepth(XDocument.Load(reader));
    }

    private static XDocument LoadXml(TextReader text)
    {
        using var reader = XmlReader.Create(text, XmlSettings());
        return CheckDepth(XDocument.Load(reader));
    }

    private static XDocument CheckDepth(XDocument xdoc)
    {
        if (xdoc.Root == null) return xdoc;
        var pending = new Stack<(XElement Element, int Depth)>();
        pending.Push((xdoc.Root, 1));
        while (pending.Count > 0)
        {
            var (element, depth) = pending.Pop();
            if (depth > MaxXmlDepth)
                throw new InvalidOperationException($"RDLC XML is nested more than {MaxXmlDepth} elements deep.");
            foreach (var child in element.Elements())
                pending.Push((child, depth + 1));
        }
        return xdoc;
    }

    // ── Core parse pipeline ──────────────────────────────────────────────────

    private Document Parse(XDocument xdoc, string? reportDirectory, string? reportName)
    {
        _warnings.Clear();
        _warningSet.Clear();
        _ns = DetectNamespace(xdoc);
        var root = xdoc.Root ?? throw new InvalidOperationException("RDLC XML has no root element.");
        var culture = _options.Culture ?? ReportCulture(root);

        _fieldDefinitions = ParseFieldDefinitions(root);

        // Dataset <Filters> are applied first; every aggregate then sees the filtered rows.
        _effectiveDatasets = _datasets;
        _evaluator = CreateEvaluator(culture, reportName);
        _effectiveDatasets = ApplyDataSetFilters(root);
        _evaluator = CreateEvaluator(culture, reportName);

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

    private RdlcExpressionEvaluator CreateEvaluator(CultureInfo culture, string? reportName)
    {
        var evaluator = new RdlcExpressionEvaluator(
            _parameters, _effectiveDatasets, _globals, culture, Warn, _options.StrictExpressions);
        evaluator.DefaultGlobals["ExecutionTime"] = DateTime.Now;
        foreach (var (name, definition) in _fieldDefinitions)
            evaluator.FieldDefinitions[name] = definition;
        if (reportName != null) evaluator.DefaultGlobals["ReportName"] = reportName;
        return evaluator;
    }

    private Dictionary<string, List<(string? DataField, string? Expression)>> _fieldDefinitions = new();

    /// <summary>
    /// <c>&lt;Field Name="X"&gt;</c> → its <c>&lt;DataField&gt;</c> (the member to read, which the designer may
    /// name differently) or <c>&lt;Value&gt;</c> (a calculated field).
    /// </summary>
    private Dictionary<string, List<(string? DataField, string? Expression)>> ParseFieldDefinitions(XElement root)
    {
        var result = new Dictionary<string, List<(string?, string?)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in root.Elements(_ns + "DataSets").Elements(_ns + "DataSet")
                     .Elements(_ns + "Fields").Elements(_ns + "Field"))
        {
            var name = field.Attribute("Name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;
            var dataField = field.Element(_ns + "DataField")?.Value.Trim();
            var expression = field.Element(_ns + "Value")?.Value.Trim();
            if (string.IsNullOrEmpty(expression) && (string.IsNullOrEmpty(dataField) || dataField.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue; // reads the member named like the field: the default
            if (!result.TryGetValue(name, out var list))
                result[name] = list = new List<(string?, string?)>();
            list.Add((string.IsNullOrEmpty(expression) ? dataField : null, string.IsNullOrEmpty(expression) ? null : expression));
        }
        return result;
    }

    private IDictionary<string, IEnumerable<object>>? ApplyDataSetFilters(XElement root)
    {
        if (_datasets == null) return null;

        var filters = root.Element(_ns + "DataSets")?.Elements(_ns + "DataSet")
            .Where(d => d.Attribute("Name") != null && d.Element(_ns + "Filters") != null)
            .ToDictionary(d => d.Attribute("Name")!.Value, d => d.Element(_ns + "Filters")!, StringComparer.OrdinalIgnoreCase);
        if (filters == null || filters.Count == 0) return _datasets;

        var result = new Dictionary<string, IEnumerable<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, rows) in _datasets)
            result[name] = filters.TryGetValue(name, out var f) ? Filter(rows, f).ToList() : rows;
        return result;
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
            Warn($"Report language '{language}' is not a known culture; using the invariant culture.");
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
    {
        if (IsHidden(child, dataRow)) return null;
        return child.Name.LocalName switch
        {
            "Textbox"   => BuildTextbox(child, dataRow),
            "Line"      => BuildLine(child),
            "Image"     => BuildImage(child, dataRow),
            "Tablix"    => BuildTablix(child),
            "Rectangle" => BuildRectangle(child, dataRow),
            _           => Unsupported(child)
        };
    }

    /// <summary>Evaluates <c>&lt;Visibility&gt;&lt;Hidden&gt;</c>; hidden items (and tablix rows/columns) are not rendered.</summary>
    private bool IsHidden(XElement item, object? row)
    {
        var hidden = item.Element(_ns + "Visibility")?.Element(_ns + "Hidden")?.Value;
        if (string.IsNullOrWhiteSpace(hidden)) return false;
        try
        {
            return _evaluator.ToBool(_evaluator.EvaluateValue(hidden.Trim(), row));
        }
        catch (RdlcExpressionException)
        {
            Warn($"Visibility '{hidden}' is not a boolean; the item is shown.");
            return false;
        }
    }

    private IElement? Unsupported(XElement item)
    {
        var name = item.Attribute("Name")?.Value;
        Warn($"Report item <{item.Name.LocalName}>{(name != null ? $" '{name}'" : "")} is not supported and was skipped.");
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
    private IElement BuildBodyLayout(XElement? reportItemsEl, float containerWidth, object? dataRow, float minHeight = 0)
    {
        if (reportItemsEl == null) return new SpacerElement(minHeight);

        var positioned = reportItemsEl.Elements()
            .Where(e => !IsHidden(e, dataRow)) // hidden items take no space
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

        if (positioned.Count == 0) return new SpacerElement(minHeight);

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
                var breakAt = band[0].el.Element(_ns + "PageBreak")?.Element(_ns + "BreakLocation")?.Value;
                if (breakAt is "Start" or "StartAndEnd") col.Items.Add(new PageBreakElement());
                var built = BuildSingleElement(band[0].el, dataRow);
                if (built != null) col.Items.Add(built);
                if (breakAt is "End" or "StartAndEnd") col.Items.Add(new PageBreakElement());
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

        // A container taller than its content (a bordered rectangle) keeps its designed height.
        if (minHeight - previousBottom >= minGap)
            col.Items.Add(new SpacerElement(minHeight - previousBottom));

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

        return WrapBoxStyle(result, styleEl, row);
    }

    /// <summary>
    /// Wraps <paramref name="content"/> with the item's background and borders. RDLC borders are per
    /// side; sides that share a width and color go into one <see cref="BorderElement"/>, others nest.
    /// </summary>
    private IElement WrapBoxStyle(IElement content, XElement? styleEl, object? row)
    {
        var background = StyleColor(styleEl, "BackgroundColor", row);
        var sides = ReadBorders(styleEl, row);

        var groups = sides.GroupBy(s => (s.Width, s.Color)).ToList();
        IElement result = content;
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            result = new BorderElement
            {
                Child = result,
                Border = new BorderStyle
                {
                    Width = g.Key.Width,
                    Color = g.Key.Color,
                    Sides = g.Aggregate(BorderSide.None, (acc, s) => acc | s.Side)
                },
                BackgroundColor = i == groups.Count - 1 ? background : null
            };
        }

        if (groups.Count == 0 && background is { } bg)
            result = new BorderElement { Child = result, Border = new BorderStyle { Width = 0 }, BackgroundColor = bg };

        return result;
    }

    /// <summary>
    /// Reads the visible border sides of a style: RDL 2008+ (<c>Border</c>, <c>TopBorder</c>…, each with
    /// Style/Color/Width) and RDL 2005 (<c>BorderStyle</c>/<c>BorderColor</c>/<c>BorderWidth</c> with
    /// Default/Top/… children). Every style other than <c>None</c> is drawn as a solid line.
    /// </summary>
    private List<(BorderSide Side, float Width, ReportColor Color)> ReadBorders(XElement? styleEl, object? row)
    {
        var result = new List<(BorderSide, float, ReportColor)>();
        if (styleEl == null) return result;

        foreach (var (side, name) in new[] { (BorderSide.Top, "Top"), (BorderSide.Right, "Right"), (BorderSide.Bottom, "Bottom"), (BorderSide.Left, "Left") })
        {
            string? Prop(string prop)
            {
                var raw = styleEl.Element(_ns + (name + "Border"))?.Element(_ns + prop)?.Value
                          ?? styleEl.Element(_ns + "Border")?.Element(_ns + prop)?.Value
                          ?? styleEl.Element(_ns + ("Border" + prop))?.Element(_ns + name)?.Value
                          ?? styleEl.Element(_ns + ("Border" + prop))?.Element(_ns + "Default")?.Value;
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var value = _evaluator.Evaluate(raw, row).Trim();
                return value.Length == 0 ? null : value;
            }

            var style = Prop("Style");
            if (style == null || style.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;

            var colorText = Prop("Color");
            var color = colorText == null ? ReportColor.Black : ParseColor(colorText);
            if (color == null)
            {
                if (colorText != null && !colorText.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
                    Warn($"Color '{colorText}' (border) is not recognized and was ignored.");
                continue;
            }

            float width = Prop("Width") is { } w && ParseUnit(w) is > 0 and var pt ? pt : 1f;
            result.Add((side, width, color.Value));
        }
        return result;
    }

    /// <summary>
    /// Evaluates a textbox value and applies the <c>Format</c> style property (from the run, else the
    /// textbox) to numbers and dates, the way SSRS does.
    /// </summary>
    private string EvaluateText(string? expression, object? row, params XElement?[] styles)
    {
        var value = _evaluator.EvaluateValue(expression, row);
        if (value is not (null or string or bool))
        {
            var format = styles.Select(s => StyleValue(s, "Format", row)).FirstOrDefault(f => f != null);
            if (format != null)
            {
                try { value = _evaluator.Format(value, format); }
                catch (RdlcExpressionException ex) { Warn($"Format '{format}' could not be applied: {ex.Message}"); }
            }
        }
        return _evaluator.ToDisplay(value);
    }

    /// <summary>Adds <paramref name="text"/> as spans, turning page-number markers into page-number spans.</summary>
    private static void AddText(TextElement element, string text, TextStyle? style)
    {
        if (text.IndexOfAny([RdlcExpressionEvaluator.PageNumberMarker, RdlcExpressionEvaluator.TotalPagesMarker]) < 0)
        {
            element.AddSpan(text, style);
            return;
        }

        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            bool page = text[i] == RdlcExpressionEvaluator.PageNumberMarker;
            bool total = text[i] == RdlcExpressionEvaluator.TotalPagesMarker;
            if (!page && !total) continue;
            if (i > start) element.AddSpan(text[start..i], style);
            if (page) element.AddCurrentPageSpan(style);
            else element.AddTotalPagesSpan(style);
            start = i + 1;
        }
        if (start < text.Length) element.AddSpan(text[start..], style);
    }

    /// <summary>
    /// Builds a <see cref="TextElement"/> from the legacy <c>&lt;Value&gt;</c> child of a textbox.
    /// </summary>
    private TextElement BuildTextFromDirectValue(XElement el, object? row, XElement? styleEl)
    {
        var text = new TextElement();
        AddText(text, EvaluateText(el.Element(_ns + "Value")?.Value, row, styleEl), style: null);
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
                var runStyleEl = runEl.Element(_ns + "Style");
                var value      = EvaluateText(runEl.Element(_ns + "Value")?.Value, row, runStyleEl, boxStyleEl);

                var spanStyle = new TextStyle();
                // Textbox defaults first, then the paragraph (where RDLC keeps TextAlign), then the run.
                ApplyTextStyle(spanStyle, boxStyleEl, row);
                ApplyTextStyle(spanStyle, paraStyleEl, row);
                ApplyTextStyle(spanStyle, runStyleEl, row);

                AddText(text, value, spanStyle);
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

        // RDL 2008+ draws a line with its Border; legacy reports use Color and a flat BorderWidth.
        if (ReadBorders(styleEl, row: null).FirstOrDefault() is { Width: > 0 } border)
        {
            line.Color = border.Color;
            line.Thickness = border.Width;
        }

        if (StyleColor(styleEl, "Color", row: null) is { } color)
            line.Color = color;

        if (styleEl?.Element(_ns + "BorderWidth") is { HasElements: false } widthEl && ParseUnit(widthEl.Value) is > 0 and var t)
            line.Thickness = t;

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
            Warn($"Image '{el.Attribute("Name")?.Value}' is not a recognized image format and will not be rendered.");

        ApplySizing(img, el);
        return img;
    }

    /// <summary>
    /// Sizes an image like SSRS: <c>Fit</c> stretches it to the item's box, <c>FitProportional</c> scales it
    /// to fit inside the box keeping its aspect ratio, <c>AutoSize</c> (the default) uses its natural size
    /// from its pixels and resolution, and <c>Clip</c> uses its natural size scaled down to fit the box
    /// (it cannot be cropped). Images whose size cannot be read fill the box.
    /// </summary>
    private void ApplySizing(ImageElement img, XElement el)
    {
        float boxW = ParseUnit(el.Element(_ns + "Width")?.Value);
        float boxH = ParseUnit(el.Element(_ns + "Height")?.Value);
        var sizing = el.Element(_ns + "Sizing")?.Value.Trim() ?? "AutoSize";
        var natural = img.MimeType == null ? null : ImageDimensions.SizeInPoints(img.SourceBytes);

        float width, height;
        if (natural is not { } n || sizing.Equals("Fit", StringComparison.OrdinalIgnoreCase))
        {
            (width, height) = (boxW, boxH);
        }
        else if (sizing.Equals("FitProportional", StringComparison.OrdinalIgnoreCase))
        {
            float scale = boxW > 0 && boxH > 0 ? Math.Min(boxW / n.Width, boxH / n.Height)
                        : boxW > 0 ? boxW / n.Width
                        : boxH > 0 ? boxH / n.Height
                        : 1f;
            (width, height) = (n.Width * scale, n.Height * scale);
        }
        else if (sizing.Equals("Clip", StringComparison.OrdinalIgnoreCase))
        {
            float scale = Math.Min(1f, Math.Min(boxW > 0 ? boxW / n.Width : 1f, boxH > 0 ? boxH / n.Height : 1f));
            (width, height) = (n.Width * scale, n.Height * scale);
        }
        else
        {
            (width, height) = n; // AutoSize: the item grows to the image
        }

        if (width > 0) img.FixedWidth = width;
        if (height > 0) img.FixedHeight = height;
    }

    private ImageElement MissingImage(string reason)
    {
        Warn($"Image skipped: {reason}.");
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
        if (PassesThroughLink(fullPath, root))
            return MissingImage($"external image '{path}' goes through a symbolic link; links could point outside the allowed directory");

        return new ImageElement(File.ReadAllBytes(fullPath));
    }

    /// <summary>True when <paramref name="fullPath"/> or a directory between it and <paramref name="root"/> is a link.</summary>
    private static bool PassesThroughLink(string fullPath, string root)
    {
        for (var path = fullPath; path.Length > root.Length; path = Path.GetDirectoryName(path)!)
        {
            FileSystemInfo info = path == fullPath ? new FileInfo(path) : new DirectoryInfo(path);
            if (info.LinkTarget != null) return true;
        }
        return false;
    }

    private IElement BuildRectangle(XElement el, object? row)
    {
        float width  = ParseUnit(el.Element(_ns + "Width")?.Value);
        float height = ParseUnit(el.Element(_ns + "Height")?.Value);
        var content = BuildBodyLayout(el.Element(_ns + "ReportItems"), width, row, height);
        return WrapBoxStyle(content, el.Element(_ns + "Style"), row);
    }

    // ── Tablix ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a table from a <c>&lt;Tablix&gt;</c> (table or list). The row hierarchy is walked as
    /// SSRS does: static members render once in their scope, group members render once per group
    /// instance (sorted and filtered), detail members once per row. Static rows before the first
    /// group become the repeated table header, static rows after the last one the footer.
    /// </summary>
    private IElement BuildTablix(XElement tablixEl)
    {
        var tablixName  = tablixEl.Attribute("Name")?.Value;
        var dataSetName = tablixEl.Element(_ns + "DataSetName")?.Value ?? string.Empty;
        var bodyEl      = tablixEl.Element(_ns + "TablixBody");
        if (bodyEl == null) return new SpacerElement();

        var colWidths = bodyEl.Element(_ns + "TablixColumns")
            ?.Elements(_ns + "TablixColumn")
            .Select(c => ParseUnit(c.Element(_ns + "Width")?.Value))
            .ToList() ?? [];

        var tblRows = bodyEl.Element(_ns + "TablixRows")
            ?.Elements(_ns + "TablixRow")
            .ToList() ?? [];

        var rowMembers = Members(tablixEl.Element(_ns + "TablixRowHierarchy"));
        var hiddenColumns = HiddenColumns(tablixEl, colWidths.Count);

        // ── Rows: the enclosing group's rows for a nested region, else the dataset ──
        IEnumerable<object> source = _evaluator.InnermostScopeRows is { } outer && _currentRegionDataset is { } outerSet
                                     && (dataSetName.Length == 0 || dataSetName.Equals(outerSet, StringComparison.OrdinalIgnoreCase))
            ? outer
            : _effectiveDatasets != null && _effectiveDatasets.TryGetValue(dataSetName, out var ds) ? ds : [];
        var dataRows = Sort(Filter(source, tablixEl.Element(_ns + "Filters")), tablixEl.Element(_ns + "SortExpressions"));

        if (dataRows.Count == 0 && tablixEl.Element(_ns + "NoRowsMessage")?.Value is { Length: > 0 } noRows)
        {
            var message = new TextElement();
            AddText(message, _evaluator.Evaluate(noRows), style: null);
            ApplyTextStyle(message.Style, tablixEl.Element(_ns + "Style"), row: null);
            return message;
        }

        // ── Columns (hidden columns dropped), scaled to the declared tablix width ──
        float tablixDeclaredWidth = ParseUnit(tablixEl.Element(_ns + "Width")?.Value);
        float totalColWidth = colWidths.Sum();
        if (tablixDeclaredWidth > 0 && totalColWidth > tablixDeclaredWidth + 1f)
            colWidths = colWidths.Select(w => w * tablixDeclaredWidth / totalColWidth).ToList();

        var table = new TableElement();
        var visibleWidths = colWidths.Where((_, i) => !hiddenColumns[i]).ToList();
        float totalWidth = visibleWidths.Sum();
        foreach (var w in visibleWidths)
            table.Columns.Add(new TableColumnDefinition { RelativeWidth = totalWidth > 0 ? w / totalWidth : 1f });
        if (table.Columns.Count == 0)
            table.Columns.Add(new TableColumnDefinition { RelativeWidth = 1 });

        var previousRegion = _currentRegionDataset;
        var previousOrder = _displayOrder; // a nested tablix must not change the outer one's order
        _currentRegionDataset = dataSetName.Length > 0 ? dataSetName : previousRegion;
        try
        {
            if (rowMembers.Sum(LeafCount) != tblRows.Count)
            {
                BuildTablixWithoutHierarchy(table, tblRows, dataRows, hiddenColumns, tablixName, dataSetName);
                return table;
            }

            // RowNumber / RunningValue count rows in the order they are displayed.
            var displayed = new List<object>();
            CollectDetailRows(rowMembers, dataRows, displayed);
            var order = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            foreach (var r in displayed) order.TryAdd(r, order.Count);
            _displayOrder = order;

            using var region = _evaluator.PushRegionScope(InDisplayOrder(dataRows), tablixName, dataSetName);

            int firstDynamic = rowMembers.FindIndex(ContainsGroup);
            int lastDynamic  = rowMembers.FindLastIndex(ContainsGroup);
            int cursor = 0;
            for (int i = 0; i < rowMembers.Count; i++)
            {
                var target = firstDynamic < 0 || i < firstDynamic ? table.HeaderCells
                           : i > lastDynamic ? table.FooterCells
                           : table.DataCells;
                EmitMember(rowMembers[i], dataRows, row: null, target, tblRows, hiddenColumns, ref cursor);
            }

            // A table with only static rows renders them as the body.
            if (firstDynamic < 0)
            {
                table.DataCells.AddRange(table.HeaderCells);
                table.HeaderCells.Clear();
            }
        }
        finally
        {
            _currentRegionDataset = previousRegion;
            _displayOrder = previousOrder;
        }

        return table;
    }

    private string? _currentRegionDataset;
    private Dictionary<object, int> _displayOrder = new(ReferenceEqualityComparer.Instance);

    private List<object> InDisplayOrder(IEnumerable<object> rows)
        => rows.OrderBy(r => _displayOrder.TryGetValue(r, out var i) ? i : int.MaxValue).ToList();

    /// <summary>
    /// Renders one row-hierarchy member: <paramref name="cursor"/> is the index of its first
    /// <c>&lt;TablixRow&gt;</c> and is advanced past all of its rows.
    /// </summary>
    private void EmitMember(XElement member, IReadOnlyList<object> scopeRows, object? row, List<TableCell> target,
                            List<XElement> tblRows, bool[] hiddenColumns, ref int cursor)
    {
        int start = cursor;
        cursor += LeafCount(member);
        var children = Members(member);
        var group = member.Element(_ns + "Group");

        if (group == null)
        {
            if (IsHidden(member, row)) return;
            if (children.Count == 0)
            {
                AppendTableCells(target, RowCells(tblRows[start]), row, hiddenColumns);
                return;
            }
            int c = start;
            foreach (var child in children)
                EmitMember(child, scopeRows, row, target, tblRows, hiddenColumns, ref c);
            return;
        }

        var groupName = group.Attribute("Name")?.Value;
        foreach (var instance in GroupInstances(member, scopeRows))
        {
            using var scope = _evaluator.PushScope(InDisplayOrder(instance), groupName);
            var first = instance[0];
            if (IsHidden(member, first)) continue;

            if (children.Count == 0)
            {
                AppendTableCells(target, RowCells(tblRows[start]), first, hiddenColumns);
                continue;
            }
            int c = start;
            foreach (var child in children)
                EmitMember(child, instance, first, target, tblRows, hiddenColumns, ref c);
        }
    }

    /// <summary>
    /// The instances of a group member over <paramref name="rows"/>: one per distinct group key, or one
    /// per row for a details group (no group expressions). Group filters and sort expressions apply.
    /// </summary>
    private List<List<object>> GroupInstances(XElement member, IReadOnlyList<object> rows)
    {
        var group = member.Element(_ns + "Group")!;
        var filtered = Filter(rows, group.Element(_ns + "Filters"));
        var sortEl = member.Element(_ns + "SortExpressions");
        var expressions = group.Element(_ns + "GroupExpressions")?.Elements(_ns + "GroupExpression")
            .Select(e => e.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? [];

        if (expressions.Count == 0)
            return Sort(filtered, sortEl).Select(r => new List<object> { r }).ToList();

        var instances = new List<List<object>>();
        var byKey = new Dictionary<string, List<object>>(StringComparer.Ordinal);
        foreach (var r in filtered)
        {
            var key = string.Join('\u001F', expressions.Select(e => _evaluator.Evaluate(e, r)));
            if (!byKey.TryGetValue(key, out var list))
                instances.Add(byKey[key] = list = new List<object>());
            list.Add(r);
        }

        // Group sort expressions may aggregate (e.g. =Sum(Fields!Total.Value)), so each instance is its own scope.
        var groupName = group.Attribute("Name")?.Value;
        return SortBy(instances, sortEl, instance =>
        {
            using var _ = _evaluator.PushScope(instance, groupName);
            return SortKeys(sortEl, instance[0]);
        });
    }

    /// <summary>Detail rows in the order the hierarchy displays them (for RowNumber / RunningValue).</summary>
    private void CollectDetailRows(List<XElement> members, IReadOnlyList<object> rows, List<object> output)
    {
        foreach (var member in members)
        {
            var children = Members(member);
            if (member.Element(_ns + "Group") == null)
            {
                CollectDetailRows(children, rows, output);
                continue;
            }
            foreach (var instance in GroupInstances(member, rows))
            {
                if (children.Count > 0) CollectDetailRows(children, instance, output);
                else output.AddRange(instance);
            }
        }
    }

    /// <summary>Fallback for a row hierarchy that does not match the rows: rows that read fields repeat per data row.</summary>
    private void BuildTablixWithoutHierarchy(TableElement table, List<XElement> tblRows, List<object> dataRows,
                                             bool[] hiddenColumns, string? tablixName, string dataSetName)
    {
        using var region = _evaluator.PushRegionScope(dataRows, tablixName, dataSetName);
        bool seenDetail = false;
        foreach (var tblRow in tblRows)
        {
            bool isDetail = tblRow.Descendants(_ns + "Value").Any(v => RdlcExpressionEvaluator.IsFieldExpression(v.Value));
            seenDetail |= isDetail;
            var cells = RowCells(tblRow);

            if (!isDetail)
                AppendTableCells(seenDetail ? table.FooterCells : table.HeaderCells, cells, null, hiddenColumns);
            else if (dataRows.Count == 0)
                AppendTableCells(table.DataCells, cells, null, hiddenColumns);
            else
                foreach (var dataRow in dataRows)
                {
                    using var detail = _evaluator.PushScope([dataRow]);
                    AppendTableCells(table.DataCells, cells, dataRow, hiddenColumns);
                }
        }
    }

    private List<XElement> RowCells(XElement tblRow)
        => tblRow.Element(_ns + "TablixCells")?.Elements(_ns + "TablixCell").ToList() ?? [];

    private List<XElement> Members(XElement? parent)
        => parent?.Element(_ns + "TablixMembers")?.Elements(_ns + "TablixMember").ToList() ?? [];

    /// <summary>Number of body rows (or columns) a hierarchy member covers: one per leaf.</summary>
    private int LeafCount(XElement member)
    {
        var children = Members(member);
        return children.Count == 0 ? 1 : children.Sum(LeafCount);
    }

    private bool ContainsGroup(XElement member)
        => member.Element(_ns + "Group") != null || Members(member).Any(ContainsGroup);

    /// <summary>Columns whose hierarchy member (or an ancestor) is hidden. Column groups are reported, not expanded.</summary>
    private bool[] HiddenColumns(XElement tablixEl, int columnCount)
    {
        var hidden = new bool[columnCount];
        var members = Members(tablixEl.Element(_ns + "TablixColumnHierarchy"));
        if (members.Any(ContainsGroup))
            Warn($"Tablix '{tablixEl.Attribute("Name")?.Value}': column groups (matrix) are not supported; columns are rendered once.");
        if (members.Sum(LeafCount) != columnCount) return hidden;

        int index = 0;
        void Walk(XElement member, bool parentHidden)
        {
            bool isHidden = parentHidden || IsHidden(member, row: null);
            var children = Members(member);
            if (children.Count == 0) { hidden[index++] = isHidden; return; }
            foreach (var child in children) Walk(child, isHidden);
        }
        foreach (var member in members) Walk(member, false);
        return hidden;
    }

    private void AppendTableCells(List<TableCell> target, List<XElement> cellXElements, object? dataRow, bool[] hiddenColumns)
    {
        int column = 0;
        for (int ci = 0; ci < cellXElements.Count; ci++)
        {
            var cellEl = cellXElements[ci];
            var contentEl = cellEl.Element(_ns + "CellContents");

            // RDL keeps ColSpan inside <CellContents>; older drafts put it on the cell.
            int colSpan = Math.Max(1, TryParseInt(
                contentEl?.Element(_ns + "ColSpan")?.Value
                ?? cellEl.Element(_ns + "ColSpan")?.Value
                ?? cellEl.Attribute("ColSpan")?.Value, 1));

            int visibleSpan = 0;
            for (int c = column; c < column + colSpan; c++)
                if (c >= hiddenColumns.Length || !hiddenColumns[c]) visibleSpan++;

            if (visibleSpan > 0)
            {
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
                target.Add(new TableCell { Content = content, ColumnSpan = visibleSpan });
            }
            column += colSpan;

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

    // ── Filters and sorting ───────────────────────────────────────────────────

    /// <summary>Rows that pass every <c>&lt;Filter&gt;</c> in <paramref name="filtersEl"/>.</summary>
    private List<object> Filter(IEnumerable<object> rows, XElement? filtersEl)
    {
        var filters = filtersEl?.Elements(_ns + "Filter").ToList();
        if (filters == null || filters.Count == 0) return rows as List<object> ?? rows.ToList();
        return rows.Where(r => filters.All(f => PassesFilter(f, r))).ToList();
    }

    private bool PassesFilter(XElement filter, object row)
    {
        var expression = filter.Element(_ns + "FilterExpression")?.Value;
        var op = filter.Element(_ns + "Operator")?.Value ?? "Equal";
        var value = _evaluator.EvaluateValue(expression, row);
        var targets = new List<object?>();
        foreach (var fv in filter.Element(_ns + "FilterValues")?.Elements(_ns + "FilterValue") ?? [])
        {
            var target = _evaluator.EvaluateValue(fv.Value, row);
            if (target is System.Collections.IEnumerable many and not string)
                targets.AddRange(many.Cast<object?>()); // multi-value parameters
            else
                targets.Add(target);
        }

        object? First() => targets.Count > 0 ? targets[0] : null;
        switch (op)
        {
            case "Equal":              return _evaluator.AreEqual(value, First());
            case "NotEqual":           return !_evaluator.AreEqual(value, First());
            case "GreaterThan":        return _evaluator.Compare(value, First()) > 0;
            case "GreaterThanOrEqual": return _evaluator.Compare(value, First()) >= 0;
            case "LessThan":           return _evaluator.Compare(value, First()) < 0;
            case "LessThanOrEqual":    return _evaluator.Compare(value, First()) <= 0;
            case "In":                 return targets.Any(t => _evaluator.AreEqual(value, t));
            case "Between":
                return targets.Count >= 2 && _evaluator.Compare(value, targets[0]) >= 0 && _evaluator.Compare(value, targets[1]) <= 0;
            case "Like":
                // NonBacktracking runs in linear time: a pattern such as "*a*a*a*a*b" cannot stall the render.
                var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(_evaluator.ToDisplay(First()))
                    .Replace("\\*", ".*").Replace("\\?", ".") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(_evaluator.ToDisplay(value), pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                    | System.Text.RegularExpressions.RegexOptions.NonBacktracking);
            default:
                Warn($"Filter operator '{op}' is not supported; the filter was ignored.");
                return true;
        }
    }

    private List<object> Sort(IEnumerable<object> rows, XElement? sortEl)
        => SortBy(rows as List<object> ?? rows.ToList(), sortEl, r => SortKeys(sortEl, r));

    private object?[] SortKeys(XElement? sortEl, object row)
        => sortEl?.Elements(_ns + "SortExpression")
               .Select(e => _evaluator.EvaluateValue(e.Element(_ns + "Value")?.Value, row))
               .ToArray() ?? [];

    /// <summary>Stable sort by the <c>&lt;SortExpression&gt;</c>s (ascending unless <c>Direction</c> is Descending).</summary>
    private List<T> SortBy<T>(List<T> items, XElement? sortEl, Func<T, object?[]> keys)
    {
        var descending = sortEl?.Elements(_ns + "SortExpression")
            .Select(e => string.Equals(e.Element(_ns + "Direction")?.Value, "Descending", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        if (descending.Length == 0 || items.Count < 2) return items;

        var keyed = items.Select(i => (Item: i, Keys: keys(i))).ToList();
        return keyed.OrderBy(k => k.Keys, Comparer<object?[]>.Create((a, b) =>
        {
            for (int i = 0; i < descending.Length; i++)
            {
                int c = _evaluator.Compare(a[i], b[i]);
                if (c != 0) return descending[i] ? -c : c;
            }
            return 0;
        })).Select(k => k.Item).ToList();
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
            Warn($"Color '{value}' ({name}) is not recognized and was ignored.");
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

