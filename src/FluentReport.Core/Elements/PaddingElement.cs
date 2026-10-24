namespace FluentReport.Elements;

public class PaddingElement : IElement
{
    public IElement? Child { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
    public float Right { get; set; }
}
