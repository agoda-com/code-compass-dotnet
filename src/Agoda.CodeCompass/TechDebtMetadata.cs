namespace Agoda.CodeCompass.Models.Agoda.CodeCompass.Data;

public static class TechDebtMetadata
{
    private static readonly Dictionary<string, TechDebtInfo> RuleMetadata = new()
    {
        ["CS8602"] = new TechDebtInfo 
        { 
            Minutes = 15, 
            Category = "NullableReferenceTypes", 
            Priority = "High",
            Rationale = "Null reference exceptions are a common source of runtime errors",
            Recommendation = "Use nullable reference types correctly to prevent null reference exceptions"
        },
        ["CA1822"] = new TechDebtInfo 
        { 
            Minutes = 10, 
            Category = "Performance", 
            Priority = "Medium",
            Rationale = "Non-static methods that don't access instance data create unnecessary allocations",
            Recommendation = "Mark methods that don't access instance state as static"
        }
    };

    public static TechDebtInfo? GetTechDebtInfo(string ruleId) => 
        RuleMetadata.TryGetValue(ruleId, out var info) ? info : null;

    public static void RegisterCustomRule(string ruleId, TechDebtInfo info)
    {
        RuleMetadata[ruleId] = info;
    }
}