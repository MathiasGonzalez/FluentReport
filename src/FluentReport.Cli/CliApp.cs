using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using FluentReport.Core;
using FluentReport.Excel;
using FluentReport.Html;
using FluentReport.Rdlc;

namespace FluentReport.Cli;

internal enum OutputFormat { Pdf, Png, Html, Excel }

/// <summary>A <c>--data</c> argument: rows for dataset <see cref="Name"/>, or for the datasets the file names when it is <c>null</c>.</summary>
internal sealed record DataArgument(string? Name, string Path);

internal sealed record RenderOptions(
    string Report,
    string Output,
    OutputFormat Format,
    IReadOnlyList<DataArgument> Data,
    IReadOnlyDictionary<string, object> Parameters,
    CultureInfo? Culture,
    bool Strict,
    bool ExternalImages,
    float Scale);

/// <summary>The command line was wrong: exit code 2 and a pointer to the help.</summary>
internal sealed class UsageException(string message) : Exception(message);

/// <summary>The input could not be used (a missing file, bad JSON): exit code 1 and the message, without a stack trace.</summary>
internal sealed class CliException(string message) : Exception(message);

internal static class CliApp
{
    public const int Success = 0, Failure = 1, BadUsage = 2;

    public static int Run(string[] args, TextWriter output, TextWriter error) => Run(args, Console.In, output, error);

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        try
        {
            if (args.Length == 0)
            {
                error.Write(Usage);
                return BadUsage;
            }

            switch (args[0])
            {
                case "-h" or "--help" or "help":
                    output.Write(Usage);
                    return Success;
                // The command forms (help, version) also work under dnx, which takes --help and
                // --version as its own options.
                case "--version" or "version":
                    output.WriteLine(Version);
                    return Success;
                case "render":
                    if (ParseRender(args[1..]) is not { } options)
                    {
                        output.Write(Usage);
                        return Success;
                    }
                    foreach (var file in Render(options, input, error))
                        output.WriteLine(file);
                    return Success;
                default:
                    throw new UsageException($"unknown command '{args[0]}'");
            }
        }
        catch (UsageException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            error.WriteLine("Run 'fluentreport --help' for usage.");
            return BadUsage;
        }
        catch (Exception ex) when (ex is CliException or IOException or UnauthorizedAccessException
                                       or XmlException or NotSupportedException or InvalidOperationException)
        {
            // Expected failures: a missing or unreadable file, an invalid report, an expression that
            // --strict rejects. The message says what is wrong; a stack trace would only hide it.
            error.WriteLine($"error: {ex.Message}");
            return Failure;
        }
        catch (Exception ex)
        {
            error.WriteLine($"error: {ex}");
            return Failure;
        }
    }

    // ── Arguments ────────────────────────────────────────────────────────────

    /// <summary>Reads the arguments of <c>render</c>; <c>null</c> when they ask for the help.</summary>
    internal static RenderOptions? ParseRender(IReadOnlyList<string> args)
    {
        string? report = null, output = null, format = null;
        CultureInfo? culture = null;
        var data = new List<DataArgument>();
        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        bool strict = false, externalImages = true;
        float scale = 2;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            string? inline = null;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.IndexOf('=') is > 2 and var eq)
            {
                inline = arg[(eq + 1)..];
                arg = arg[..eq];
            }

            string Value()
            {
                if (inline != null) return inline;
                if (++i < args.Count) return args[i];
                throw new UsageException($"{arg} needs a value");
            }

            void Flag()
            {
                if (inline != null) throw new UsageException($"{arg} takes no value");
            }

            switch (arg)
            {
                case "-h" or "--help":
                    return null;
                case "-o" or "--output":
                    output = Value();
                    break;
                case "-f" or "--format":
                    format = Value();
                    break;
                case "-d" or "--data":
                    data.Add(ParseData(Value()));
                    break;
                case "-p" or "--param":
                {
                    var (name, value) = SplitPair(Value(), arg);
                    parameters[name] = value;
                    break;
                }
                case "--culture":
                    culture = ParseCulture(Value());
                    break;
                case "--scale":
                {
                    var text = Value();
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out scale)
                        || !float.IsFinite(scale) || scale <= 0 || scale > 16)
                        throw new UsageException($"--scale must be a number greater than 0 and at most 16, not '{text}'");
                    break;
                }
                case "--strict":
                    Flag();
                    strict = true;
                    break;
                case "--no-external-images":
                    Flag();
                    externalImages = false;
                    break;
                default:
                    if (arg.Length > 1 && arg[0] == '-')
                        throw new UsageException($"unknown option '{arg}'");
                    if (report != null)
                        throw new UsageException($"unexpected argument '{arg}': render takes one report");
                    report = arg;
                    break;
            }
        }

        if (report == null)
            throw new UsageException("missing the report file (.rdlc)");

        OutputFormat resolved;
        if (format != null)
            resolved = ParseFormat(format)
                ?? throw new UsageException($"unknown format '{format}': use pdf, png, html or xlsx");
        else if (output != null)
            resolved = ParseFormat(Path.GetExtension(output).TrimStart('.'))
                ?? throw new UsageException($"cannot tell the format of '{output}': end it in .pdf, .png, .html or .xlsx, or pass --format");
        else
            resolved = OutputFormat.Pdf;

        // Without -o, the output goes to the current directory, named after the report.
        output ??= Path.GetFileNameWithoutExtension(report) + Extension(resolved);

        return new RenderOptions(report, output, resolved, data, parameters, culture, strict, externalImages, scale);
    }

    /// <summary><c>Name=file.json</c> names the dataset; a bare path (or one whose prefix is not a name, such as <c>C:\data.json</c>) does not.</summary>
    internal static DataArgument ParseData(string value)
    {
        int eq = value.IndexOf('=');
        if (eq > 0 && IsName(value.AsSpan(0, eq)))
        {
            var path = value[(eq + 1)..];
            if (path.Length == 0) throw new UsageException($"--data {value}: missing the file after '='");
            return new DataArgument(value[..eq], path);
        }
        return new DataArgument(null, value);
    }

    private static (string Name, string Value) SplitPair(string value, string option)
    {
        int eq = value.IndexOf('=');
        if (eq <= 0) throw new UsageException($"{option} expects Name=value, not '{value}'");
        return (value[..eq], value[(eq + 1)..]);
    }

    private static bool IsName(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || !(char.IsLetter(text[0]) || text[0] == '_')) return false;
        foreach (var c in text)
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        return true;
    }

    private static CultureInfo ParseCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name, predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            throw new UsageException($"unknown culture '{name}' (examples: es-UY, en-US, pt-BR)");
        }
    }

    internal static OutputFormat? ParseFormat(string name) => name.ToLowerInvariant() switch
    {
        "pdf" => OutputFormat.Pdf,
        "png" => OutputFormat.Png,
        "html" or "htm" => OutputFormat.Html,
        "xlsx" or "excel" => OutputFormat.Excel,
        _ => null
    };

    private static string Extension(OutputFormat format) => format switch
    {
        OutputFormat.Png => ".png",
        OutputFormat.Html => ".html",
        OutputFormat.Excel => ".xlsx",
        _ => ".pdf"
    };

    // ── Rendering ────────────────────────────────────────────────────────────

    /// <summary>Imports the report with its data and writes it; returns the files written.</summary>
    internal static IReadOnlyList<string> Render(RenderOptions options, TextReader input, TextWriter error)
    {
        if (!File.Exists(options.Report))
            throw new CliException($"report not found: {options.Report}");

        var declared = DeclaredDatasets(options.Report);
        var datasets = JsonDatasets.Load(options.Data, declared, input);
        foreach (var name in datasets.Keys)
            if (declared.Count > 0 && !declared.Contains(name, StringComparer.OrdinalIgnoreCase))
                error.WriteLine($"warning: the report has no dataset named '{name}' (it has {string.Join(", ", declared)})");

        var factory = new RdlcDocumentFactory(
            datasets,
            options.Parameters.ToDictionary(StringComparer.OrdinalIgnoreCase),
            globals: null,
            new RdlcImportOptions
            {
                Culture = options.Culture,
                StrictExpressions = options.Strict,
                LoadExternalImages = options.ExternalImages,
            });
        var document = factory.ParseFromFile(options.Report);
        foreach (var warning in factory.Warnings)
            error.WriteLine($"warning: {warning}");

        return Write(document, options);
    }

    private static IReadOnlyList<string> Write(Document document, RenderOptions options)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(options.Output)) is { } directory)
            Directory.CreateDirectory(directory);

        switch (options.Format)
        {
            case OutputFormat.Html:
                document.GenerateHtml(options.Output);
                return [options.Output];
            case OutputFormat.Excel:
                document.GenerateExcel(options.Output);
                return [options.Output];
            case OutputFormat.Png:
            {
                var pages = document.GenerateImages(options.Scale);
                if (pages.Count == 1)
                {
                    File.WriteAllBytes(options.Output, pages[0]);
                    return [options.Output];
                }
                // One file per page: report.png, report-2.png, report-3.png, ...
                var files = new List<string>();
                string stem = Path.Combine(Path.GetDirectoryName(options.Output) ?? "", Path.GetFileNameWithoutExtension(options.Output));
                string extension = Path.GetExtension(options.Output) is { Length: > 0 } e ? e : ".png";
                for (int i = 0; i < pages.Count; i++)
                {
                    var file = i == 0 ? options.Output : $"{stem}-{i + 1}{extension}";
                    File.WriteAllBytes(file, pages[i]);
                    files.Add(file);
                }
                return files;
            }
            default:
                document.GeneratePdf(options.Output);
                return [options.Output];
        }
    }

    /// <summary>
    /// Dataset names the report uses: declared in <c>&lt;DataSets&gt;</c> or referenced by a data region.
    /// Read with the importer's settings (no DTD, no external resources).
    /// </summary>
    internal static IReadOnlyList<string> DeclaredDatasets(string reportPath)
    {
        var names = new List<string>();
        void Add(string? name)
        {
            name = name?.Trim();
            if (!string.IsNullOrEmpty(name) && !names.Contains(name, StringComparer.OrdinalIgnoreCase)) names.Add(name);
        }

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true };
        using var reader = XmlReader.Create(reportPath, settings);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;
            if (reader.LocalName == "DataSet") Add(reader.GetAttribute("Name"));
            else if (reader.LocalName == "DataSetName") Add(reader.ReadElementContentAsString());
        }
        return names;
    }

    // ── Help ─────────────────────────────────────────────────────────────────

    internal static string Version
    {
        get
        {
            var version = typeof(CliApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            int plus = version.IndexOf('+'); // drop the commit hash Source Link appends
            return plus >= 0 ? version[..plus] : version;
        }
    }

    internal static string Usage => $"""
        FluentReport {Version}: renders RDLC reports with JSON data to PDF, PNG, HTML or Excel.

        Usage:
          fluentreport render <report.rdlc> [options]
          fluentreport help | version

        Options:
          -o, --output <file>       Output file; its extension picks the format (.pdf, .png, .html,
                                    .xlsx). Default: the report's name with .pdf, in the current
                                    directory.
          -f, --format <format>     pdf, png, html or xlsx, when the output has another extension.
          -d, --data [Name=]<file>  Rows as JSON; repeat it for several datasets.
                                      Name=rows.json   an array of objects for dataset Name
                                      data.json        an object of datasets: {"Lines": [...]},
                                                       or an array of objects when the report has
                                                       one dataset
                                    "-" as the file reads standard input.
          -p, --param Name=value    Report parameter (=Parameters!Name.Value); repeat it as needed.
              --culture <name>      Culture for numbers and dates, such as es-UY or en-US.
                                    Default: the report's <Language>, else invariant.
              --scale <n>           PNG pixels per point. Default 2 (144 dpi). A report with
                                    several pages writes name.png, name-2.png, ...
              --strict              Fail on an expression that cannot be evaluated, instead of
                                    leaving it empty with a warning.
              --no-external-images  Do not load images from files next to the report.
          -h, --help                Show this help.
              --version             Show the version.

        Exit codes: 0 done, 1 the report or its data could not be used, 2 wrong arguments.

        Examples:
          dnx FluentReport.Cli render invoice.rdlc --data Lines=lines.json -o invoice.pdf
          fluentreport render invoice.rdlc -d data.json -p Customer=ACME --culture es-UY -o invoice.html

        Under dnx, --help, --version and -v are read by dnx itself: use the help and version
        commands, or put -- after the package name (dnx FluentReport.Cli -- render --help).

        """;
}
