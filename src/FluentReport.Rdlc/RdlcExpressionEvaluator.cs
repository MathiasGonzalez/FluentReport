using System.Globalization;
using System.Text.RegularExpressions;

namespace FluentReport.Rdlc;

/// <summary>
/// Evaluates RDLC expression strings against a data row and parameter dictionary.
/// Supports:
/// <list type="bullet">
///   <item><c>=Fields!FieldName.Value</c> – resolved from the current data row.</item>
///   <item><c>=First(Fields!FieldName.Value, "DataSetName")</c> – resolved from the first row of the named dataset.</item>
///   <item><c>=Parameters!ParamName.Value</c> – resolved from the parameter dictionary.</item>
///   <item><c>=Globals!Name.Value</c> – resolved from the globals dictionary (e.g. ReportName).</item>
///   <item><c>=IIF(condition, trueValue, falseValue)</c> – conditional expression.</item>
///   <item><c>=Switch(cond1, val1, cond2, val2, ...)</c> – multi-branch conditional.</item>
///   <item><c>=Format(expr, "formatString")</c> – formats a value using .NET/VB.NET format strings.</item>
///   <item><c>=Sum/Count/Avg/Min/Max(Fields!X.Value, "DataSetName")</c> – aggregate over a dataset.</item>
///   <item><c>=CountRows("DataSetName")</c> – total row count of a dataset.</item>
///   <item><c>=expr1 &amp; expr2</c> – string concatenation.</item>
///   <item>Conditions: <c>=</c>, <c>&lt;&gt;</c>, <c>&gt;</c>, <c>&lt;</c>, <c>&gt;=</c>, <c>&lt;=</c>.</item>
///   <item>Literal strings (no leading <c>=</c>) – returned as-is.</item>
/// </list>
/// </summary>
public sealed class RdlcExpressionEvaluator
{
    private static readonly Regex FieldsRegex =
        new(@"^Fields!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParamsRegex =
        new(@"^Parameters!(?<name>\w+)\.Value$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GlobalsRegex =
        new(@"^Globals!(?<name>\w+)(\.Value)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches: First(Fields!Name.Value, "DataSetName")  or  First(Fields!Name.Value, DataSetName)
    private static readonly Regex FirstFieldsRegex =
        new(@"^First\s*\(\s*Fields!(?<name>\w+)\.Value\s*,\s*""?(?<ds>[^""\)]+)""?\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AggregateRegex =
        new(@"^(?<fn>Sum|Count|CountRows|Avg|Average|Min|Max)\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IDictionary<string, object>? _parameters;
    private readonly IDictionary<string, IEnumerable<object>>? _datasets;
    private readonly IDictionary<string, object>? _globals;

    public RdlcExpressionEvaluator(
        IDictionary<string, object>? parameters = null,
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? globals = null)
    {
        _parameters = parameters;
        _datasets = datasets;
        _globals = globals;
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

        // Globals!X.Value  (e.g. Globals!ReportName.Value)
        var globalsMatch = GlobalsRegex.Match(expr);
        if (globalsMatch.Success)
        {
            var gName = globalsMatch.Groups["name"].Value;
            if (_globals != null && _globals.TryGetValue(gName, out var gv))
                return gv?.ToString() ?? string.Empty;
            return string.Empty;
        }

        // IIF(condition, trueVal, falseVal)
        if (expr.StartsWith("IIF", StringComparison.OrdinalIgnoreCase) && expr.Length > 3 && expr[3] == '(')
            return EvaluateIIF(expr, row);

        // Switch(cond1, val1, cond2, val2, ...)
        if (expr.StartsWith("Switch", StringComparison.OrdinalIgnoreCase) && expr.Length > 6 && expr[6] == '(')
            return EvaluateSwitch(expr, row);

        // Format(expression, "format-string")
        if (expr.StartsWith("Format", StringComparison.OrdinalIgnoreCase) && expr.Length > 6 && expr[6] == '(')
            return EvaluateFormat(expr, row);

        // Aggregate functions: Sum, Count, Avg, Min, Max, CountRows
        var aggMatch = AggregateRegex.Match(expr);
        if (aggMatch.Success)
            return EvaluateAggregate(expr, row, aggMatch.Groups["fn"].Value);

        // String concatenation with & operator (VB.NET string concat) — must be checked before
        // the quoted-literal shortcut, because expressions like "A" & "B" start and end with '"'.
        var concatParts = SplitByTopLevelConcatOperator(expr);
        if (concatParts != null)
            return string.Concat(concatParts.Select(p => EvaluateExpr(p.Trim(), row)));

        // Quoted string literal inside an expression: "text"
        if (expr.StartsWith('"') && expr.EndsWith('"') && expr.Length >= 2)
            return expr[1..^1];

        // Bare numeric literal — used as a constant in comparisons (e.g. Fields!X.Value > 100).
        if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return expr;

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

    // ── Format ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates <c>Format(expression, "formatString")</c>.
    /// Supports standard .NET format strings and named VB.NET format strings
    /// (<c>"Short Date"</c>, <c>"Long Date"</c>, <c>"Currency"</c>, etc.).
    /// </summary>
    private string EvaluateFormat(string expr, object? row)
    {
        var args = SplitTopLevelArgs(expr, expr.IndexOf('('));
        if (args.Count < 1) return string.Empty;

        var value = EvaluateExpr(args[0].Trim(), row);
        var fmt   = args.Count >= 2 ? NormalizeVbFormatString(args[1].Trim().Trim('"')) : string.Empty;

        if (string.IsNullOrEmpty(fmt))
            return value;

        // Try numeric format first.
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dblVal))
        {
            try { return dblVal.ToString(fmt, CultureInfo.InvariantCulture); }
            catch { return value; }
        }

        // Try DateTime format.
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVal))
        {
            try { return dtVal.ToString(fmt, CultureInfo.InvariantCulture); }
            catch { return value; }
        }

        return value;
    }

    /// <summary>Maps VB.NET named format strings to their .NET equivalents.</summary>
    private static string NormalizeVbFormatString(string fmt)
        => fmt.ToLowerInvariant() switch
        {
            "short date" or "general date" => "d",
            "long date"                    => "D",
            "short time"                   => "t",
            "long time"                    => "T",
            "currency"                     => "C",
            "fixed"                        => "F",
            "standard"                     => "N",
            "percent"                      => "P",
            "scientific"                   => "E",
            _                              => fmt
        };

    // ── Aggregate functions ────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates aggregate functions: <c>Sum</c>, <c>Count</c>, <c>Avg</c>, <c>Average</c>,
    /// <c>Min</c>, <c>Max</c>, <c>CountRows</c>.
    /// When no dataset name is provided and exactly one dataset is registered, that dataset is used.
    /// </summary>
    private string EvaluateAggregate(string expr, object? row, string funcName)
    {
        var args = SplitTopLevelArgs(expr, expr.IndexOf('('));

        // CountRows([optional dataset name])
        if (string.Equals(funcName, "CountRows", StringComparison.OrdinalIgnoreCase))
        {
            var dsNameForCount = args.Count >= 1 ? args[0].Trim().Trim('"') : string.Empty;
            var sourceForCount = GetDataset(dsNameForCount);
            return (sourceForCount?.Count() ?? 0).ToString(CultureInfo.InvariantCulture);
        }

        if (args.Count == 0) return string.Empty;

        var fieldExpr = args[0].Trim();
        var dsName    = args.Count >= 2 ? args[1].Trim().Trim('"') : string.Empty;
        var source    = GetDataset(dsName);

        if (source == null) return string.Empty;

        if (string.Equals(funcName, "Count", StringComparison.OrdinalIgnoreCase))
        {
            var cnt = source.Select(r => EvaluateExpr(fieldExpr, r)).Count(v => !string.IsNullOrEmpty(v));
            return cnt.ToString(CultureInfo.InvariantCulture);
        }

        var numericValues = source
            .Select(r => EvaluateExpr(fieldExpr, r))
            .Where(v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            .Select(v => double.Parse(v, CultureInfo.InvariantCulture))
            .ToList();

        if (numericValues.Count == 0) return string.Empty;

        return funcName.ToUpperInvariant() switch
        {
            "SUM"              => numericValues.Sum().ToString(CultureInfo.InvariantCulture),
            "AVG" or "AVERAGE" => numericValues.Average().ToString(CultureInfo.InvariantCulture),
            "MIN"              => numericValues.Min().ToString(CultureInfo.InvariantCulture),
            "MAX"              => numericValues.Max().ToString(CultureInfo.InvariantCulture),
            _                  => string.Empty
        };
    }

    /// <summary>
    /// Returns the dataset with the given name, or — when <paramref name="dsName"/> is empty —
    /// the only registered dataset if exactly one is available.
    /// </summary>
    private IEnumerable<object>? GetDataset(string dsName)
    {
        if (_datasets == null) return null;
        if (!string.IsNullOrEmpty(dsName))
            return _datasets.TryGetValue(dsName, out var ds) ? ds : null;
        // If no dataset name specified, use the sole dataset.
        return _datasets.Count == 1 ? _datasets.Values.First() : null;
    }

    // ── Condition evaluator ───────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a boolean condition expression.
    /// Supports: <c>Fields!X.Value = "literal"</c>, <c>Fields!X.Value &lt;&gt; "literal"</c>,
    /// <c>Fields!X.Value &gt; 0</c>, <c>Fields!X.Value &lt;= 100</c>, <c>True/False</c> literals.
    /// Unknown conditions return <c>true</c> (permissive fallback).
    /// </summary>
    private bool EvaluateCondition(string condition, object? row)
    {
        condition = condition.Trim();

        var (opPos, op) = FindTopLevelComparisonOperator(condition);
        if (opPos > 0 && op != null)
        {
            var lhs    = condition[..opPos].Trim();
            var rhs    = condition[(opPos + op.Length)..].Trim();
            var lhsVal = EvaluateExpr(lhs, row);
            var rhsVal = UnquoteOrEval(rhs, row);
            return op switch
            {
                "="  => string.Equals(lhsVal, rhsVal, StringComparison.OrdinalIgnoreCase),
                "<>" => !string.Equals(lhsVal, rhsVal, StringComparison.OrdinalIgnoreCase),
                ">=" => CompareValues(lhsVal, rhsVal) >= 0,
                "<=" => CompareValues(lhsVal, rhsVal) <= 0,
                ">"  => CompareValues(lhsVal, rhsVal) > 0,
                "<"  => CompareValues(lhsVal, rhsVal) < 0,
                _    => false
            };
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

    /// <summary>
    /// Finds the position and operator string of the first top-level comparison operator
    /// (<c>=</c>, <c>&lt;&gt;</c>, <c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&lt;</c>)
    /// not inside parentheses or quoted strings.
    /// Multi-character operators are checked before single-character ones.
    /// </summary>
    private static (int pos, string? op) FindTopLevelComparisonOperator(string s)
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
            if (depth != 0) continue;

            // Multi-char operators first (order matters: check 2-char before 1-char).
            if (i + 1 < s.Length)
            {
                var two = s.Substring(i, 2);
                if (two is "<>" or ">=" or "<=") return (i, two);
            }
            if (c is '=' or '>' or '<') return (i, c.ToString());
        }
        return (-1, null);
    }

    /// <summary>
    /// Compares two string values: first attempts numeric comparison, then
    /// <see cref="DateTime"/> comparison, and falls back to ordinal string comparison.
    /// </summary>
    private static int CompareValues(string a, string b)
    {
        if (double.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var da) &&
            double.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out var db))
            return da.CompareTo(db);

        if (DateTime.TryParse(a, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dta) &&
            DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtb))
            return dta.CompareTo(dtb);

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Splits <paramref name="expr"/> by top-level <c>&amp;</c> (VB.NET string concatenation)
    /// operators that are not inside parentheses or quoted strings.
    /// Returns <c>null</c> when no top-level <c>&amp;</c> is found.
    /// </summary>
    private static List<string>? SplitByTopLevelConcatOperator(string expr)
    {
        var parts = new List<string>();
        int depth = 0;
        bool inStr = false;
        int start = 0;
        bool found = false;

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '"') { inStr = !inStr; continue; }
            if (inStr) continue;
            if (c == '(') { depth++; continue; }
            if (c == ')') { depth--; continue; }
            if (depth != 0) continue;

            if (c == '&')
            {
                parts.Add(expr[start..i]);
                start = i + 1;
                found = true;
            }
        }

        if (!found) return null;
        parts.Add(expr[start..]);
        return parts;
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
