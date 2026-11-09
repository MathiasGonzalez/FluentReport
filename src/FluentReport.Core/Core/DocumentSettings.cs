namespace FluentReport.Core;

/// <summary>The document model: the list of page definitions.</summary>
public class DocumentSettings
{
    /// <summary>Page definitions, in output order. Each one produces as many pages as its content needs.</summary>
    public List<PageSettings> Pages { get; } = new();
}
