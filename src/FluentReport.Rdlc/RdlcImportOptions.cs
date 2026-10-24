using System.Globalization;

namespace FluentReport.Rdlc;

/// <summary>Options that control how an RDLC report definition is imported.</summary>
public sealed class RdlcImportOptions
{
    /// <summary>
    /// Culture used to format values (numbers, dates, <c>Format(x, "C2")</c>) and to read numbers
    /// and dates that arrive as text. When <c>null</c>, the report's <c>&lt;Language&gt;</c> is used
    /// if it is a literal culture name, otherwise <see cref="CultureInfo.InvariantCulture"/>, so the
    /// output never depends on the machine's regional settings.
    /// </summary>
    public CultureInfo? Culture { get; init; }

    /// <summary>
    /// Whether <c>&lt;Image&gt;</c> items with <c>Source = External</c> are loaded from disk. Default <c>true</c>.
    /// Images are only read from inside <see cref="ExternalImageDirectory"/>.
    /// </summary>
    public bool LoadExternalImages { get; init; } = true;

    /// <summary>
    /// Directory that external images must live in. When <c>null</c>, the directory of the
    /// <c>.rdlc</c> file is used for <c>FromRdlc(path)</c>; reports parsed from a stream or a string
    /// load no external images unless this is set. Paths that resolve outside it are ignored.
    /// </summary>
    public string? ExternalImageDirectory { get; init; }

    /// <summary>
    /// When <c>true</c>, an expression the importer cannot evaluate throws <see cref="NotSupportedException"/>.
    /// When <c>false</c> (default) it renders as empty text and is reported in
    /// <see cref="RdlcDocumentFactory.Warnings"/>.
    /// </summary>
    public bool StrictExpressions { get; init; }
}
