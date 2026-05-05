namespace FluentReport.Rdlc;

/// <summary>
/// Extension methods that add RDLC import capabilities to <see cref="Document"/>.
/// </summary>
public static class DocumentRdlcExtensions
{
    /// <summary>
    /// Parses an RDLC report definition file and returns a <see cref="Document"/> ready for
    /// rendering (PDF, PNG, Excel, …).
    /// </summary>
    /// <param name="path">
    /// Absolute or relative path to the <c>.rdlc</c> file.
    /// </param>
    /// <param name="datasets">
    /// Optional dictionary mapping dataset names (as declared in the RDLC) to sequences of data
    /// rows. Each row may be a POCO (property values resolved via reflection) or an
    /// <see cref="IDictionary{TKey,TValue}"/> of string → object.
    /// </param>
    /// <param name="parameters">
    /// Optional dictionary of report parameter values, keyed by parameter name.
    /// These are used to resolve <c>=Parameters!X.Value</c> expressions.
    /// </param>
    /// <returns>A fully configured <see cref="Document"/> instance.</returns>
    public static Document FromRdlc(
        string path,
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new RdlcDocumentFactory(datasets, parameters);
        return factory.ParseFromFile(path);
    }

    /// <summary>
    /// Parses an RDLC report definition from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">Stream containing the RDLC XML.</param>
    /// <param name="datasets">Optional dataset rows (see overload with <paramref name="datasets"/>).</param>
    /// <param name="parameters">Optional report parameters.</param>
    public static Document FromRdlcStream(
        Stream stream,
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new RdlcDocumentFactory(datasets, parameters);
        return factory.ParseFromStream(stream);
    }

    /// <summary>
    /// Parses an RDLC report definition from an XML string.
    /// Useful for testing or when the RDLC content is embedded in code.
    /// </summary>
    /// <param name="xml">RDLC XML string.</param>
    /// <param name="datasets">Optional dataset rows.</param>
    /// <param name="parameters">Optional report parameters.</param>
    public static Document FromRdlcXml(
        string xml,
        IDictionary<string, IEnumerable<object>>? datasets = null,
        IDictionary<string, object>? parameters = null)
    {
        var factory = new RdlcDocumentFactory(datasets, parameters);
        return factory.ParseFromXml(xml);
    }
}
