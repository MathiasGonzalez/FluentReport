using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentReport;
using FluentReport.Core;
using FluentReport.Elements;
using FluentReport.Styling;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FluentReport.Schema;

/// <summary>
/// Parses editor schema YAML/JSON files and translates them into <see cref="Document"/>.
/// </summary>
public sealed class SchemaDocumentFactory
{
    private static readonly Regex TemplateRegex = new("\\{\\{\\s*(?<expr>[^}]+)\\s*\\}\\}", RegexOptions.Compiled);

    private readonly IDictionary<string, IEnumerable<object>> _dataSources;
    private readonly IDictionary<string, object> _parameters;

    private string? _baseDirectory;
    private Dictionary<string, TextStyleNode> _styles = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, GroupDefinitionNode> _groups = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, RepeatableDefinitionNode> _repeatables = new(StringComparer.OrdinalIgnoreCase);

    public SchemaDocumentFactory(
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        _dataSources = dataSources is null
            ? new Dictionary<string, IEnumerable<object>>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, IEnumerable<object>>(dataSources, StringComparer.OrdinalIgnoreCase);
        _parameters = parameters is null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
    }

    public Document ParseFromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var content = File.ReadAllText(fullPath);
        _baseDirectory = Path.GetDirectoryName(fullPath);

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        return extension == ".json"
            ? ParseFromJson(content)
            : ParseFromYaml(content);
    }

    public Document ParseFromStream(Stream stream, string? format = null)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = reader.ReadToEnd();

        return string.Equals(format, "json", StringComparison.OrdinalIgnoreCase)
            ? ParseFromJson(content)
            : ParseFromYaml(content);
    }

    public Document ParseFromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var schema = deserializer.Deserialize<ReportSchema>(yaml)
            ?? throw new InvalidOperationException("Schema YAML could not be parsed.");

        return Build(schema);
    }

    public Document ParseFromJson(string json)
    {
        var schema = JsonSerializer.Deserialize<ReportSchema>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? throw new InvalidOperationException("Schema JSON could not be parsed.");

        return Build(schema);
    }

    private Document Build(ReportSchema schema)
    {
        if (schema.Pages is null || schema.Pages.Count == 0)
            throw new ArgumentException("Schema must contain at least one page.", nameof(schema));

        _styles = (schema.Styles ?? new Dictionary<string, TextStyleNode>())
            .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        _groups = (schema.Definitions?.Groups ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g.Id))
            .ToDictionary(g => g.Id!, g => g, StringComparer.OrdinalIgnoreCase);

        _repeatables = (schema.Definitions?.Repeatables ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .ToDictionary(r => r.Id!, r => r, StringComparer.OrdinalIgnoreCase);

        var settings = new DocumentSettings();
        foreach (var page in schema.Pages)
            settings.Pages.Add(BuildPage(page, schema.PageDefaults));

        return Document.FromSettings(settings);
    }

    private PageSettings BuildPage(SchemaPageNode page, PageDefaultsNode? defaults)
    {
        var margin = page.Margin ?? defaults?.Margin;

        var builtPage = new PageSettings
        {
            Size = ResolvePageSize(page.Size ?? defaults?.Size, page.Orientation ?? defaults?.Orientation),
            MarginTop = margin?.Top ?? 40,
            MarginRight = margin?.Right ?? 40,
            MarginBottom = margin?.Bottom ?? 40,
            MarginLeft = margin?.Left ?? 40,
            HeaderElement = BuildRegion(page.Regions?.Header),
            ContentElement = BuildRegion(page.Regions?.Content) ?? new SpacerElement(),
            FooterElement = BuildRegion(page.Regions?.Footer)
        };

        return builtPage;
    }

    private IElement? BuildRegion(RegionNode? region)
    {
        var nodes = region?.Nodes;
        if (nodes is null || nodes.Count == 0)
            return null;

        var ordered = nodes
            .OrderBy(n => n.ZIndex ?? 0)
            .ThenBy(n => n.Frame?.Y ?? 0)
            .ThenBy(n => n.Frame?.X ?? 0)
            .ToList();

        var column = new ColumnElement();
        float cursorY = 0;

        foreach (var node in ordered)
        {
            var top = node.Frame?.Y ?? cursorY;
            if (top > cursorY)
                column.Items.Add(new SpacerElement(top - cursorY));

            var element = BuildNode(node, null);
            if (element != null)
                column.Items.Add(element);

            cursorY = Math.Max(cursorY, top + (node.Frame?.Height ?? 0));
        }

        return column.Items.Count == 1 ? column.Items[0] : column;
    }

    private IElement? BuildNode(SchemaNode node, Dictionary<string, object?>? row)
    {
        var type = (node.Type ?? string.Empty).Trim().ToLowerInvariant();

        IElement? element = type switch
        {
            "text" => BuildText(node, row),
            "line" => BuildLine(node),
            "spacer" => new SpacerElement(node.Size ?? node.Frame?.Height ?? 0),
            "pagebreak" => new PageBreakElement(),
            "image" => BuildImage(node, row),
            "table" => BuildTable(node),
            "repeat" => BuildRepeat(node),
            "groupinstance" => BuildGroupInstance(node),
            _ => null
        };

        if (element == null)
            return null;

        if (!type.Equals("text", StringComparison.OrdinalIgnoreCase)
            && TryParseHorizontalAlignment(node.Align, out var align))
        {
            element = new AlignElement { Child = element, Alignment = align };
        }

        return element;
    }

    private IElement BuildText(SchemaNode node, Dictionary<string, object?>? row)
    {
        var style = ResolveTextStyle(node);

        if (node.Runs is { Count: > 0 })
        {
            var text = new TextElement();
            foreach (var run in node.Runs)
            {
                var spanStyle = style.Clone();
                if (string.Equals(run.Token, "currentPage", StringComparison.OrdinalIgnoreCase))
                {
                    text.AddCurrentPageSpan(spanStyle);
                    continue;
                }

                if (string.Equals(run.Token, "totalPages", StringComparison.OrdinalIgnoreCase))
                {
                    text.AddTotalPagesSpan(spanStyle);
                    continue;
                }

                text.AddSpan(ResolveTemplate(run.Value ?? string.Empty, row), spanStyle);
            }

            return text;
        }

        var content = ResolveTemplate(node.Value ?? string.Empty, row);
        var textElement = new TextElement(content);
        ApplyTextStyle(textElement.Style, style);
        return textElement;
    }

    private IElement BuildLine(SchemaNode node)
    {
        var line = new LineElement
        {
            Thickness = node.Thickness ?? 1
        };

        if (!string.IsNullOrWhiteSpace(node.Color))
            line.Color = ParseColor(node.Color!, ReportColor.Black);

        var frame = node.Frame;
        if (frame?.Height > frame?.Width)
            line.Direction = LineDirection.Vertical;

        return line;
    }

    private IElement BuildImage(SchemaNode node, Dictionary<string, object?>? row)
    {
        var sourceValue = ResolveTemplate(node.Source?.Value ?? string.Empty, row);
        var mode = (node.Source?.Mode ?? "path").ToLowerInvariant();

        ImageElement image;
        if (mode is "base64" or "bytes")
        {
            try
            {
                image = new ImageElement(Convert.FromBase64String(sourceValue));
            }
            catch
            {
                return new SpacerElement();
            }
        }
        else
        {
            var path = sourceValue;
            if (!Path.IsPathRooted(path) && !string.IsNullOrWhiteSpace(_baseDirectory))
                path = Path.Combine(_baseDirectory!, path);

            image = new ImageElement(path);
        }

        if (node.Frame?.Width > 0)
            image.FixedWidth = node.Frame.Width;
        if (node.Frame?.Height > 0)
            image.FixedHeight = node.Frame.Height;

        image.Fit = ParseImageFit(node.Fit);
        return image;
    }

    private IElement BuildTable(SchemaNode node)
    {
        var table = new TableElement();
        var columns = ResolveTableColumns(node);

        foreach (var column in columns)
        {
            table.Columns.Add(new TableColumnDefinition { RelativeWidth = column.Width ?? 1 });

            var headerText = new TextElement(column.Header ?? column.Field ?? string.Empty);
            headerText.Style.Bold = true;
            if (TryParseTextAlignment(column.Align, out var headerAlign))
                headerText.Style.Alignment = headerAlign;

            table.HeaderCells.Add(new TableCell { Content = headerText, IsHeader = true });
        }

        foreach (var row in GetDataRows(node.DataSource))
        {
            foreach (var column in columns)
            {
                var value = ResolveExpression($"row.{column.Field}", row);
                var text = new TextElement(ToText(value));
                if (TryParseTextAlignment(column.Align, out var cellAlign))
                    text.Style.Alignment = cellAlign;
                table.DataCells.Add(new TableCell { Content = text });
            }
        }

        table.BorderWidth = 0.5f;
        table.BorderColor = ReportColor.LightGray;
        return table;
    }

    private IElement BuildRepeat(SchemaNode node)
    {
        var definition = ResolveRepeatable(node.DefinitionRef, "repeat");
        var template = node.ItemTemplate ?? definition?.ItemTemplate ?? string.Empty;
        var spacing = node.ItemGap ?? definition?.ItemGap ?? 0;

        var items = GetDataRows(node.DataSource ?? definition?.DataSource)
            .Select(row => (IElement)new TextElement(ResolveTemplate(template, row)))
            .ToList();

        return new ListElement(items, spacing);
    }

    private IElement? BuildGroupInstance(SchemaNode node)
    {
        if (string.IsNullOrWhiteSpace(node.GroupRef) || !_groups.TryGetValue(node.GroupRef, out var group))
            return null;

        var region = new RegionNode { Nodes = group.Nodes ?? [] };
        return BuildRegion(region);
    }

    private List<TableColumnNode> ResolveTableColumns(SchemaNode node)
    {
        if (node.Columns is { Count: > 0 })
            return node.Columns;

        var definition = ResolveRepeatable(node.DefinitionRef, "table");
        return definition?.Columns ?? [];
    }

    private RepeatableDefinitionNode? ResolveRepeatable(string? definitionRef, string expectedType)
    {
        if (string.IsNullOrWhiteSpace(definitionRef))
            return null;

        if (!_repeatables.TryGetValue(definitionRef, out var definition))
            return null;

        return string.Equals(definition.Type, expectedType, StringComparison.OrdinalIgnoreCase)
            ? definition
            : null;
    }

    private IEnumerable<Dictionary<string, object?>> GetDataRows(string? dataSourceName)
    {
        if (string.IsNullOrWhiteSpace(dataSourceName))
            return [];

        if (!_dataSources.TryGetValue(dataSourceName, out var rows))
            return [];

        return rows.Select(ToDictionary);
    }

    private static Dictionary<string, object?> ToDictionary(object row)
    {
        if (row is IDictionary<string, object> dict)
            return dict.ToDictionary<KeyValuePair<string, object>, string, object?>(
                k => k.Key,
                v => v.Value,
                StringComparer.OrdinalIgnoreCase);

        if (row is IDictionary<string, object?> nullableDict)
            return new Dictionary<string, object?>(nullableDict, StringComparer.OrdinalIgnoreCase);

        if (row is System.Collections.IDictionary rawDict)
        {
            var mapped = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in rawDict)
            {
                if (entry.Key is string key)
                    mapped[key] = entry.Value;
            }
            return mapped;
        }

        return row.GetType()
            .GetProperties()
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(row), StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveTemplate(string input, Dictionary<string, object?>? row)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.Contains("{{", StringComparison.Ordinal))
            return input;

        return TemplateRegex.Replace(input, match =>
        {
            var expr = match.Groups["expr"].Value;
            var value = ResolveExpression(expr, row);
            return ToText(value);
        });
    }

    private object? ResolveExpression(string expression, Dictionary<string, object?>? row)
    {
        var parts = expression.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var value = ResolveExpressionRoot(parts[0], row);

        for (var i = 1; i < parts.Length; i++)
            value = ApplyFilter(value, parts[i]);

        return value;
    }

    private object? ResolveExpressionRoot(string expression, Dictionary<string, object?>? row)
    {
        expression = expression.Trim();

        if (expression.StartsWith("parameters.", StringComparison.OrdinalIgnoreCase))
        {
            var key = expression["parameters.".Length..];
            return _parameters.TryGetValue(key, out var value) ? value : null;
        }

        if (expression.StartsWith("row.", StringComparison.OrdinalIgnoreCase))
        {
            var key = expression["row.".Length..];
            return ResolvePath(row, key);
        }

        if ((expression.StartsWith('"') && expression.EndsWith('"')) ||
            (expression.StartsWith('\'') && expression.EndsWith('\'')))
        {
            return expression[1..^1];
        }

        if (decimal.TryParse(expression, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            return number;

        return expression;
    }

    private static object? ResolvePath(Dictionary<string, object?>? source, string key)
    {
        if (source is null)
            return null;

        object? current = source;
        var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var segment in segments)
        {
            if (current is IDictionary<string, object?> dictNullable)
            {
                dictNullable.TryGetValue(segment, out current);
                continue;
            }

            if (current is IDictionary<string, object> dict)
            {
                dict.TryGetValue(segment, out current);
                continue;
            }

            var prop = current?.GetType().GetProperty(segment);
            current = prop?.GetValue(current);
        }

        return current;
    }

    private static object? ApplyFilter(object? value, string filter)
    {
        filter = filter.Trim();
        if (filter.Equals("upper", StringComparison.OrdinalIgnoreCase))
            return ToText(value).ToUpperInvariant();
        if (filter.Equals("lower", StringComparison.OrdinalIgnoreCase))
            return ToText(value).ToLowerInvariant();
        if (filter.Equals("trim", StringComparison.OrdinalIgnoreCase))
            return ToText(value).Trim();
        if (filter.Equals("currency", StringComparison.OrdinalIgnoreCase))
        {
            if (decimal.TryParse(ToText(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var money))
                return money.ToString("C", CultureInfo.CurrentCulture);
            return ToText(value);
        }

        if (filter.StartsWith("number(", StringComparison.OrdinalIgnoreCase) && filter.EndsWith(')'))
        {
            var format = filter[7..^1].Trim().Trim('"', '\'');
            if (decimal.TryParse(ToText(value), NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                return number.ToString(format, CultureInfo.InvariantCulture);
            return ToText(value);
        }

        if (filter.StartsWith("date(", StringComparison.OrdinalIgnoreCase) && filter.EndsWith(')'))
        {
            var format = filter[5..^1].Trim().Trim('"', '\'');
            if (DateTime.TryParse(ToText(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date.ToString(format, CultureInfo.InvariantCulture);
            return ToText(value);
        }

        return value;
    }

    private static string ToText(object? value)
        => value is null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private TextStyle ResolveTextStyle(SchemaNode node)
    {
        var style = new TextStyle();

        if (!string.IsNullOrWhiteSpace(node.StyleRef) && _styles.TryGetValue(node.StyleRef, out var styleRef))
            ApplyTextStyle(style, styleRef);

        if (node.FontSize.HasValue) style.FontSize = node.FontSize.Value;
        if (!string.IsNullOrWhiteSpace(node.FontFamily)) style.FontFamily = node.FontFamily;
        if (node.Bold.HasValue) style.Bold = node.Bold.Value;
        if (node.Italic.HasValue) style.Italic = node.Italic.Value;
        if (node.Underline.HasValue) style.Underline = node.Underline.Value;
        if (!string.IsNullOrWhiteSpace(node.Color)) style.Color = ParseColor(node.Color!, style.Color);
        if (node.LineSpacing.HasValue) style.LineSpacing = node.LineSpacing.Value;
        if (TryParseTextAlignment(node.Align, out var align)) style.Alignment = align;

        return style;
    }

    private static void ApplyTextStyle(TextStyle target, TextStyle source)
    {
        target.FontSize = source.FontSize;
        target.FontFamily = source.FontFamily;
        target.Bold = source.Bold;
        target.Italic = source.Italic;
        target.Underline = source.Underline;
        target.Color = source.Color;
        target.LineSpacing = source.LineSpacing;
        target.Alignment = source.Alignment;
    }

    private static void ApplyTextStyle(TextStyle target, TextStyleNode style)
    {
        if (style.FontSize.HasValue) target.FontSize = style.FontSize.Value;
        if (!string.IsNullOrWhiteSpace(style.FontFamily)) target.FontFamily = style.FontFamily;
        if (style.Bold.HasValue) target.Bold = style.Bold.Value;
        if (style.Italic.HasValue) target.Italic = style.Italic.Value;
        if (style.Underline.HasValue) target.Underline = style.Underline.Value;
        if (!string.IsNullOrWhiteSpace(style.Color)) target.Color = ParseColor(style.Color!, target.Color);
        if (style.LineSpacing.HasValue) target.LineSpacing = style.LineSpacing.Value;
        if (TryParseTextAlignment(style.Align, out var align)) target.Alignment = align;
    }

    private static bool TryParseTextAlignment(string? value, out TextAlignment alignment)
    {
        alignment = value?.Trim().ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            "justify" => TextAlignment.Justify,
            "left" => TextAlignment.Left,
            _ => TextAlignment.Left
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseHorizontalAlignment(string? value, out HorizontalAlignment alignment)
    {
        alignment = value?.Trim().ToLowerInvariant() switch
        {
            "center" => HorizontalAlignment.Center,
            "right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        return !string.IsNullOrWhiteSpace(value);
    }

    private static ReportColor ParseColor(string raw, ReportColor fallback)
    {
        try
        {
            return ReportColor.FromHex(raw);
        }
        catch
        {
            return fallback;
        }
    }

    private static ImageFit ParseImageFit(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            "cover" => ImageFit.Cover,
            "fill" => ImageFit.Fill,
            "fitwidth" => ImageFit.FitWidth,
            "fitheight" => ImageFit.FitHeight,
            _ => ImageFit.Contain
        };

    private static PageSize ResolvePageSize(string? size, string? orientation)
    {
        var pageSize = size?.Trim().ToUpperInvariant() switch
        {
            "A3" => PageSizes.A3,
            "A5" => PageSizes.A5,
            "LETTER" => PageSizes.Letter,
            "LEGAL" => PageSizes.Legal,
            _ => PageSizes.A4
        };

        return string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase)
            ? pageSize.Landscape()
            : pageSize;
    }
}
