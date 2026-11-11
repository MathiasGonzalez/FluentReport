using System.Text.Json;

namespace FluentReport.Cli;

/// <summary>Reads <c>--data</c> JSON files into the datasets the RDLC importer takes: rows as dictionaries.</summary>
internal static class JsonDatasets
{
    private static readonly JsonDocumentOptions Options = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <param name="data">The <c>--data</c> arguments, in order; a later file replaces a dataset an earlier one gave.</param>
    /// <param name="declared">Datasets the report uses, to name the rows of an unnamed file.</param>
    /// <param name="input">Read for the file <c>-</c>.</param>
    public static Dictionary<string, IEnumerable<object>> Load(
        IReadOnlyList<DataArgument> data, IReadOnlyList<string> declared, TextReader input)
    {
        var datasets = new Dictionary<string, IEnumerable<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, path) in data)
        {
            using var json = Parse(path, input);
            var root = json.RootElement;

            if (name != null)
            {
                datasets[name] = Rows(root, path, name);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                    datasets[property.Name] = Rows(property.Value, path, property.Name);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                if (declared.Count != 1)
                    throw new CliException(declared.Count == 0
                        ? $"{path} holds rows, but the report uses no dataset"
                        : $"{path} holds rows, but the report has several datasets ({string.Join(", ", declared)}): say which with --data Name={path}");
                datasets[declared[0]] = Rows(root, path, declared[0]);
            }
            else
            {
                throw new CliException($"{path}: expected an array of rows or an object of datasets, not {Describe(root.ValueKind)}");
            }
        }
        return datasets;
    }

    private static JsonDocument Parse(string path, TextReader input)
    {
        try
        {
            if (path == "-") return JsonDocument.Parse(input.ReadToEnd(), Options);
            if (!File.Exists(path)) throw new CliException($"data file not found: {path}");
            return JsonDocument.Parse(File.ReadAllBytes(path), Options);
        }
        catch (JsonException ex)
        {
            throw new CliException($"{path} is not valid JSON: {ex.Message}");
        }
    }

    private static List<object> Rows(JsonElement array, string path, string dataset)
    {
        if (array.ValueKind != JsonValueKind.Array)
            throw new CliException($"{path}: dataset '{dataset}' must be an array of rows, not {Describe(array.ValueKind)}");

        var rows = new List<object>(array.GetArrayLength());
        int index = 0;
        foreach (var row in array.EnumerateArray())
        {
            index++;
            if (row.ValueKind != JsonValueKind.Object)
                throw new CliException($"{path}: row {index} of '{dataset}' must be an object, not {Describe(row.ValueKind)}");
            rows.Add(Row(row));
        }
        return rows;
    }

    /// <summary>A JSON object as a row. A repeated property keeps its last value, as JSON parsers usually do.</summary>
    internal static Dictionary<string, object?> Row(JsonElement element)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            row[property.Name] = Value(property.Value);
        return row;
    }

    /// <summary>
    /// Numbers become <see cref="decimal"/>, as the importer computes with decimals; ones out of its
    /// range become <see cref="double"/>. Dates stay text: the importer reads them with the report's culture.
    /// </summary>
    internal static object? Value(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetDecimal(out var number) ? number : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => Row(element),
        JsonValueKind.Array => element.EnumerateArray().Select(Value).ToList(),
        _ => null
    };

    private static string Describe(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => "a string",
        JsonValueKind.Number => "a number",
        JsonValueKind.True or JsonValueKind.False => "a boolean",
        _ => "null"
    };
}
