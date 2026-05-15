using System.Globalization;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FluentReport.Schema;

/// <summary>
/// Converts a FluentReport schema (YAML or JSON) into equivalent C# <c>Document.Create()</c> fluent-API code.
/// The generated code is a best-effort starting point — data-binding loops are emitted as commented
/// stubs because the exact row type is only known at runtime.
/// </summary>
public static class SchemaCodeGenerator
{
    /// <summary>
    /// Generates C# fluent-API code from a YAML or JSON schema string.
    /// </summary>
    /// <param name="schema">Schema YAML or JSON content.</param>
    /// <param name="isJson"><c>true</c> to parse as JSON; <c>false</c> (default) to parse as YAML.</param>
    /// <returns>A C# source-code string.</returns>
    public static string GenerateCSharp(string schema, bool isJson = false)
    {
        ReportSchema parsed;

        if (isJson)
        {
            parsed = JsonSerializer.Deserialize<ReportSchema>(schema, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? throw new InvalidOperationException("Schema JSON could not be parsed.");
        }
        else
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            parsed = deserializer.Deserialize<ReportSchema>(schema)
                ?? throw new InvalidOperationException("Schema YAML could not be parsed.");
        }

        return Generate(parsed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Main generator
    // ─────────────────────────────────────────────────────────────────────────

    private static string Generate(ReportSchema schema)
    {
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using FluentReport;");
        sb.AppendLine("using FluentReport.Core;");
        sb.AppendLine();

        // Parameters preamble
        if (schema.Parameters is { Count: > 0 })
        {
            sb.AppendLine("// Parameters (fill in before calling Document.Create):");
            foreach (var (name, param) in schema.Parameters)
                sb.AppendLine($"// - {name} ({param.Type ?? "string"}{(param.Required == true ? ", required" : "")})");
            sb.AppendLine();
        }

        // Data sources preamble
        if (schema.DataSources is { Count: > 0 })
        {
            sb.AppendLine("// Data sources needed:");
            foreach (var (name, _) in schema.DataSources)
                sb.AppendLine($"// - {name}: IEnumerable<Dictionary<string, object>>");
            sb.AppendLine();
        }

        sb.AppendLine("var document = Document.Create(container =>");
        sb.AppendLine("{");

        foreach (var page in schema.Pages ?? [])
            EmitPage(sb, page, schema, "    ");

        sb.AppendLine("});");
        sb.AppendLine();
        sb.AppendLine("// Render:");
        sb.AppendLine("// byte[] pdf   = document.GeneratePdf();");
        sb.AppendLine("// byte[] xlsx  = document.GenerateExcel();");
        sb.AppendLine("// string html  = document.GenerateHtml();");

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Page
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitPage(StringBuilder sb, SchemaPageNode page, ReportSchema schema, string indent)
    {
        var pd = schema.PageDefaults;

        sb.AppendLine($"{indent}container.Page(page =>");
        sb.AppendLine($"{indent}{{");

        // Size
        var size        = page.Size ?? pd?.Size ?? "A4";
        var orientation = page.Orientation ?? pd?.Orientation ?? "portrait";
        var sizeConst   = GetPageSizeConst(size);

        if (string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine($"{indent}    page.Size(PageSizes.{sizeConst}.Landscape());");
        else
            sb.AppendLine($"{indent}    page.Size(PageSizes.{sizeConst});");

        // Margin
        var margin = page.Margin ?? pd?.Margin;
        if (margin is null)
        {
            sb.AppendLine($"{indent}    page.MarginAll(40);");
        }
        else if (AllEqual(margin.Top, margin.Right, margin.Bottom, margin.Left))
        {
            sb.AppendLine($"{indent}    page.MarginAll({Fmt(margin.Top ?? 40f)});");
        }
        else
        {
            sb.AppendLine($"{indent}    page.Margin(");
            sb.AppendLine($"{indent}        top:    {Fmt(margin.Top    ?? 40f)},");
            sb.AppendLine($"{indent}        right:  {Fmt(margin.Right  ?? 40f)},");
            sb.AppendLine($"{indent}        bottom: {Fmt(margin.Bottom ?? 40f)},");
            sb.AppendLine($"{indent}        left:   {Fmt(margin.Left   ?? 40f)});");
        }

        sb.AppendLine();

        // Regions
        var regions = page.Regions;
        if (regions is not null)
        {
            if (regions.Header  is { Nodes.Count: > 0 }) EmitRegion(sb, "Header",  regions.Header,  schema, indent + "    ");
            if (regions.Content is { Nodes.Count: > 0 }) EmitRegion(sb, "Content", regions.Content, schema, indent + "    ");
            if (regions.Footer  is { Nodes.Count: > 0 }) EmitRegion(sb, "Footer",  regions.Footer,  schema, indent + "    ");
        }

        sb.AppendLine($"{indent}}});");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Region
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitRegion(StringBuilder sb, string region, RegionNode node, ReportSchema schema, string indent)
    {
        var nodes = node.Nodes ?? [];
        if (nodes.Count == 0) return;

        if (nodes.Count == 1)
        {
            sb.Append($"{indent}page.{region}()");
            EmitNodeChain(sb, nodes[0], schema, indent + "    ");
            sb.AppendLine(";");
        }
        else
        {
            sb.AppendLine($"{indent}page.{region}().Column(col =>");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    col.Spacing(4);");

            foreach (var n in nodes)
            {
                sb.Append($"{indent}    col.Item()");
                EmitNodeChain(sb, n, schema, indent + "        ");
                sb.AppendLine(";");
            }

            sb.AppendLine($"{indent}}});");
        }

        sb.AppendLine();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Node chain (appends fluent calls starting with a dot)
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitNodeChain(StringBuilder sb, SchemaNode node, ReportSchema schema, string indent)
    {
        var style = ResolveStyle(node.StyleRef, schema);

        switch (node.Type?.ToLowerInvariant())
        {
            case "text":
                EmitTextNode(sb, node, style);
                break;

            case "line":
                sb.Append($".Line({Fmt(node.Thickness ?? 1f)}, {Q(node.Color ?? "#CCCCCC")})");
                break;

            case "spacer":
                sb.Append($".Spacer({Fmt(node.Size ?? 8f)})");
                break;

            case "pagebreak":
                sb.Append(".PageBreak()");
                break;

            case "image":
                EmitImageNode(sb, node, indent);
                break;

            case "table":
                EmitTableNode(sb, node, schema, indent);
                break;

            case "repeat":
                EmitRepeatNode(sb, node, indent);
                break;

            case "groupinstance":
                sb.Append($".Text(\"/* groupInstance: {node.GroupRef} */\")");
                break;

            default:
                sb.Append($".Text(\"/* unknown type: {node.Type} */\")");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Text node
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitTextNode(StringBuilder sb, SchemaNode node, TextStyleNode? style)
    {
        var hasRuns = node.Runs is { Count: > 0 };

        if (hasRuns)
        {
            // Dynamic text with page-number tokens
            sb.AppendLine(".Text(x =>");
            sb.AppendLine("        {");
            foreach (var run in node.Runs!)
            {
                if (!string.IsNullOrEmpty(run.Token))
                {
                    var method = run.Token.ToLowerInvariant() switch
                    {
                        "currentpage" => "x.CurrentPageNumber()",
                        "totalpages"  => "x.TotalPages()",
                        _             => $"x.Span(/* token:{run.Token} */\"\")",
                    };
                    sb.AppendLine($"            {method};");
                }
                else if (!string.IsNullOrEmpty(run.Value))
                {
                    sb.AppendLine($"            x.Span({Q(run.Value!)});");
                }
            }
            sb.Append("        })");
        }
        else
        {
            sb.Append($".Text({Q(node.Value ?? "")})");
        }

        // Style chain
        ApplyTextStyle(sb, node, style);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Image node
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitImageNode(StringBuilder sb, SchemaNode node, string indent)
    {
        var mode  = node.Source?.Mode  ?? "path";
        var value = node.Source?.Value ?? "";

        string bytesExpr = mode switch
        {
            "path"   => $"File.ReadAllBytes({Q(value)})",
            "base64" => $"Convert.FromBase64String({Q(value)})",
            _        => "Array.Empty<byte>() /* provide image bytes */",
        };

        var fit = node.Fit?.ToLowerInvariant() switch
        {
            "cover"     => "ImageScaling.Cover",
            "fill"      => "ImageScaling.Fill",
            "fitheight" => "ImageScaling.FitHeight",
            _           => "ImageScaling.FitWidth",
        };

        sb.AppendLine();
        sb.AppendLine($"{indent}.Image({bytesExpr}, {fit})");
        sb.Append($"{indent}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Table node
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitTableNode(StringBuilder sb, SchemaNode node, ReportSchema schema, string indent)
    {
        var columns = node.Columns ?? [];
        var ds      = node.DataSource ?? "dataSource";

        sb.AppendLine();
        sb.AppendLine($"{indent}.Table(table =>");
        sb.AppendLine($"{indent}{{");

        // Column widths
        sb.AppendLine($"{indent}    table.ColumnsDefinition(cols =>");
        sb.AppendLine($"{indent}    {{");
        foreach (var col in columns)
            sb.AppendLine($"{indent}        cols.RelativeColumn({Fmt(col.Width ?? 1f)});");
        sb.AppendLine($"{indent}    }});");
        sb.AppendLine();

        // Header row
        if (columns.Count > 0)
        {
            sb.AppendLine($"{indent}    table.Header(h =>");
            sb.AppendLine($"{indent}    {{");
            foreach (var col in columns)
            {
                var headerText = col.Header ?? col.Field ?? "";
                sb.AppendLine($"{indent}        h.Cell().Text({Q(headerText)}).Bold();");
            }
            sb.AppendLine($"{indent}    }});");
            sb.AppendLine();
        }

        // Data rows stub
        sb.AppendLine($"{indent}    // foreach (var row in {ds})");
        sb.AppendLine($"{indent}    // {{");
        foreach (var col in columns)
        {
            var alignChain = (col.Align?.ToLowerInvariant()) switch
            {
                "right"  => ".AlignRight()",
                "center" => ".AlignCenter()",
                _        => "",
            };
            sb.AppendLine($"{indent}    //     table.Cell(){alignChain}.Text(row[\"{col.Field ?? "field"}\"]?.ToString() ?? \"\");");
        }
        sb.AppendLine($"{indent}    // }}");

        if (node.BorderWidth is not null)
        {
            sb.AppendLine($"{indent}}})");
            sb.Append($"{indent}.BorderEachCell({Fmt(node.BorderWidth.Value)}, {Q(node.BorderColor ?? "#CCCCCC")})");
        }
        else
        {
            sb.Append($"{indent}}})");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Repeat node
    // ─────────────────────────────────────────────────────────────────────────

    private static void EmitRepeatNode(StringBuilder sb, SchemaNode node, string indent)
    {
        var ds       = node.DataSource  ?? "dataSource";
        var template = node.ItemTemplate ?? "{{row.field}}";
        var gap      = node.ItemGap     ?? 4f;

        sb.AppendLine();
        sb.AppendLine($"{indent}// Repeat block – source: '{ds}', itemTemplate: \"{template}\", gap: {Fmt(gap)}");
        sb.AppendLine($"{indent}// .Column(col =>");
        sb.AppendLine($"{indent}// {{");
        sb.AppendLine($"{indent}//     col.Spacing({Fmt(gap)});");
        sb.AppendLine($"{indent}//     foreach (var row in {ds})");
        sb.AppendLine($"{indent}//         col.Item().Text(row[\"field\"]?.ToString() ?? \"\");");
        sb.AppendLine($"{indent}// }})");
        sb.Append($"{indent}.Text(\"/* repeat: {ds} */\")");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Style helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void ApplyTextStyle(StringBuilder sb, SchemaNode node, TextStyleNode? style)
    {
        var fontSize   = node.FontSize   ?? style?.FontSize;
        var bold       = node.Bold       ?? style?.Bold;
        var italic     = node.Italic     ?? style?.Italic;
        var underline  = node.Underline  ?? style?.Underline;
        var color      = node.Color      ?? style?.Color;
        var fontFamily = node.FontFamily ?? style?.FontFamily;
        var align      = node.Align      ?? style?.Align;

        if (fontSize   is not null)     sb.Append($".FontSize({Fmt(fontSize.Value)})");
        if (bold       == true)         sb.Append(".Bold()");
        if (italic     == true)         sb.Append(".Italic()");
        if (underline  == true)         sb.Append(".Underline()");
        if (color      is not null)     sb.Append($".Color({Q(color)})");
        if (fontFamily is not null)     sb.Append($".FontFamily({Q(fontFamily)})");
        if (align      is not null)     sb.Append($".{AlignMethod(align)}");
    }

    private static TextStyleNode? ResolveStyle(string? styleRef, ReportSchema schema)
    {
        if (styleRef is null || schema.Styles is null) return null;
        schema.Styles.TryGetValue(styleRef, out var style);
        return style;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Formatting helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string GetPageSizeConst(string? size) => size?.ToUpperInvariant() switch
    {
        "A3"     => "A3",
        "A5"     => "A5",
        "LETTER" => "Letter",
        "LEGAL"  => "Legal",
        _        => "A4",
    };

    private static string AlignMethod(string? align) => align?.ToLowerInvariant() switch
    {
        "center" => "AlignCenter()",
        "right"  => "AlignRight()",
        _        => "AlignLeft()",
    };

    private static bool AllEqual(float? a, float? b, float? c, float? d)
        => a.HasValue && a == b && b == c && c == d;

    private static string Fmt(float v)
        => v == MathF.Floor(v)
            ? ((int)v).ToString(CultureInfo.InvariantCulture)
            : v.ToString("G", CultureInfo.InvariantCulture);

    /// <summary>Returns a C# string literal (double-quoted, escaped).</summary>
    private static string Q(string s)
        => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
