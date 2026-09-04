namespace TextBanner.Models;

public record ComboItem(string Label, object Value)
{
    public override string ToString() => Label;
}
