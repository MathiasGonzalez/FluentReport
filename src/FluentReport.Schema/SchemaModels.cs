namespace FluentReport.Schema;

internal sealed class ReportSchema
{
    public string? Kind { get; set; }
    public int SchemaVersion { get; set; }
    public string? Name { get; set; }
    public MetadataNode? Metadata { get; set; }
    public PageDefaultsNode? PageDefaults { get; set; }
    public Dictionary<string, ParameterNode>? Parameters { get; set; }
    public Dictionary<string, DataSourceNode>? DataSources { get; set; }
    public object? Assets { get; set; }
    public Dictionary<string, TextStyleNode>? Styles { get; set; }
    public RendererOptionsNode? RendererOptions { get; set; }
    public DefinitionsNode? Definitions { get; set; }
    public List<SchemaPageNode>? Pages { get; set; }
}

internal sealed class MetadataNode
{
    public string? Title { get; set; }
}

internal sealed class ParameterNode
{
    public string? Type { get; set; }
    public bool? Required { get; set; }
}

internal sealed class DataSourceNode
{
    public string? Type { get; set; }
}

internal sealed class RendererOptionsNode
{
    public HtmlRendererOptionsNode? Html { get; set; }
}

internal sealed class HtmlRendererOptionsNode
{
    public int? MaxWidth { get; set; }
    public string? FontFamily { get; set; }
    public string? PageDividerStyle { get; set; }
    public bool? OutlookCompatible { get; set; }
}

internal sealed class PageDefaultsNode
{
    public string? Size { get; set; }
    public string? Orientation { get; set; }
    public MarginNode? Margin { get; set; }
}

internal sealed class MarginNode
{
    public float? Top { get; set; }
    public float? Right { get; set; }
    public float? Bottom { get; set; }
    public float? Left { get; set; }
}

internal sealed class DefinitionsNode
{
    public List<GroupDefinitionNode>? Groups { get; set; }
    public List<RepeatableDefinitionNode>? Repeatables { get; set; }
}

internal sealed class GroupDefinitionNode
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public FrameNode? Frame { get; set; }
    public List<SchemaNode>? Nodes { get; set; }
}

internal sealed class RepeatableDefinitionNode
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? DataSource { get; set; }
    public List<TableColumnNode>? Columns { get; set; }
    public string? ItemTemplate { get; set; }
    public float? ItemGap { get; set; }
    public string? GrowthMode { get; set; }
    public string? OverflowMode { get; set; }
    public bool? KeepTogether { get; set; }
}

internal sealed class SchemaPageNode
{
    public string? Id { get; set; }
    public string? Size { get; set; }
    public string? Orientation { get; set; }
    public MarginNode? Margin { get; set; }
    public PageRegionsNode? Regions { get; set; }
}

internal sealed class PageRegionsNode
{
    public RegionNode? Header { get; set; }
    public RegionNode? Content { get; set; }
    public RegionNode? Footer { get; set; }
}

internal sealed class RegionNode
{
    public FrameNode? Frame { get; set; }
    public List<SchemaNode>? Nodes { get; set; }
}

internal sealed class SchemaNode
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public FrameNode? Frame { get; set; }
    public int? ZIndex { get; set; }

    // text
    public string? Value { get; set; }
    public List<TextRunNode>? Runs { get; set; }
    public string? StyleRef { get; set; }
    public string? Align { get; set; }
    public float? FontSize { get; set; }
    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? Underline { get; set; }
    public string? Color { get; set; }
    public string? FontFamily { get; set; }
    public float? LineSpacing { get; set; }

    // line/spacer
    public float? Thickness { get; set; }
    public float? Size { get; set; }

    // image
    public ImageSourceNode? Source { get; set; }
    public string? Fit { get; set; }
    public string? Alt { get; set; }

    // table/repeat
    public string? Name { get; set; }
    public string? DataSource { get; set; }
    public string? DefinitionRef { get; set; }
    public List<TableColumnNode>? Columns { get; set; }
    public string? ItemTemplate { get; set; }
    public float? ItemGap { get; set; }
    public string? GrowthMode { get; set; }
    public string? OverflowMode { get; set; }
    public bool? KeepTogether { get; set; }

    // group instance
    public string? GroupRef { get; set; }
}

internal sealed class FrameNode
{
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Width { get; set; }
    public float? Height { get; set; }
}

internal sealed class TextRunNode
{
    public string? Value { get; set; }
    public string? Token { get; set; }
}

internal sealed class ImageSourceNode
{
    public string? Mode { get; set; }
    public string? Value { get; set; }
}

internal sealed class TableColumnNode
{
    public string? Field { get; set; }
    public string? Header { get; set; }
    public float? Width { get; set; }
    public string? Align { get; set; }
}

internal sealed class TextStyleNode
{
    public float? FontSize { get; set; }
    public string? FontFamily { get; set; }
    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? Underline { get; set; }
    public string? Color { get; set; }
    public string? Background { get; set; }
    public float? LineSpacing { get; set; }
    public string? Align { get; set; }
}
