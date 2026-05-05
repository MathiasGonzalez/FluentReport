using System.Text.RegularExpressions;

namespace FluentReport.Rdlc;

/// <summary>
/// Evaluates RDLC expression strings against a data row and parameter dictionary.
/// Supports:
/// <list type="bullet">
///   <item><c>=Fields!FieldName.Value</c> – resolved from the current data row.</item>
///   <item><c>=First(Fields!FieldName.Value, "DataSetName")</c> – resolved from the first row of the named dataset.</item>
///   <item><c>=Parameters!ParamName.Value</c> – resolved from the parameter dictionary.</item>
///   <item><c>=IIF(condition, trueValue, falseValue)</c> – conditional expression.</item>
///   <item><c>=Switch(cond1, val1, cond2, val2, ...)</c> – multi-branch conditional.</item>
///   <item>Literal strings (no leading <c>=</c>) – returned as-is.</item>
/// </list>
/// Expressions supported: simple field equality comparisons, e.g. <c>Fields!X.Value = "literal"</c>.
/// </summary>
public sealed class RdlcExpressionEvaluator
{
    private static readonly Regex FieldsRegex =
        new(@"^Fields!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParamsRegex =
        new(@"^Parameters!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches: First(Fields!Name.Value, "DataSetName")  or  First(Fields!Name.Value, DataSetName)
    private static readonly Regex FirstFieldsRegex =
        new(@"^First\s*\(\s*Fields!(?<name>\w+)\.Value\s*,\s*""?(?<ds>[^""\)]+)""?\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IDictionary<string, object>? _parameters;
    private readonly IDictionary<string, IEnumerable<object>>? _datasets;

    public RdlcExpressionEvaluator(
        IDictionary<string, object>? parameters = null,
        IDictionary<string, IEnumerable<object>>? datasets = null)
    {
        _parameters = parameters;
        _datasets = datasets;
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
        return EvaluateExpr(expr, row);
    }

    // ── Core expression dispatcher ────────────────────────────────────────────

    private string EvaluateExpr(string expr, object? row)
    {
        expr = expr.Trim();

        // Fields!X.Value
        var fieldsMatch = FieldsRegex.Match(expr);
        if (fieldsMatch.Success)
            return ResolveField(fieldsMatch.Groups["name"].Value, row);

        // First(Fields!X.Value, "ds")
        var firstMatch = FirstFieldsRegex.Match(expr);
        if (firstMatch.Success)
        {
            var fieldName = firstMatch.Groups["name"].Value;
            var dsName    = firstMatch.Groups["ds"].Value.Trim();
            // If we have a current row, prefer it; otherwise look up the dataset.
            if (row != null)
                return ResolveField(fieldName, row);
            if (_datasets != null && _datasets.TryGetValue(dsName, out var ds))
                return ResolveField(fieldName, ds.FirstOrDefault());
            return string.Empty;
        }

        // Parameters!X.Value
        var paramsMatch = ParamsRegex.Match(expr);
        if (paramsMatch.Success && _parameters != null)
        {
            var paramName = paramsMatch.Groups["name"].Value;
            return _parameters.TryGetValue(paramName, out var pv) ? pv?.ToString() ?? string.Empty : string.Empty;
        }

        // IIF(condition, trueVal, falseVal)
        if (expr.StartsWith("IIF", StringComparison.OrdinalIgnoreCase) && expr.Length > 3 && expr[3] == '(')
            return EvaluateIIF(expr, row);

        // Switch(cond1, val1, cond2, val2, ...)
        if (expr.StartsWith("Switch", StringComparison.OrdinalIgnoreCase) && expr.Length > 6 && expr[6] == '(')
            return EvaluateSwitch(expr, row);

        // Quoted string literal inside an expression: "text"
        if (expr.StartsWith('"') && expr.EndsWith('"') && expr.Length >= 2)
            return expr[1..^1];

        // Unknown / unsupported expression — return empty string.
        return string.Empty;
    }

    // ── IIF ───────────────────────────────────────────────────────────────────

    private string EvaluateIIF(string expr, object? row)
    {
        // IIF(condition, trueVal, falseVal)
        // Extract the three comma-separated top-level arguments.
        var args = SplitTopLevelArgs(expr, startAfter: expr.IndexOf('('));
        if (args.Count < 3) return string.Empty;

        bool condition = EvaluateCondition(args[0].Trim(), row);
        return condition
            ? EvaluateExpr(args[1].Trim(), row)
            : EvaluateExpr(args[2].Trim(), row);
    }

    // ── Switch ────────────────────────────────────────────────────────────────

    private string EvaluateSwitch(string expr, object? row)
    {
        // Switch(cond1, val1, cond2, val2, ...)
        var args = SplitTopLevelArgs(expr, startAfter: expr.IndexOf('('));
        // Pairs: even index = condition, odd index = value
        for (int i = 0; i + 1 < args.Count; i += 2)
        {
            if (EvaluateCondition(args[i].Trim(), row))
                return EvaluateExpr(args[i + 1].Trim(), row);
        }
        return string.Empty;
    }

    // ── Condition evaluator ───────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a simple boolean condition expression.
    /// Supports: <c>Fields!X.Value = "literal"</c>, <c>Fields!X.Value = True/False</c>.
    /// Unknown conditions return <c>true</c> (permissive fallback).
    /// </summary>
    private bool EvaluateCondition(string condition, object? row)
    {
        condition = condition.Trim();

        // Fields!X.Value = "literal"   or   Fields!X.Value = True/False
        var eqIdx = FindTopLevelEquals(condition);
        if (eqIdx > 0)
        {
            var lhs = condition[..eqIdx].Trim();
            var rhs = condition[(eqIdx + 1)..].Trim();
            var lhsVal = EvaluateExpr(lhs, row);
            var rhsVal = UnquoteOrEval(rhs, row);
            return string.Equals(lhsVal, rhsVal, StringComparison.OrdinalIgnoreCase);
        }

        // True / False literal
        if (bool.TryParse(condition, out var boolVal)) return boolVal;

        // Fallback: treat unknown condition as true
        return true;
    }

    private string UnquoteOrEval(string expr, object? row)
    {
        expr = expr.Trim();
        if (expr.StartsWith('"') && expr.EndsWith('"') && expr.Length >= 2)
            return expr[1..^1];
        return EvaluateExpr(expr, row);
    }

    // ── Argument splitter ─────────────────────────────────────────────────────

    /// <summary>
    /// Splits the content inside the outermost parentheses of <paramref name="expr"/>
    /// (starting after <paramref name="startAfter"/>) into top-level comma-separated arguments.
    /// </summary>
    private static List<string> SplitTopLevelArgs(string expr, int startAfter)
    {
        var result = new List<string>();
        int depth = 0;
        bool inStr = false;
        int argStart = startAfter + 1; // skip opening '('

        for (int i = startAfter; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '"') { inStr = !inStr; continue; }
            if (inStr) continue;

            if (c == '(') { depth++; continue; }
            if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    result.Add(expr[argStart..i].Trim());
                    break;
                }
                continue;
            }
            if (c == ',' && depth == 1)
            {
                result.Add(expr[argStart..i].Trim());
                argStart = i + 1;
            }
        }
        return result;
    }

    /// <summary>Finds the index of the top-level <c>=</c> operator (not inside parentheses or quotes).</summary>
    private static int FindTopLevelEquals(string s)
    {
        int depth = 0;
        bool inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"') { inStr = !inStr; continue; }
            if (inStr) continue;
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (c == '=' && depth == 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// Returns <c>true</c> when the expression references a dataset field
    /// (<c>=Fields!X.Value</c> or <c>=First(Fields!X.Value, ...)</c>).
    /// </summary>
    public static bool IsFieldExpression(string? expression)
    {
        if (string.IsNullOrEmpty(expression) || !expression.StartsWith('='))
            return false;
        var expr = expression[1..].Trim();
        return FieldsRegex.IsMatch(expr) || FirstFieldsRegex.IsMatch(expr);
    }

    // ── Field resolver ────────────────────────────────────────────────────────

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
