using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace FluentReport.Rdlc;

/// <summary>
/// Evaluates RDLC (VB.NET-style) expressions against a data row, the report parameters,
/// globals and datasets.
/// </summary>
/// <remarks>
/// Values keep their CLR type (decimal, DateTime, bool, string…) while an expression is
/// evaluated and are turned into text only at the end, with <see cref="Culture"/>. Numbers that
/// arrive as strings are read as invariant ("1234.5") first and with <see cref="Culture"/> second.
/// <para>
/// Supported: literals, <c>Fields!X.Value</c>, <c>Parameters!X.Value</c>/<c>.Label</c>,
/// <c>Globals!X(.Value)</c>, operators <c>^ * / \ Mod + - &amp; = &lt;&gt; &lt; &gt; &lt;= &gt;= Not And AndAlso Or OrElse Xor</c>,
/// and the functions listed in <see cref="CallFunction"/>. Anything else is reported through the
/// warning callback (or thrown in strict mode) and evaluates to an empty value.
/// </para>
/// </remarks>
internal sealed class RdlcExpressionEvaluator
{
    private static readonly Regex FieldExpressionRegex = new(
        @"^\s*=\s*(First\s*\(\s*)?Fields!\w+\.Value\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>?> MemberCache = new();

    private readonly IDictionary<string, object>? _parameters;
    private readonly IDictionary<string, IEnumerable<object>>? _datasets;
    private readonly IDictionary<string, object>? _globals;
    private readonly Action<string>? _warn;
    private readonly bool _strict;
    private readonly Dictionary<string, Node> _parsed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<object>> _materialized = new(StringComparer.OrdinalIgnoreCase);

    public RdlcExpressionEvaluator(
        IDictionary<string, object>? parameters = null,
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? globals = null,
        CultureInfo? culture = null,
        Action<string>? warn = null,
        bool strict = false)
    {
        _parameters = parameters;
        _datasets = datasets;
        _globals = globals;
        Culture = culture ?? CultureInfo.InvariantCulture;
        _warn = warn;
        _strict = strict;
    }

    /// <summary>Culture used to format values and to read numbers/dates that arrive as text.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Evaluates an RDLC value and returns its display text. Literals (no leading '=') are returned as-is.</summary>
    public string Evaluate(string? expression, object? row = null) => ToDisplay(EvaluateValue(expression, row));

    /// <summary>Evaluates an RDLC value and returns the typed result (null when empty or unsupported).</summary>
    public object? EvaluateValue(string? expression, object? row = null)
    {
        if (string.IsNullOrEmpty(expression)) return null;
        if (!expression.StartsWith('=')) return expression;

        try
        {
            if (!_parsed.TryGetValue(expression, out var node))
                _parsed[expression] = node = new Parser(expression[1..]).ParseAll();
            return node.Eval(this, row);
        }
        catch (RdlcExpressionException ex)
        {
            if (_strict)
                throw new NotSupportedException($"RDLC expression '{expression}' could not be evaluated: {ex.Message}", ex);
            _warn?.Invoke($"Expression '{expression}' evaluated to empty: {ex.Message}");
            return null;
        }
    }

    /// <summary><c>true</c> for <c>=Fields!X.Value…</c> and <c>=First(Fields!X.Value…)</c> expressions.</summary>
    public static bool IsFieldExpression(string? expression)
        => expression != null && FieldExpressionRegex.IsMatch(expression);

    // ── Conversions ──────────────────────────────────────────────────────────

    internal string ToDisplay(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        bool b => b ? "True" : "False",
        IFormattable f => f.ToString(null, Culture),
        _ => value.ToString() ?? string.Empty
    };

    private static bool IsNumeric(object? v) => v is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>Reads a number from a typed value or from text (invariant first, then <see cref="Culture"/>).</summary>
    internal decimal? ToNumber(object? value)
    {
        switch (value)
        {
            case null: return null;
            case decimal d: return d;
            case double or float:
                var dbl = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsFinite(dbl) && Math.Abs(dbl) < 7.9e28 ? (decimal)dbl : null;
            case bool: return null;
            case IConvertible when IsNumeric(value): return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            case string s:
                s = s.Trim();
                if (s.Length == 0) return null;
                if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var inv)) return inv;
                if (decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, Culture, out var loc)) return loc;
                return null;
            default: return null;
        }
    }

    internal DateTime? ToDate(object? value) => value switch
    {
        DateTime dt => dt,
        DateTimeOffset dto => dto.DateTime,
        DateOnly d => d.ToDateTime(TimeOnly.MinValue),
        string s when DateTime.TryParse(s, Culture, DateTimeStyles.None, out var a) => a,
        string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b) => b,
        _ => null
    };

    internal bool ToBool(object? value) => value switch
    {
        null => false,
        bool b => b,
        string s when bool.TryParse(s.Trim(), out var b) => b,
        _ when ToNumber(value) is { } n => n != 0,
        _ => throw new RdlcExpressionException($"'{ToDisplay(value)}' is not a boolean")
    };

    private int Compare(object? a, object? b)
    {
        if (ToNumber(a) is { } na && ToNumber(b) is { } nb)
            return na.CompareTo(nb);
        if ((a is DateTime || b is DateTime) && ToDate(a) is { } da && ToDate(b) is { } db)
            return da.CompareTo(db);
        return string.Compare(ToDisplay(a), ToDisplay(b), StringComparison.OrdinalIgnoreCase);
    }

    private bool AreEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if ((IsNumeric(a) || IsNumeric(b)) && ToNumber(a) is { } na && ToNumber(b) is { } nb) return na == nb;
        if ((a is DateTime || b is DateTime) && ToDate(a) is { } da && ToDate(b) is { } db) return da == db;
        return string.Equals(ToDisplay(a), ToDisplay(b), StringComparison.OrdinalIgnoreCase);
    }

    // ── Data access ──────────────────────────────────────────────────────────

    private static object? ResolveField(string name, object? row)
    {
        switch (row)
        {
            case null:
                return null;
            case IDictionary<string, object?> dict:
                if (dict.TryGetValue(name, out var exact)) return exact;
                return dict.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
            case IReadOnlyDictionary<string, object?> ro:
                if (ro.TryGetValue(name, out var roExact)) return roExact;
                return ro.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
            case IDictionary raw:
                foreach (DictionaryEntry e in raw)
                    if (e.Key is string k && string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) return e.Value;
                return null;
        }

        var getter = MemberCache.GetOrAdd((row.GetType(), name), static key =>
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
            if (key.Item1.GetProperty(key.Item2, flags) is { } p) return p.GetValue;
            if (key.Item1.GetField(key.Item2, flags) is { } f) return f.GetValue;
            return null;
        });
        return getter?.Invoke(row);
    }

    private List<object>? GetDataset(string? name)
    {
        if (_datasets == null) return null;
        if (string.IsNullOrEmpty(name))
        {
            if (_datasets.Count != 1) return null;
            name = _datasets.Keys.First();
        }
        if (_materialized.TryGetValue(name, out var cached)) return cached;
        if (!_datasets.TryGetValue(name, out var rows)) return null;
        return _materialized[name] = rows.ToList();
    }

    // ── Functions ────────────────────────────────────────────────────────────

    /// <summary>
    /// Supported functions: IIF, Switch, Choose, Format, FormatNumber, FormatCurrency, FormatPercent,
    /// CStr, CInt, CLng, CDec, CDbl, CBool, CDate, Len, Trim, LTrim, RTrim, UCase, LCase, Left, Right,
    /// Mid, Replace, IsNothing, Now, Today, and the aggregates Sum, Count, CountDistinct, CountRows,
    /// Avg/Average, Min, Max, First, Last.
    /// </summary>
    private object? CallFunction(string name, IReadOnlyList<Node> args, object? row)
    {
        object? Arg(int i) => i < args.Count ? args[i].Eval(this, row) : throw new RdlcExpressionException($"{name}() expects more arguments");
        string Str(int i) => ToDisplay(Arg(i));
        decimal Num(int i) => ToNumber(Arg(i)) ?? throw new RdlcExpressionException($"{name}(): '{Str(i)}' is not a number");

        switch (name.ToUpperInvariant())
        {
            case "IIF":
                return ToBool(Arg(0)) ? Arg(1) : Arg(2);
            case "SWITCH":
                for (int i = 0; i + 1 < args.Count; i += 2)
                    if (ToBool(Arg(i))) return Arg(i + 1);
                return null;
            case "CHOOSE":
                var index = (int)Num(0);
                return index >= 1 && index < args.Count ? Arg(index) : null;

            case "FORMAT":
                return Format(Arg(0), args.Count > 1 ? Str(1) : null);
            case "FORMATNUMBER":
                return ToNumber(Arg(0))?.ToString("N" + (args.Count > 1 ? (int)Num(1) : 2), Culture);
            case "FORMATCURRENCY":
                return ToNumber(Arg(0))?.ToString("C" + (args.Count > 1 ? (int)Num(1) : 2), Culture);
            case "FORMATPERCENT":
                return ToNumber(Arg(0))?.ToString("P" + (args.Count > 1 ? (int)Num(1) : 2), Culture);

            case "CSTR": return Str(0);
            case "CINT":
            case "CLNG": return (long)Math.Round(Num(0), MidpointRounding.ToEven);
            case "CDEC":
            case "CDBL": return Num(0);
            case "CBOOL": return ToBool(Arg(0));
            case "CDATE": return ToDate(Arg(0)) ?? throw new RdlcExpressionException($"'{Str(0)}' is not a date");

            case "LEN": return (decimal)Str(0).Length;
            case "TRIM": return Str(0).Trim();
            case "LTRIM": return Str(0).TrimStart();
            case "RTRIM": return Str(0).TrimEnd();
            case "UCASE": return Str(0).ToUpper(Culture);
            case "LCASE": return Str(0).ToLower(Culture);
            case "LEFT": { var s = Str(0); return s[..Math.Clamp((int)Num(1), 0, s.Length)]; }
            case "RIGHT": { var s = Str(0); var n = Math.Clamp((int)Num(1), 0, s.Length); return s[^n..]; }
            case "MID":
            {
                var s = Str(0);
                var start = Math.Clamp((int)Num(1) - 1, 0, s.Length);
                var len = args.Count > 2 ? Math.Clamp((int)Num(2), 0, s.Length - start) : s.Length - start;
                return s.Substring(start, len);
            }
            case "REPLACE": return Str(0).Replace(Str(1), Str(2), StringComparison.Ordinal);
            case "ISNOTHING": return Arg(0) is null or "";
            case "NOW": return DateTime.Now;
            case "TODAY": return DateTime.Today;

            case "SUM": case "COUNT": case "COUNTDISTINCT": case "AVG": case "AVERAGE":
            case "MIN": case "MAX": case "FIRST": case "LAST": case "COUNTROWS":
                return Aggregate(name.ToUpperInvariant(), args, row);

            default:
                throw new RdlcExpressionException($"function '{name}' is not supported");
        }
    }

    private object? Aggregate(string fn, IReadOnlyList<Node> args, object? row)
    {
        if (fn == "COUNTROWS")
        {
            var scopeRows = GetDataset(args.Count > 0 ? ToDisplay(args[0].Eval(this, row)) : null);
            return scopeRows == null ? null : (decimal)scopeRows.Count;
        }

        if (args.Count == 0) throw new RdlcExpressionException($"{fn}() expects an argument");

        // First() inside a detail row reads the current row, as before.
        if (fn == "FIRST" && row != null) return args[0].Eval(this, row);

        var rows = GetDataset(args.Count > 1 ? ToDisplay(args[1].Eval(this, row)) : null);
        if (rows == null) return null;

        var values = rows.Select(r => args[0].Eval(this, r)).ToList();
        switch (fn)
        {
            case "FIRST": return values.FirstOrDefault();
            case "LAST": return values.LastOrDefault();
            case "COUNT": return (decimal)values.Count(v => v is not null && !(v is string s && s.Length == 0));
            case "COUNTDISTINCT": return (decimal)values.Where(v => v is not null).Select(ToDisplay).Distinct().Count();
        }

        var numbers = values.Select(ToNumber).Where(n => n.HasValue).Select(n => n!.Value).ToList();
        if (numbers.Count == 0)
        {
            if (fn is "MIN" or "MAX")
            {
                var dates = values.Select(ToDate).Where(d => d.HasValue).Select(d => d!.Value).ToList();
                if (dates.Count > 0) return fn == "MIN" ? dates.Min() : dates.Max();
            }
            return null;
        }

        return fn switch
        {
            "SUM" => numbers.Sum(),
            "AVG" or "AVERAGE" => numbers.Average(),
            "MIN" => numbers.Min(),
            "MAX" => numbers.Max(),
            _ => null
        };
    }

    private object? Format(object? value, string? format)
    {
        if (string.IsNullOrEmpty(format)) return value;
        format = format.ToLowerInvariant() switch
        {
            "short date" => "d",
            "long date" => "D",
            "general date" => "G",
            "short time" => "t",
            "long time" => "T",
            "currency" => "C",
            "fixed" => "F",
            "standard" => "N",
            "percent" => "P",
            "scientific" => "E",
            _ => format
        };

        try
        {
            if (value is DateTime or DateTimeOffset or DateOnly)
                return ToDate(value)!.Value.ToString(format, Culture);
            if (ToNumber(value) is { } n)
                return n.ToString(format, Culture);
            if (ToDate(value) is { } d)
                return d.ToString(format, Culture);
        }
        catch (FormatException)
        {
            throw new RdlcExpressionException($"invalid format string '{format}'");
        }
        return value;
    }

    // ── AST ──────────────────────────────────────────────────────────────────

    private abstract record Node
    {
        public abstract object? Eval(RdlcExpressionEvaluator ev, object? row);
    }

    private sealed record Literal(object? Value) : Node
    {
        public override object? Eval(RdlcExpressionEvaluator ev, object? row) => Value;
    }

    private sealed record Reference(string Collection, string Name) : Node
    {
        public override object? Eval(RdlcExpressionEvaluator ev, object? row)
        {
            switch (Collection.ToUpperInvariant())
            {
                case "FIELDS":
                    return ResolveField(Name, row);
                case "PARAMETERS":
                    if (ev._parameters != null && ev._parameters.TryGetValue(Name, out var p)) return p;
                    ev._warn?.Invoke($"Parameter '{Name}' was not supplied.");
                    return null;
                case "GLOBALS":
                    if (ev._globals != null && ev._globals.TryGetValue(Name, out var g)) return g;
                    ev._warn?.Invoke($"Global '{Name}' was not supplied (pass it through the globals dictionary).");
                    return null;
                default:
                    throw new RdlcExpressionException($"collection '{Collection}!' is not supported");
            }
        }
    }

    private sealed record Unary(string Op, Node Operand) : Node
    {
        public override object? Eval(RdlcExpressionEvaluator ev, object? row)
        {
            var v = Operand.Eval(ev, row);
            return Op switch
            {
                "NOT" => !ev.ToBool(v),
                "-" => -(ev.ToNumber(v) ?? throw new RdlcExpressionException($"'{ev.ToDisplay(v)}' is not a number")),
                _ => throw new RdlcExpressionException($"operator '{Op}' is not supported")
            };
        }
    }

    private sealed record Binary(string Op, Node Left, Node Right) : Node
    {
        public override object? Eval(RdlcExpressionEvaluator ev, object? row)
        {
            switch (Op)
            {
                case "ANDALSO": return ev.ToBool(Left.Eval(ev, row)) && ev.ToBool(Right.Eval(ev, row));
                case "ORELSE": return ev.ToBool(Left.Eval(ev, row)) || ev.ToBool(Right.Eval(ev, row));
            }

            var l = Left.Eval(ev, row);
            var r = Right.Eval(ev, row);
            switch (Op)
            {
                case "AND": return ev.ToBool(l) & ev.ToBool(r);
                case "OR": return ev.ToBool(l) | ev.ToBool(r);
                case "XOR": return ev.ToBool(l) ^ ev.ToBool(r);
                case "&": return ev.ToDisplay(l) + ev.ToDisplay(r);
                case "=": return ev.AreEqual(l, r);
                case "<>": return !ev.AreEqual(l, r);
                case "<": return ev.Compare(l, r) < 0;
                case ">": return ev.Compare(l, r) > 0;
                case "<=": return ev.Compare(l, r) <= 0;
                case ">=": return ev.Compare(l, r) >= 0;
                case "+" when (l is string || r is string) && !(IsNumeric(l) || IsNumeric(r)):
                    return ev.ToDisplay(l) + ev.ToDisplay(r);
            }

            decimal a = ev.ToNumber(l) ?? (l is null ? 0 : throw new RdlcExpressionException($"'{ev.ToDisplay(l)}' is not a number"));
            decimal b = ev.ToNumber(r) ?? (r is null ? 0 : throw new RdlcExpressionException($"'{ev.ToDisplay(r)}' is not a number"));
            try
            {
                return Op switch
                {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    "/" => a / b,
                    "\\" => decimal.Truncate(a / b),
                    "MOD" => a % b,
                    "^" => (decimal)Math.Pow((double)a, (double)b),
                    _ => throw new RdlcExpressionException($"operator '{Op}' is not supported")
                };
            }
            catch (DivideByZeroException)
            {
                throw new RdlcExpressionException("division by zero");
            }
            catch (OverflowException)
            {
                throw new RdlcExpressionException("numeric overflow");
            }
        }
    }

    private sealed record Call(string Name, IReadOnlyList<Node> Args) : Node
    {
        public override object? Eval(RdlcExpressionEvaluator ev, object? row) => ev.CallFunction(Name, Args, row);
    }

    // ── Parser ───────────────────────────────────────────────────────────────

    private enum TokenKind { Number, String, Identifier, Operator, End }

    private readonly record struct Token(TokenKind Kind, string Text);

    /// <summary>Recursive-descent parser following VB.NET operator precedence.</summary>
    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public Parser(string text) => _tokens = Tokenize(text);

        public Node ParseAll()
        {
            var node = ParseOr();
            if (Peek.Kind != TokenKind.End)
                throw new RdlcExpressionException($"unexpected '{Peek.Text}'");
            return node;
        }

        private Token Peek => _tokens[_pos];
        private Token Next() => _tokens[_pos++];
        private bool IsKeyword(string kw) => Peek.Kind == TokenKind.Identifier && Peek.Text.Equals(kw, StringComparison.OrdinalIgnoreCase);
        private bool IsOperator(string op) => Peek.Kind == TokenKind.Operator && Peek.Text == op;

        private void Expect(string op)
        {
            if (!IsOperator(op)) throw new RdlcExpressionException($"expected '{op}' but found '{Peek.Text}'");
            _pos++;
        }

        private Node ParseOr()
        {
            var left = ParseAnd();
            while (IsKeyword("Or") || IsKeyword("OrElse") || IsKeyword("Xor"))
                left = new Binary(Next().Text.ToUpperInvariant(), left, ParseAnd());
            return left;
        }

        private Node ParseAnd()
        {
            var left = ParseNot();
            while (IsKeyword("And") || IsKeyword("AndAlso"))
                left = new Binary(Next().Text.ToUpperInvariant(), left, ParseNot());
            return left;
        }

        private Node ParseNot()
        {
            if (IsKeyword("Not")) { _pos++; return new Unary("NOT", ParseNot()); }
            return ParseComparison();
        }

        private static readonly string[] ComparisonOps = ["=", "<>", "<", ">", "<=", ">="];

        private Node ParseComparison()
        {
            var left = ParseConcat();
            while (Peek.Kind == TokenKind.Operator && ComparisonOps.Contains(Peek.Text))
                left = new Binary(Next().Text, left, ParseConcat());
            return left;
        }

        private Node ParseConcat()
        {
            var left = ParseAdditive();
            while (IsOperator("&"))
            {
                _pos++;
                left = new Binary("&", left, ParseAdditive());
            }
            return left;
        }

        private Node ParseAdditive()
        {
            var left = ParseModulo();
            while (IsOperator("+") || IsOperator("-"))
                left = new Binary(Next().Text, left, ParseModulo());
            return left;
        }

        private Node ParseModulo()
        {
            var left = ParseIntegerDivision();
            while (IsKeyword("Mod"))
            {
                _pos++;
                left = new Binary("MOD", left, ParseIntegerDivision());
            }
            return left;
        }

        private Node ParseIntegerDivision()
        {
            var left = ParseMultiplicative();
            while (IsOperator("\\"))
            {
                _pos++;
                left = new Binary("\\", left, ParseMultiplicative());
            }
            return left;
        }

        private Node ParseMultiplicative()
        {
            var left = ParseUnary();
            while (IsOperator("*") || IsOperator("/"))
                left = new Binary(Next().Text, left, ParseUnary());
            return left;
        }

        private Node ParseUnary()
        {
            if (IsOperator("-")) { _pos++; return new Unary("-", ParseUnary()); }
            if (IsOperator("+")) { _pos++; return ParseUnary(); }
            return ParsePower();
        }

        private Node ParsePower()
        {
            var left = ParsePrimary();
            while (IsOperator("^"))
            {
                _pos++;
                left = new Binary("^", left, ParsePrimary());
            }
            return left;
        }

        private Node ParsePrimary()
        {
            var token = Next();
            switch (token.Kind)
            {
                case TokenKind.Number:
                    return new Literal(decimal.Parse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture));
                case TokenKind.String:
                    return new Literal(token.Text);
                case TokenKind.Operator when token.Text == "(":
                    var inner = ParseOr();
                    Expect(")");
                    return inner;
                case TokenKind.Identifier:
                    return ParseIdentifier(token.Text);
                default:
                    throw new RdlcExpressionException(token.Kind == TokenKind.End ? "unexpected end of expression" : $"unexpected '{token.Text}'");
            }
        }

        private Node ParseIdentifier(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "TRUE": return new Literal(true);
                case "FALSE": return new Literal(false);
                case "NOTHING": return new Literal(null);
            }

            if (IsOperator("!"))
            {
                _pos++;
                var member = Next();
                if (member.Kind != TokenKind.Identifier)
                    throw new RdlcExpressionException($"expected a name after '{name}!'");
                // Optional .Value / .Label / .IsMissing suffix (only the value is modelled).
                while (IsOperator("."))
                {
                    _pos++;
                    var prop = Next();
                    if (prop.Kind != TokenKind.Identifier || !(prop.Text.Equals("Value", StringComparison.OrdinalIgnoreCase)
                                                             || prop.Text.Equals("Label", StringComparison.OrdinalIgnoreCase)))
                        throw new RdlcExpressionException($"property '{name}!{member.Text}.{prop.Text}' is not supported");
                }
                return new Reference(name, member.Text);
            }

            if (IsOperator("("))
            {
                _pos++;
                var args = new List<Node>();
                if (!IsOperator(")"))
                {
                    do { args.Add(ParseOr()); }
                    while (TryConsume(","));
                }
                Expect(")");
                return new Call(name, args);
            }

            if (name.Equals("Now", StringComparison.OrdinalIgnoreCase) || name.Equals("Today", StringComparison.OrdinalIgnoreCase))
                return new Call(name, []);

            throw new RdlcExpressionException($"'{name}' is not supported");
        }

        private bool TryConsume(string op)
        {
            if (!IsOperator(op)) return false;
            _pos++;
            return true;
        }

        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c) || (c == '_' && (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]))))
                {
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    var sb = new StringBuilder();
                    i++;
                    while (true)
                    {
                        if (i >= text.Length) throw new RdlcExpressionException("unterminated string literal");
                        if (text[i] == '"')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i += 2; continue; }
                            i++;
                            break;
                        }
                        sb.Append(text[i++]);
                    }
                    tokens.Add(new Token(TokenKind.String, sb.ToString()));
                    continue;
                }

                if (char.IsDigit(c) || (c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
                {
                    int start = i;
                    while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.')) i++;
                    if (i < text.Length && (text[i] is 'e' or 'E') && i + 1 < text.Length && (char.IsDigit(text[i + 1]) || text[i + 1] is '+' or '-'))
                    {
                        i += 2;
                        while (i < text.Length && char.IsDigit(text[i])) i++;
                    }
                    tokens.Add(new Token(TokenKind.Number, text[start..i]));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i++;
                    tokens.Add(new Token(TokenKind.Identifier, text[start..i]));
                    continue;
                }

                if (i + 1 < text.Length && text.Substring(i, 2) is "<>" or "<=" or ">=")
                {
                    tokens.Add(new Token(TokenKind.Operator, text.Substring(i, 2)));
                    i += 2;
                    continue;
                }

                if ("+-*/\\^&=<>(),!.".Contains(c))
                {
                    tokens.Add(new Token(TokenKind.Operator, c.ToString()));
                    i++;
                    continue;
                }

                throw new RdlcExpressionException($"unexpected character '{c}'");
            }
            tokens.Add(new Token(TokenKind.End, "end of expression"));
            return tokens;
        }
    }
}

internal sealed class RdlcExpressionException(string message) : Exception(message);
