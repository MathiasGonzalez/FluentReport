using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentReport;
using FluentReport.Html;
using FluentReport.Excel;
using FluentReport.Schema;
using ModelContextProtocol.Server;

namespace FluentReport.Mcp;

/// <summary>
/// MCP tools exposed to AI coding agents for report generation and schema authoring.
/// </summary>
[McpServerToolType]
public sealed class FluentReportMcpTools
{
    private static readonly JsonSerializerOptions JsonOut = new()
    {
        WriteIndented         = true,
        PropertyNamingPolicy  = JsonNamingPolicy.CamelCase
    };

    // ────────────────────────────────────────────────────────────────────────
    // validate_schema
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "validate_schema")]
    [Description("Validates a FluentReport schema YAML or JSON without rendering. Returns JSON with isValid, errors (code/message/path), and warnings. Use this before render_to_* to get actionable feedback.")]
    public static string ValidateSchema(string schema, string? format = null)
    {
        try
        {
            var result = IsJsonFormat(format, schema)
                ? DocumentSchemaExtensions.ValidateSchemaJson(schema)
                : DocumentSchemaExtensions.ValidateSchema(schema);

            return JsonSerializer.Serialize(new
            {
                result.IsValid,
                Errors   = result.Errors.Select(e => new { e.Code, e.Message, e.Path }),
                Warnings = result.Warnings
            }, JsonOut);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                IsValid  = false,
                Errors   = new[] { new { Code = "internal_error", Message = ex.Message, Path = "" } },
                Warnings = Array.Empty<string>()
            }, JsonOut);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // render_to_pdf
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "render_to_pdf")]
    [Description("Renders a FluentReport schema to PDF. Returns base64-encoded PDF bytes. dataSources format: {\"dsName\": [{\"field\": \"value\"}]}. parameters format: {\"key\": \"value\"}.")]
    public static string RenderToPdf(
        string  schema,
        string? format      = null,
        string? dataSources = null,
        string? parameters  = null)
    {
        try
        {
            var doc = ParseDocument(schema, format, dataSources, parameters);
            using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            return ErrorJson(ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // render_to_html
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "render_to_html")]
    [Description("Renders a FluentReport schema to HTML. Returns HTML string (not base64). Set fragment=true for a <div> without <html>/<body> wrapper.")]
    public static string RenderToHtml(
        string  schema,
        string? format      = null,
        string? dataSources = null,
        string? parameters  = null,
        bool    fragment    = false)
    {
        try
        {
            var doc = ParseDocument(schema, format, dataSources, parameters);
            return fragment ? doc.GenerateHtmlFragment() : doc.GenerateHtml();
        }
        catch (Exception ex)
        {
            return ErrorJson(ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // render_to_excel
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "render_to_excel")]
    [Description("Renders a FluentReport schema to Excel (.xlsx). Returns base64-encoded XLSX bytes.")]
    public static string RenderToExcel(
        string  schema,
        string? format      = null,
        string? dataSources = null,
        string? parameters  = null)
    {
        try
        {
            var doc = ParseDocument(schema, format, dataSources, parameters);
            using var ms = new MemoryStream();
            doc.GenerateExcel(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            return ErrorJson(ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // list_node_types
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "list_node_types")]
    [Description("Returns JSON documentation for every supported FluentReport node type and their properties. Use this to discover available options when authoring schemas.")]
    public static string ListNodeTypes()
    {
        var types = new[]
        {
            new { type = "text", description = "Renders a text block. Key properties: value (string), styleRef, align (left|center|right), fontSize, bold, italic, underline, color (#hex), fontFamily, lineSpacing, runs (array of {value,token} for page numbers), padding, paddingTop/Bottom/Left/Right/Horizontal/Vertical, background (#hex), borderWidth, borderColor (#hex)." },
            new { type = "line", description = "Renders a horizontal rule. Key properties: thickness (float, default 1), color (#hex)." },
            new { type = "spacer", description = "Inserts blank vertical space. Key property: size (float, points)." },
            new { type = "pagebreak", description = "Forces a page break. No additional properties required." },
            new { type = "image", description = "Renders an image. Key properties: source.value (file path or base64 string), source.mode (path|base64|bytes), fit (cover|fill|fitwidth|fitheight|none), frame.width, frame.height." },
            new { type = "table", description = "Renders a data table. Key properties: dataSource, columns (array of {field, header, width, align}), definitionRef, borderWidth, borderColor, padding, background." },
            new { type = "repeat", description = "Renders repeated items. Key properties: dataSource, itemTemplate (with {{row.field}} bindings), itemGap (float), definitionRef." },
            new { type = "groupInstance", description = "Inserts a reusable group from definitions.groups. Key property: groupRef." },
        };

        return JsonSerializer.Serialize(types, JsonOut);
    }

    // ────────────────────────────────────────────────────────────────────────
    // get_schema_template
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "get_schema_template")]
    [Description("Returns a ready-to-use FluentReport YAML schema template. useCase: minimal (default), invoice, or table_report.")]
    public static string GetSchemaTemplate(string? useCase = null)
    {
        return (useCase ?? "minimal").Trim().ToLowerInvariant() switch
        {
            "invoice"       => InvoiceTemplate(),
            "table_report"  => TableReportTemplate(),
            _               => MinimalTemplate()
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static Document ParseDocument(
        string  schema,
        string? format,
        string? dataSources,
        string? parameters)
    {
        var ds     = ParseDataSources(dataSources);
        var params_ = ParseParameters(parameters);

        var factory = new SchemaDocumentFactory(ds, params_);

        return IsJsonFormat(format, schema)
            ? factory.ParseFromJson(schema)
            : factory.ParseFromYaml(schema);
    }

    private static bool IsJsonFormat(string? format, string schema)
    {
        if (!string.IsNullOrWhiteSpace(format))
            return string.Equals(format.Trim(), "json", StringComparison.OrdinalIgnoreCase);

        // auto-detect: JSON starts with { or [
        var trimmed = schema.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static Dictionary<string, IEnumerable<object>> ParseDataSources(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        var result = new Dictionary<string, IEnumerable<object>>(StringComparer.OrdinalIgnoreCase);
        var node   = JsonNode.Parse(json) as JsonObject
            ?? throw new ArgumentException("dataSources must be a JSON object.");

        foreach (var (key, value) in node)
        {
            if (value is not JsonArray arr) continue;
            result[key] = arr
                .Select(item => (object)(item?.ToJsonString() ?? "null"))
                .ToList();
        }

        // Re-parse each row as Dictionary<string,object?> for the factory
        var typed = new Dictionary<string, IEnumerable<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, _) in node)
        {
            if (node[key] is not JsonArray arr) continue;
            typed[key] = arr
                .Select(item => (object)ParseRowObject(item))
                .ToList();
        }

        return typed;
    }

    private static Dictionary<string, object?> ParseRowObject(JsonNode? node)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (node is not JsonObject obj) return dict;

        foreach (var (k, v) in obj)
        {
            dict[k] = v switch
            {
                JsonValue jv when jv.TryGetValue<string>(out var s)  => s,
                JsonValue jv when jv.TryGetValue<decimal>(out var d) => d,
                JsonValue jv when jv.TryGetValue<bool>(out var b)    => b,
                null => null,
                _    => v.ToJsonString()
            };
        }

        return dict;
    }

    private static Dictionary<string, object> ParseParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var node   = JsonNode.Parse(json) as JsonObject
            ?? throw new ArgumentException("parameters must be a JSON object.");

        foreach (var (key, value) in node)
        {
            result[key] = value switch
            {
                JsonValue jv when jv.TryGetValue<string>(out var s)  => s,
                JsonValue jv when jv.TryGetValue<decimal>(out var d) => d,
                JsonValue jv when jv.TryGetValue<bool>(out var b)    => b,
                null => string.Empty,
                _    => value.ToJsonString()
            };
        }

        return result;
    }

    private static string ErrorJson(Exception ex) =>
        JsonSerializer.Serialize(new
        {
            error   = true,
            message = ex.Message,
            type    = ex.GetType().Name
        }, JsonOut);

    // ────────────────────────────────────────────────────────────────────────
    // Templates
    // ────────────────────────────────────────────────────────────────────────

    private static string MinimalTemplate() => """
        kind: FluentReport
        schemaVersion: 1

        pages:
          - regions:
              content:
                nodes:
                  - type: text
                    value: Hello, World!
        """;

    private static string InvoiceTemplate() => """
        kind: FluentReport
        schemaVersion: 1

        styles:
          heading:
            fontSize: 18
            bold: true
          label:
            bold: true
            fontSize: 10
          small:
            fontSize: 9

        pages:
          - regions:
              header:
                nodes:
                  - type: text
                    value: "{{parameters.companyName}}"
                    styleRef: heading

              content:
                nodes:
                  - type: text
                    value: "Invoice No: {{parameters.invoiceNo}}"
                    styleRef: label

                  - type: spacer
                    size: 8

                  - type: table
                    dataSource: lines
                    columns:
                      - field: description
                        header: Description
                        width: 3
                      - field: qty
                        header: Qty
                        width: 1
                        align: center
                      - field: price
                        header: Unit Price
                        width: 1
                        align: right
                      - field: total
                        header: Total
                        width: 1
                        align: right

              footer:
                nodes:
                  - type: line
                    thickness: 0.5
                  - type: text
                    value: "Page "
                    runs:
                      - value: "Page "
                      - token: currentPage
                      - value: " of "
                      - token: totalPages
                    styleRef: small
                    align: center
        """;

    private static string TableReportTemplate() => """
        kind: FluentReport
        schemaVersion: 1

        styles:
          title:
            fontSize: 16
            bold: true
          subtitle:
            fontSize: 11
            color: "#555555"

        pages:
          - regions:
              content:
                nodes:
                  - type: text
                    value: "{{parameters.reportTitle}}"
                    styleRef: title

                  - type: text
                    value: "Generated: {{parameters.generatedAt}}"
                    styleRef: subtitle

                  - type: spacer
                    size: 12

                  - type: table
                    dataSource: rows
                    columns:
                      - field: col1
                        header: Column 1
                        width: 2
                      - field: col2
                        header: Column 2
                        width: 1
                        align: right
        """;
}
