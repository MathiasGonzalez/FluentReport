namespace FluentReport.Elements;

public enum HorizontalAlignment { Left, Center, Right }

public class AlignElement : IElement
{
    public IElement? Child { get; set; }
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
}
