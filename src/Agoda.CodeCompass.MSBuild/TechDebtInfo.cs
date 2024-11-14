namespace Agoda.CodeCompass.Models;

public class TechDebtInfo
{
    public required int Minutes { get; init; }
    public required string Category { get; init; }
    public required string Priority { get; init; }
    public string? Rationale { get; init; }
    public string? Recommendation { get; init; }
}