using System.Text.Json;
using FluentReport.Core;
using FluentReport.Styling;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FluentReport.Schema;

// ── Public result types ──────────────────────────────────────────────────────

/// <summary>A single structured validation issue found in a schema document.</summary>
public sealed record ValidationError(
    /// <summary>Machine-readable error code (e.g. <c>node_type_unknown</c>).</summary>
    string Code,
    /// <summary>Human-readable explanation of the issue.</summary>
    string Message,
    /// <summary>Dot/bracket path to the offending field (e.g. <c>pages[0].regions.content.nodes[2].styleRef</c>).</summary>
    string Path);

/// <summary>The result of a <see cref="SchemaValidator"/> run.</summary>
public sealed class ValidationResult
{
    internal ValidationResult(List<ValidationError> errors, List<string> warnings)
    {
        Errors   = errors;
        Warnings = warnings;
    }

    /// <summary><c>true</c> when no errors were found.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>List of blocking errors. An empty list means the schema is valid.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Non-blocking advisory messages (e.g. unknown property values that are silently ignored).</summary>
    public IReadOnlyList<string> Warnings { get; }
}

// ── Validator ────────────────────────────────────────────────────────────────

/// <summary>
/// Validates FluentReport schema YAML or JSON without rendering a document.
/// Returns structured <see cref="ValidationError"/> entries instead of throwing exceptions,
/// making it safe to call from AI agents that iterate on schema generation.
/// </summary>
public sealed class SchemaValidator
{
    private static readonly HashSet<string> KnownNodeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text", "line", "spacer", "pagebreak", "image", "table", "repeat", "groupinstance"
    };

    private static readonly HashSet<string> KnownImageModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "path", "base64", "bytes"
    };

    private static readonly HashSet<string> KnownFits = new(StringComparer.OrdinalIgnoreCase)
    {
        "cover", "fill", "fitwidth", "fitheight", "none"
    };

    private static readonly HashSet<string> KnownAlignments = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "center", "right"
    };

    // ── Entry points ────────────────────────────────────────────────────────

    /// <summary>Validates a schema from a YAML string.</summary>
    public static ValidationResult Validate(string yaml)
    {
        ReportSchema? schema;
        var errors   = new List<ValidationError>();
        var warnings = new List<string>();

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            schema = deserializer.Deserialize<ReportSchema>(yaml);
        }
        catch (Exception ex)
        {
            errors.Add(new("schema_parse_error", $"YAML parse failed: {ex.Message}", ""));
            return new ValidationResult(errors, warnings);
        }

        if (schema is null)
        {
            errors.Add(new("schema_parse_error", "YAML produced a null document.", ""));
            return new ValidationResult(errors, warnings);
        }

        ValidateSchema(schema, errors, warnings);
        return new ValidationResult(errors, warnings);
    }

    /// <summary>Validates a schema from a JSON string.</summary>
    public static ValidationResult ValidateJson(string json)
    {
        ReportSchema? schema;
        var errors   = new List<ValidationError>();
        var warnings = new List<string>();

        try
        {
            schema = JsonSerializer.Deserialize<ReportSchema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling         = JsonCommentHandling.Skip
            });
        }
        catch (Exception ex)
        {
            errors.Add(new("schema_parse_error", $"JSON parse failed: {ex.Message}", ""));
            return new ValidationResult(errors, warnings);
        }

        if (schema is null)
        {
            errors.Add(new("schema_parse_error", "JSON produced a null document.", ""));
            return new ValidationResult(errors, warnings);
        }

        ValidateSchema(schema, errors, warnings);
        return new ValidationResult(errors, warnings);
    }

    // ── Core validation ─────────────────────────────────────────────────────

    private static void ValidateSchema(
        ReportSchema schema,
        List<ValidationError> errors,
        List<string> warnings)
    {
        // kind
        if (!string.IsNullOrWhiteSpace(schema.Kind) &&
            !string.Equals(schema.Kind, "FluentReport", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new("kind_unsupported",
                $"Document kind '{schema.Kind}' is not supported. Expected 'FluentReport'.",
                "kind"));
        }

        // schemaVersion
        if (schema.SchemaVersion != 1)
        {
            errors.Add(new("schema_version_unsupported",
                $"Schema version '{schema.SchemaVersion}' is not supported. Supported version: 1.",
                "schemaVersion"));
        }

        // pages
        if (schema.Pages is null || schema.Pages.Count == 0)
        {
            errors.Add(new("pages_empty",
                "Schema must contain at least one page.",
                "pages"));
            return; // no point walking empty pages
        }

        // collect defined names for reference checks
        var styles = (schema.Styles ?? [])
            .Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groups = (schema.Definitions?.Groups ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g.Id))
            .Select(g => g.Id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var repeatables = (schema.Definitions?.Repeatables ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .ToDictionary(r => r.Id!, r => r, StringComparer.OrdinalIgnoreCase);

        // walk pages
        for (var pi = 0; pi < schema.Pages.Count; pi++)
        {
            var page = schema.Pages[pi];
            var pageBase = $"pages[{pi}]";

            if (page.Regions is not null)
            {
                ValidateRegion(page.Regions.Header,  $"{pageBase}.regions.header",  styles, groups, repeatables, errors, warnings);
                ValidateRegion(page.Regions.Content, $"{pageBase}.regions.content", styles, groups, repeatables, errors, warnings);
                ValidateRegion(page.Regions.Footer,  $"{pageBase}.regions.footer",  styles, groups, repeatables, errors, warnings);
            }
        }
    }

    private static void ValidateRegion(
        RegionNode? region,
        string path,
        HashSet<string> styles,
        HashSet<string> groups,
        Dictionary<string, RepeatableDefinitionNode> repeatables,
        List<ValidationError> errors,
        List<string> warnings)
    {
        if (region?.Nodes is null) return;

        for (var ni = 0; ni < region.Nodes.Count; ni++)
        {
            ValidateNode(region.Nodes[ni], $"{path}.nodes[{ni}]",
                styles, groups, repeatables, errors, warnings);
        }
    }

    private static void ValidateNode(
        SchemaNode node,
        string path,
        HashSet<string> styles,
        HashSet<string> groups,
        Dictionary<string, RepeatableDefinitionNode> repeatables,
        List<ValidationError> errors,
        List<string> warnings)
    {
        var type = (node.Type ?? "").Trim().ToLowerInvariant();

        // unknown node type
        if (!string.IsNullOrWhiteSpace(node.Type) && !KnownNodeTypes.Contains(type))
        {
            errors.Add(new("node_type_unknown",
                $"Unsupported node type '{node.Type}'.",
                $"{path}.type"));
        }
        else if (string.IsNullOrWhiteSpace(node.Type))
        {
            errors.Add(new("node_type_missing",
                "Node is missing a 'type' property.",
                $"{path}.type"));
        }

        // styleRef
        if (!string.IsNullOrWhiteSpace(node.StyleRef) && !styles.Contains(node.StyleRef))
        {
            errors.Add(new("style_ref_not_found",
                $"Style '{node.StyleRef}' is not defined in styles.",
                $"{path}.styleRef"));
        }

        // align
        if (!string.IsNullOrWhiteSpace(node.Align) && !KnownAlignments.Contains(node.Align.Trim()))
        {
            warnings.Add($"{path}.align: unknown alignment '{node.Align}' (expected left|center|right).");
        }

        // color fields
        ValidateColor(node.Color,       $"{path}.color",       errors);
        ValidateColor(node.Background,  $"{path}.background",  errors);
        ValidateColor(node.BorderColor, $"{path}.borderColor", errors);

        // type-specific checks
        switch (type)
        {
            case "image":
                if (string.IsNullOrWhiteSpace(node.Source?.Value))
                    errors.Add(new("image_source_missing",
                        "Image node requires a non-empty 'source.value'.",
                        $"{path}.source.value"));

                if (!string.IsNullOrWhiteSpace(node.Source?.Mode) &&
                    !KnownImageModes.Contains(node.Source!.Mode!))
                    errors.Add(new("invalid_image_mode",
                        $"Image source mode '{node.Source.Mode}' is not supported. Expected path|base64|bytes.",
                        $"{path}.source.mode"));

                if (!string.IsNullOrWhiteSpace(node.Fit) && !KnownFits.Contains(node.Fit.Trim()))
                    warnings.Add($"{path}.fit: unknown fit value '{node.Fit}'.");
                break;

            case "groupinstance":
                if (string.IsNullOrWhiteSpace(node.GroupRef))
                    errors.Add(new("group_instance_missing_ref",
                        "groupInstance node requires a 'groupRef'.",
                        $"{path}.groupRef"));
                else if (!groups.Contains(node.GroupRef))
                    errors.Add(new("group_ref_not_found",
                        $"Group '{node.GroupRef}' is not defined in definitions.groups.",
                        $"{path}.groupRef"));
                break;

            case "table":
                ValidateDefinitionRef(node, path, "table", repeatables, errors);
                break;

            case "repeat":
                ValidateDefinitionRef(node, path, "repeat", repeatables, errors);
                break;
        }
    }

    private static void ValidateDefinitionRef(
        SchemaNode node,
        string path,
        string expectedType,
        Dictionary<string, RepeatableDefinitionNode> repeatables,
        List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(node.DefinitionRef)) return;

        if (!repeatables.TryGetValue(node.DefinitionRef, out var def))
        {
            errors.Add(new("definition_ref_not_found",
                $"Repeatable definition '{node.DefinitionRef}' is not defined.",
                $"{path}.definitionRef"));
            return;
        }

        if (!string.Equals(def.Type, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new("definition_type_mismatch",
                $"Repeatable '{node.DefinitionRef}' has type '{def.Type}', expected '{expectedType}'.",
                $"{path}.definitionRef"));
        }
    }

    private static void ValidateColor(string? value, string path, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        try { ReportColor.FromHex(value); }
        catch
        {
            errors.Add(new("invalid_color",
                $"'{value}' is not a valid hex color. Expected #RGB, #RRGGBB, or #RRGGBBAA.",
                path));
        }
    }
}
