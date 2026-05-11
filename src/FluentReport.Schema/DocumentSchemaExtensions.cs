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
}
