using System.Text.RegularExpressions;

namespace FluentReport.Rdlc;

/// <summary>
/// Evaluates RDLC expression strings against a data row and parameter dictionary.
/// Supports:
/// <list type="bullet">
///   <item><c>=Fields!FieldName.Value</c> – resolved from the current data row.</item>
///   <item><c>=Parameters!ParamName.Value</c> – resolved from the parameter dictionary.</item>
///   <item>Literal strings (no leading <c>=</c>) – returned as-is.</item>
/// </list>
/// </summary>
public sealed class RdlcExpressionEvaluator
{
    private static readonly Regex FieldsRegex =
        new(@"^Fields!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParamsRegex =
        new(@"^Parameters!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IDictionary<string, object>? _parameters;

    public RdlcExpressionEvaluator(IDictionary<string, object>? parameters = null)
    {
        _parameters = parameters;
    }

    /// <summary>
    /// Evaluates the expression using an optional data row.
    /// </summary>
    /// <param name="expression">Raw value string from the RDLC XML (e.g. <c>=Fields!Name.Value</c>).</param>
    /// <param name="row">
    /// Optional current data row. May be an <see cref="IDictionary{TKey,TValue}"/> of string→object,
    /// or any POCO (property values are resolved via reflection).
    /// </param>
    /// <returns>The resolved string value, or an empty string when resolution fails.</returns>
    public string Evaluate(string? expression, object? row = null)
    {
        if (string.IsNullOrEmpty(expression))
            return string.Empty;

        // Not an expression — return literal.
        if (!expression.StartsWith('='))
            return expression;

        var expr = expression[1..].Trim();

        // =Fields!X.Value
        var fieldsMatch = FieldsRegex.Match(expr);
        if (fieldsMatch.Success)
        {
            var fieldName = fieldsMatch.Groups["name"].Value;
            return ResolveField(fieldName, row);
        }

        // =Parameters!X.Value
        var paramsMatch = ParamsRegex.Match(expr);
        if (paramsMatch.Success && _parameters != null)
        {
            var paramName = paramsMatch.Groups["name"].Value;
            return _parameters.TryGetValue(paramName, out var pv) ? pv?.ToString() ?? string.Empty : string.Empty;
        }

        // Unknown / unsupported expression — return empty string.
        return string.Empty;
    }

    /// <summary>
    /// Returns <c>true</c> when the expression references a dataset field (<c>=Fields!X.Value</c>).
    /// </summary>
    public static bool IsFieldExpression(string? expression)
    {
        if (string.IsNullOrEmpty(expression) || !expression.StartsWith('='))
            return false;
        return FieldsRegex.IsMatch(expression[1..].Trim());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ResolveField(string fieldName, object? row)
    {
        if (row == null) return string.Empty;

        // Dictionary<string, object> row
        if (row is IDictionary<string, object> dict)
            return dict.TryGetValue(fieldName, out var dv) ? dv?.ToString() ?? string.Empty : string.Empty;

        // POCO – use reflection
        var prop = row.GetType().GetProperty(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        if (prop != null)
            return prop.GetValue(row)?.ToString() ?? string.Empty;

        var field = row.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        return field?.GetValue(row)?.ToString() ?? string.Empty;
    }
}
