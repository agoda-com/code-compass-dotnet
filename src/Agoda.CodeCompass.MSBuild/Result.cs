namespace Agoda.CodeCompass.MSBuild;

public class Result
{
    public string RuleId { get; set; } = string.Empty;
    public Message Message { get; set; } = new();
    public Location[] Locations { get; set; } = Array.Empty<Location>();
    public TechDebtProperties Properties { get; set; } = new();
}