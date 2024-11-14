

using System.Text.Json;
using Agoda.CodeCompass.Models.Agoda.CodeCompass.Data;
using Microsoft.CodeAnalysis;

public class SarifReporter
{
    public static string GenerateSarifReport(IEnumerable<Diagnostic> diagnostics)
    {
        var sarifReport = new
        {
            schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Agoda.CodeCompass",
                            semanticVersion = "1.0.0",
                            informationUri = "https://agoda.github.io/code-compass",
                            rules = diagnostics
                                .Select(d => d.Descriptor)
                                .Distinct()
                                .Select(descriptor => new
                                {
                                    id = descriptor.Id,
                                    name = descriptor.Title.ToString(),
                                    shortDescription = new { text = descriptor.Title.ToString() },
                                    fullDescription = new { text = descriptor.Description.ToString() },
                                    help = new { text = descriptor.Description.ToString() },
                                    properties = GetTechDebtProperties(descriptor.Id)
                                })
                                .ToArray()
                        }
                    },
                    results = diagnostics.Select(d => new
                    {
                        ruleId = d.Id,
                        message = new { text = d.GetMessage() },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new
                                    {
                                        uri = d.Location.GetLineSpan().Path,
                                        uriBaseId = "%SRCROOT%"
                                    },
                                    region = new
                                    {
                                        startLine = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                                        startColumn = d.Location.GetLineSpan().StartLinePosition.Character + 1,
                                        endLine = d.Location.GetLineSpan().EndLinePosition.Line + 1,
                                        endColumn = d.Location.GetLineSpan().EndLinePosition.Character + 1
                                    }
                                }
                            }
                        },
                        properties = GetTechDebtProperties(d.Id)
                    }).ToArray()
                }
            }
        };

        return JsonSerializer.Serialize(sarifReport, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private static object GetTechDebtProperties(string ruleId)
    {
        var techDebtInfo = TechDebtMetadata.GetTechDebtInfo(ruleId);
        return new
        {
            techDebt = techDebtInfo == null ? null : new
            {
                techDebtInfo.Minutes,
                techDebtInfo.Category,
                techDebtInfo.Priority,
                techDebtInfo.Rationale,
                techDebtInfo.Recommendation
            }
        };
    }
}