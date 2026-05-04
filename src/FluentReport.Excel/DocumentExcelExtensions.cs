namespace FluentReport.Excel;

/// <summary>
/// Extension methods that add Excel generation capabilities to <see cref="Document"/>.
/// </summary>
public static class DocumentExcelExtensions
{
    /// <summary>Generates an Excel (.xlsx) file and saves it to the given path.</summary>
    public static void GenerateExcel(this Document document, string filePath)
    {
        using var stream = File.Create(filePath);
        document.GenerateExcel(stream);
    }

    /// <summary>Generates an Excel (.xlsx) file and writes it to the given stream.</summary>
    public static void GenerateExcel(this Document document, Stream stream)
    {
        var renderer = new ExcelDocumentRenderer(document.Settings);
        renderer.RenderToStream(stream);
    }

    /// <summary>Generates an Excel (.xlsx) file and returns the raw bytes.</summary>
    public static byte[] GenerateExcel(this Document document)
    {
        using var ms = new MemoryStream();
        document.GenerateExcel(ms);
        return ms.ToArray();
    }
}
