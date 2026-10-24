namespace FluentReport.Rdlc;

/// <summary>
/// RDLC import entry points, available as <c>Document.FromRdlc(...)</c>, <c>Document.FromRdlcStream(...)</c>
/// and <c>Document.FromRdlcXml(...)</c>. Use <see cref="RdlcDocumentFactory"/> directly to read
/// <see cref="RdlcDocumentFactory.Warnings"/>.
/// </summary>
public static class DocumentRdlcExtensions
{
    extension(Document)
    {
        /// <summary>Parses an RDLC report definition file into a <see cref="Document"/>.</summary>
        /// <param name="path">Path to the <c>.rdlc</c> file. External images are resolved from its directory.</param>
        /// <param name="datasets">
        /// Dataset name (as declared in the RDLC) → rows. A row may be a POCO, an
        /// <see cref="IDictionary{TKey,TValue}"/> of string → object, or a non-generic dictionary.
        /// </param>
        /// <param name="parameters">Values for <c>=Parameters!X.Value</c>.</param>
        /// <param name="globals">Values for <c>=Globals!X.Value</c> (e.g. <c>ReportName</c>).</param>
        /// <param name="options">Culture, external image and strictness options.</param>
        public static Document FromRdlc(
            string path,
            IDictionary<string, IEnumerable<object>>? datasets = null,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? globals = null,
            RdlcImportOptions? options = null)
            => new RdlcDocumentFactory(datasets, parameters, globals, options).ParseFromFile(path);

        /// <summary>Parses an RDLC report definition from a stream. See <see cref="FromRdlc"/> for the parameters.</summary>
        public static Document FromRdlcStream(
            Stream stream,
            IDictionary<string, IEnumerable<object>>? datasets = null,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? globals = null,
            RdlcImportOptions? options = null)
            => new RdlcDocumentFactory(datasets, parameters, globals, options).ParseFromStream(stream);

        /// <summary>Parses an RDLC report definition from an XML string. See <see cref="FromRdlc"/> for the parameters.</summary>
        public static Document FromRdlcXml(
            string xml,
            IDictionary<string, IEnumerable<object>>? datasets = null,
            IDictionary<string, object>? parameters = null,
            IDictionary<string, object>? globals = null,
            RdlcImportOptions? options = null)
            => new RdlcDocumentFactory(datasets, parameters, globals, options).ParseFromXml(xml);
    }
}
