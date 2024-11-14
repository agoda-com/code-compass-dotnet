using Agoda.CodeCompass.MSBuild;

public class Rule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Message ShortDescription { get; set; } = new();
    public Message FullDescription { get; set; } = new();
    public Message Help { get; set; } = new();
    public TechDebtProperties Properties { get; set; } = new();
}