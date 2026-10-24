namespace FluentReport.Elements;

/// <summary>
/// Marker interface for every node of the document model. Elements carry data only;
/// each renderer (HTML, Excel, …) decides how to lay them out.
/// </summary>
public interface IElement;
