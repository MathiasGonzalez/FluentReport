using FluentReport;

namespace FluentReport.Schema;

/// <summary>
/// Extension methods that add schema import capabilities to <see cref="Document"/>.
/// </summary>
public static class DocumentSchemaExtensions
{
    /// <summary>
    /// Extension overload that parses a schema file (.yaml/.yml/.json) and returns a <see cref="Document"/>.
    /// </summary>
    public static Document FromSchema(
        this Document document,
        string path,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        _ = document;
        return FromSchema(path, dataSources, parameters);
    }

    /// <summary>
    /// Parses a schema file (.yaml/.yml/.json) and returns a <see cref="Document"/>.
    /// </summary>
    public static Document FromSchema(
        string path,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new SchemaDocumentFactory(dataSources, parameters);
        return factory.ParseFromFile(path);
    }

    /// <summary>
    /// Parses a schema from a stream.
    /// </summary>
    /// <param name="stream">Schema stream.</param>
    /// <param name="format">Optional format hint: <c>yaml</c> or <c>json</c>. Defaults to <c>yaml</c>.</param>
    public static Document FromSchemaStream(
        this Document document,
        Stream stream,
        string? format = null,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        _ = document;
        return FromSchemaStream(stream, format, dataSources, parameters);
    }

    /// <summary>
    /// Parses a schema from a stream.
    /// </summary>
    /// <param name="stream">Schema stream.</param>
    /// <param name="format">Optional format hint: <c>yaml</c> or <c>json</c>. Defaults to <c>yaml</c>.</param>
    public static Document FromSchemaStream(
        Stream stream,
        string? format = null,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new SchemaDocumentFactory(dataSources, parameters);
        return factory.ParseFromStream(stream, format);
    }

    /// <summary>
    /// Parses a schema from a YAML string.
    /// </summary>
    public static Document FromSchemaYaml(
        this Document document,
        string yaml,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        _ = document;
        return FromSchemaYaml(yaml, dataSources, parameters);
    }

    /// <summary>
    /// Parses a schema from a YAML string.
    /// </summary>
    public static Document FromSchemaYaml(
        string yaml,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new SchemaDocumentFactory(dataSources, parameters);
        return factory.ParseFromYaml(yaml);
    }

    /// <summary>
    /// Parses a schema from a JSON string.
    /// </summary>
    public static Document FromSchemaJson(
        this Document document,
        string json,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        _ = document;
        return FromSchemaJson(json, dataSources, parameters);
    }

    /// <summary>
    /// Parses a schema from a JSON string.
    /// </summary>
    public static Document FromSchemaJson(
        string json,
        IDictionary<string, IEnumerable<object>>? dataSources = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new SchemaDocumentFactory(dataSources, parameters);
        return factory.ParseFromJson(json);
    }

    /// <summary>
    /// Validates a schema YAML string without rendering a document.
    /// Returns structured <see cref="ValidationResult"/> instead of throwing exceptions.
    /// </summary>
    public static ValidationResult ValidateSchema(string yaml)
        => SchemaValidator.Validate(yaml);

    /// <summary>
    /// Validates a schema JSON string without rendering a document.
    /// </summary>
    public static ValidationResult ValidateSchemaJson(string json)
        => SchemaValidator.ValidateJson(json);

    /// <summary>
    /// Converts a FluentReport schema (YAML or JSON) to equivalent C# <c>Document.Create()</c>
    /// fluent-API code. Useful for migrating a declarative schema to programmatic code.
    /// </summary>
    /// <param name="schema">Schema YAML or JSON content.</param>
    /// <param name="format">
    /// Optional format hint: <c>"yaml"</c> or <c>"json"</c>.
    /// When omitted the format is auto-detected from the first character of the content.
    /// </param>
    /// <returns>A C# source-code string.</returns>
    public static string ToFluentCSharp(string schema, string? format = null)
    {
        bool isJson = !string.IsNullOrWhiteSpace(format)
            ? string.Equals(format.Trim(), "json", StringComparison.OrdinalIgnoreCase)
            : schema.TrimStart() is { Length: > 0 } t && (t[0] == '{' || t[0] == '[');

        return SchemaCodeGenerator.GenerateCSharp(schema, isJson);
    }
}
