namespace Agoda.CodeCompass.MSBuild.Sarif;

public class V1Result
{
    public string RuleId { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public V1Location[] Locations { get; set; } = Array.Empty<V1Location>();
    public TechDebtProperties Properties { get; set; } = new();
}