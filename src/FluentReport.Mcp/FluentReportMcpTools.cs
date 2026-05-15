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
            new { type = "table", description = "Renders a data table. Key properties: dataSource, columns (array of {field, header, width, align}), definitionRef, cellBorderWidth, cellBorderColor (per-cell grid lines), padding, background. Note: borderWidth/borderColor apply an outer container border around the whole table, not cell borders." },
            new { type = "repeat", description = "Renders repeated items. Key properties: dataSource, itemTemplate (with {{row.field}} bindings), itemGap (float), definitionRef." },
            new { type = "groupInstance", description = "Inserts a reusable group from definitions.groups. Key property: groupRef." },
        };

        return JsonSerializer.Serialize(types, JsonOut);
    }

    // ────────────────────────────────────────────────────────────────────────
    // schema_to_csharp
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "schema_to_csharp")]
    [Description("Converts a FluentReport YAML or JSON schema to equivalent C# Document.Create() fluent-API code. Returns a ready-to-compile C# snippet. Useful when migrating a declarative schema to programmatic code, or when an AI agent has derived a schema from an image or description and wants the C# equivalent.")]
    public static string SchemaToCSharp(string schema, string? format = null)
    {
        try
        {
            return DocumentSchemaExtensions.ToFluentCSharp(schema, format);
        }
        catch (Exception ex)
        {
            return ErrorJson(ex);
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // get_schema_template
    // ────────────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "get_schema_template")]
    [Description("Returns a ready-to-use FluentReport YAML schema template. useCase values: minimal (default), invoice, table_report, factura_uy (Uruguayan e-invoice / CFE), recibo_sueldo_uy (Uruguayan payslip / MTSS), remito_uy (Uruguayan delivery note / DGI), recibo_pago_uy (Uruguayan payment receipt).")]
    public static string GetSchemaTemplate(string? useCase = null)
    {
        return (useCase ?? "minimal").Trim().ToLowerInvariant() switch
        {
            "invoice"          => InvoiceTemplate(),
            "table_report"     => TableReportTemplate(),
            "factura_uy"       => FacturaUyTemplate(),
            "recibo_sueldo_uy" => ReciboSueldoUyTemplate(),
            "remito_uy"        => RemitoUyTemplate(),
            "recibo_pago_uy"   => ReciboPagoUyTemplate(),
            _                  => MinimalTemplate()
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

    // ────────────────────────────────────────────────────────────────────────
    // Uruguayan document templates
    // ────────────────────────────────────────────────────────────────────────

    private static string FacturaUyTemplate() => """
        kind: FluentReport
        schemaVersion: 1
        name: factura-electronica-uy

        pageDefaults:
          size: A4
          orientation: portrait
          margin:
            top: 40
            right: 40
            bottom: 50
            left: 40

        parameters:
          emisor_nombre:
            type: string
            required: true
          emisor_nombre_comercial:
            type: string
          emisor_rut:
            type: string
            required: true
          emisor_domicilio:
            type: string
          tipo_documento:
            type: string
            required: true
          serie_numero:
            type: string
            required: true
          fecha_emision:
            type: string
            required: true
          receptor_nombre:
            type: string
            required: true
          receptor_rut:
            type: string
          receptor_direccion:
            type: string
          subtotal:
            type: string
          iva_10:
            type: string
          iva_22:
            type: string
          total:
            type: string

        dataSources:
          lineas:
            type: array

        styles:
          heading:
            fontSize: 13
            bold: true
          subheading:
            fontSize: 11
            bold: true
          label:
            fontSize: 9
            bold: true
            color: "#444444"
          body:
            fontSize: 9
          small:
            fontSize: 8
            color: "#444444"
          docBox:
            fontSize: 13
            bold: true
            color: "#003366"
            align: center
          totalLine:
            fontSize: 11
            bold: true
          legal:
            fontSize: 7
            color: "#444444"
            align: center

        pages:
          - regions:
              header:
                nodes:
                  - type: text
                    value: "{{parameters.emisor_nombre}}"
                    styleRef: heading

                  - type: text
                    value: "{{parameters.emisor_nombre_comercial}}"
                    styleRef: subheading

                  - type: text
                    value: "RUT: {{parameters.emisor_rut}}  |  {{parameters.emisor_domicilio}}"
                    styleRef: small

                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: "{{parameters.tipo_documento}}  N°: {{parameters.serie_numero}}"
                    styleRef: docBox

                  - type: text
                    value: "Fecha de emisión: {{parameters.fecha_emision}}"
                    styleRef: label

              content:
                nodes:
                  - type: text
                    value: Receptor
                    styleRef: subheading

                  - type: text
                    value: "Razón social: {{parameters.receptor_nombre}}"
                    styleRef: body

                  - type: text
                    value: "RUT: {{parameters.receptor_rut}}  |  {{parameters.receptor_direccion}}"
                    styleRef: body

                  - type: spacer
                    size: 8

                  - type: text
                    value: Detalle
                    styleRef: subheading

                  - type: table
                    dataSource: lineas
                    cellBorderWidth: 0.5
                    cellBorderColor: "#DDDDDD"
                    columns:
                      - field: cant
                        header: Cant.
                        width: 1
                        align: center
                      - field: descripcion
                        header: Descripción
                        width: 4
                      - field: precio_unitario
                        header: P. Unitario
                        width: 1.5
                        align: right
                      - field: iva
                        header: IVA
                        width: 0.8
                        align: center
                      - field: total
                        header: Total
                        width: 1.5
                        align: right

                  - type: spacer
                    size: 8

                  - type: text
                    value: "Subtotal (sin IVA): {{parameters.subtotal}}"
                    styleRef: body
                    align: right

                  - type: text
                    value: "IVA 10%: {{parameters.iva_10}}"
                    styleRef: body
                    align: right

                  - type: text
                    value: "IVA 22%: {{parameters.iva_22}}"
                    styleRef: body
                    align: right

                  - type: line
                    thickness: 1
                    color: "#003366"

                  - type: text
                    value: "TOTAL: {{parameters.total}}"
                    styleRef: totalLine
                    align: right

              footer:
                nodes:
                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: "Documento fiscal electrónico emitido conforme a la normativa CFE vigente de DGI Uruguay."
                    styleRef: legal

                  - type: text
                    styleRef: legal
                    align: center
                    runs:
                      - value: "Página "
                      - token: currentPage
                      - value: " de "
                      - token: totalPages
        """;

    private static string ReciboSueldoUyTemplate() => """
        kind: FluentReport
        schemaVersion: 1
        name: recibo-sueldo-uy

        pageDefaults:
          size: A4
          orientation: portrait
          margin:
            top: 40
            right: 40
            bottom: 50
            left: 40

        parameters:
          emp_nombre:
            type: string
            required: true
          emp_rut:
            type: string
            required: true
          emp_domicilio:
            type: string
          trab_nombre:
            type: string
            required: true
          trab_ci:
            type: string
          trab_cargo:
            type: string
          trab_legajo:
            type: string
          trab_bps:
            type: string
          periodo_desc:
            type: string
            required: true
          periodo_from:
            type: string
          periodo_to:
            type: string
          fecha_pago:
            type: string
            required: true
          total_haberes:
            type: string
            required: true
          total_descuentos:
            type: string
            required: true
          neto_liquidar:
            type: string
            required: true

        dataSources:
          haberes:
            type: array
          descuentos:
            type: array

        styles:
          heading:
            fontSize: 13
            bold: true
          label:
            fontSize: 9
            bold: true
            color: "#444444"
          body:
            fontSize: 9
          small:
            fontSize: 8
            color: "#444444"
          sectionTitle:
            fontSize: 10
            bold: true
            color: "#003366"
          netoPay:
            fontSize: 14
            bold: true
            color: "#003366"
            align: right
          legal:
            fontSize: 7
            color: "#444444"
            align: center

        pages:
          - regions:
              header:
                nodes:
                  - type: text
                    value: "{{parameters.emp_nombre}}"
                    styleRef: heading

                  - type: text
                    value: "RUT: {{parameters.emp_rut}}  |  {{parameters.emp_domicilio}}"
                    styleRef: small

                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: RECIBO DE SUELDO
                    styleRef: sectionTitle
                    align: center

                  - type: text
                    value: "Período: {{parameters.periodo_desc}}  |  Pago: {{parameters.fecha_pago}}"
                    styleRef: label
                    align: center

              content:
                nodes:
                  - type: text
                    value: Datos del Trabajador
                    styleRef: sectionTitle

                  - type: text
                    value: "Nombre: {{parameters.trab_nombre}}"
                    styleRef: body

                  - type: text
                    value: "C.I.: {{parameters.trab_ci}}  |  Cargo: {{parameters.trab_cargo}}  |  Legajo: {{parameters.trab_legajo}}  |  BPS: {{parameters.trab_bps}}"
                    styleRef: small

                  - type: spacer
                    size: 8

                  - type: text
                    value: Haberes
                    styleRef: sectionTitle

                  - type: table
                    dataSource: haberes
                    cellBorderWidth: 0.5
                    cellBorderColor: "#DDDDDD"
                    columns:
                      - field: concepto
                        header: Concepto
                        width: 4
                      - field: monto
                        header: Monto
                        width: 2
                        align: right

                  - type: spacer
                    size: 6

                  - type: text
                    value: Descuentos
                    styleRef: sectionTitle

                  - type: table
                    dataSource: descuentos
                    cellBorderWidth: 0.5
                    cellBorderColor: "#DDDDDD"
                    columns:
                      - field: concepto
                        header: Concepto
                        width: 4
                      - field: tasa
                        header: Tasa
                        width: 1
                        align: center
                      - field: monto
                        header: Monto
                        width: 2
                        align: right

                  - type: spacer
                    size: 8

                  - type: line
                    thickness: 1
                    color: "#003366"

                  - type: text
                    value: "Total haberes: {{parameters.total_haberes}}"
                    styleRef: body
                    align: right

                  - type: text
                    value: "Total descuentos: {{parameters.total_descuentos}}"
                    styleRef: body
                    align: right

                  - type: text
                    value: "NETO A LIQUIDAR: {{parameters.neto_liquidar}}"
                    styleRef: netoPay

              footer:
                nodes:
                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: "Recibo obligatorio conforme al Decreto-Ley 14.188 (MTSS). Retenciones BPS y FONASA según tasas vigentes."
                    styleRef: legal

                  - type: text
                    value: "Conforme: _______________________ (firma trabajador)"
                    styleRef: legal
                    align: right
        """;

    private static string RemitoUyTemplate() => """
        kind: FluentReport
        schemaVersion: 1
        name: remito-entrega-uy

        pageDefaults:
          size: A4
          orientation: portrait
          margin:
            top: 40
            right: 40
            bottom: 50
            left: 40

        parameters:
          numero:
            type: string
            required: true
          fecha:
            type: string
            required: true
          hora:
            type: string
          remitente_nombre:
            type: string
            required: true
          remitente_rut:
            type: string
            required: true
          remitente_domicilio:
            type: string
          destinatario_nombre:
            type: string
            required: true
          destinatario_rut:
            type: string
          destinatario_domicilio:
            type: string
          lugar_entrega:
            type: string
          transportista:
            type: string

        dataSources:
          items:
            type: array

        styles:
          heading:
            fontSize: 13
            bold: true
          label:
            fontSize: 9
            bold: true
            color: "#444444"
          body:
            fontSize: 9
          small:
            fontSize: 8
            color: "#444444"
          sectionTitle:
            fontSize: 10
            bold: true
            color: "#003366"
          docNumber:
            fontSize: 13
            bold: true
            color: "#003366"
            align: center
          legal:
            fontSize: 7
            color: "#444444"
            align: center

        pages:
          - regions:
              header:
                nodes:
                  - type: text
                    value: "{{parameters.remitente_nombre}}"
                    styleRef: heading

                  - type: text
                    value: "RUT: {{parameters.remitente_rut}}  |  {{parameters.remitente_domicilio}}"
                    styleRef: small

                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: REMITO DE ENTREGA
                    styleRef: docNumber

                  - type: text
                    value: "Nº: {{parameters.numero}}  |  Fecha: {{parameters.fecha}}  Hora: {{parameters.hora}}"
                    styleRef: label
                    align: center

              content:
                nodes:
                  - type: text
                    value: Destinatario
                    styleRef: sectionTitle

                  - type: text
                    value: "Razón social: {{parameters.destinatario_nombre}}"
                    styleRef: body

                  - type: text
                    value: "RUT: {{parameters.destinatario_rut}}  |  {{parameters.destinatario_domicilio}}"
                    styleRef: body

                  - type: text
                    value: "Lugar de entrega: {{parameters.lugar_entrega}}"
                    styleRef: body

                  - type: text
                    value: "Transportista: {{parameters.transportista}}"
                    styleRef: body

                  - type: spacer
                    size: 8

                  - type: text
                    value: Artículos
                    styleRef: sectionTitle

                  - type: table
                    dataSource: items
                    cellBorderWidth: 0.5
                    cellBorderColor: "#DDDDDD"
                    columns:
                      - field: cantidad
                        header: Cantidad
                        width: 1
                        align: center
                      - field: unidad
                        header: Unidad
                        width: 1
                        align: center
                      - field: descripcion
                        header: Descripción
                        width: 5
                      - field: observaciones
                        header: Obs.
                        width: 2

                  - type: spacer
                    size: 24

                  - type: text
                    value: "Firma remitente: _______________________        Firma receptor: _______________________"
                    styleRef: small

              footer:
                nodes:
                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: "Documento requerido por DGI para bienes en tránsito – Resolución DGI Nº 2.530/991."
                    styleRef: legal

                  - type: text
                    styleRef: legal
                    align: center
                    runs:
                      - value: "Página "
                      - token: currentPage
                      - value: " de "
                      - token: totalPages
        """;

    private static string ReciboPagoUyTemplate() => """
        kind: FluentReport
        schemaVersion: 1
        name: recibo-pago-uy

        pageDefaults:
          size: A4
          orientation: portrait
          margin:
            top: 40
            right: 40
            bottom: 50
            left: 40

        parameters:
          numero:
            type: string
            required: true
          fecha:
            type: string
            required: true
          benef_nombre:
            type: string
            required: true
          benef_rut:
            type: string
            required: true
          benef_domicilio:
            type: string
          pagador_nombre:
            type: string
            required: true
          pagador_rut:
            type: string
          concepto:
            type: string
            required: true
          monto_cifra:
            type: string
            required: true
          monto_letras:
            type: string
            required: true
          moneda:
            type: string
          forma_pago:
            type: string
          cuenta:
            type: string

        styles:
          heading:
            fontSize: 13
            bold: true
          label:
            fontSize: 9
            bold: true
            color: "#444444"
          body:
            fontSize: 9
          small:
            fontSize: 8
            color: "#444444"
          sectionTitle:
            fontSize: 10
            bold: true
            color: "#003366"
          amount:
            fontSize: 22
            bold: true
            color: "#003366"
            align: center
          legal:
            fontSize: 7
            color: "#444444"
            align: center

        pages:
          - regions:
              header:
                nodes:
                  - type: text
                    value: "{{parameters.benef_nombre}}"
                    styleRef: heading

                  - type: text
                    value: "RUT: {{parameters.benef_rut}}  |  {{parameters.benef_domicilio}}"
                    styleRef: small

                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: RECIBO DE PAGO
                    styleRef: sectionTitle
                    align: center

                  - type: text
                    value: "Nº: {{parameters.numero}}  |  Fecha: {{parameters.fecha}}"
                    styleRef: label
                    align: center

              content:
                nodes:
                  - type: text
                    value: "Recibí de:"
                    styleRef: sectionTitle

                  - type: text
                    value: "{{parameters.pagador_nombre}}"
                    styleRef: body

                  - type: text
                    value: "RUT: {{parameters.pagador_rut}}"
                    styleRef: body

                  - type: spacer
                    size: 8

                  - type: text
                    value: "{{parameters.monto_cifra}}"
                    styleRef: amount

                  - type: text
                    value: "({{parameters.monto_letras}}) – {{parameters.moneda}}"
                    styleRef: body
                    align: center

                  - type: spacer
                    size: 8

                  - type: text
                    value: "En concepto de:"
                    styleRef: label

                  - type: text
                    value: "{{parameters.concepto}}"
                    styleRef: body

                  - type: spacer
                    size: 6

                  - type: text
                    value: "Forma de pago: {{parameters.forma_pago}}"
                    styleRef: body

                  - type: text
                    value: "{{parameters.cuenta}}"
                    styleRef: body

                  - type: spacer
                    size: 24

                  - type: text
                    value: "Firma: _______________________"
                    styleRef: small
                    align: right

                  - type: text
                    value: "{{parameters.benef_nombre}}"
                    styleRef: small
                    align: right

              footer:
                nodes:
                  - type: line
                    thickness: 0.5
                    color: "#AAAAAA"

                  - type: text
                    value: "El presente recibo cancela la obligación indicada en el concepto (Código de Comercio, Ley 16.060)."
                    styleRef: legal
        """;
}
