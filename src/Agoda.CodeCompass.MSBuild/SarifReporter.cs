using System.Text.Json;
using Agoda.CodeCompass.Data;
using Agoda.CodeCompass.MSBuild.Sarif;
using Microsoft.CodeAnalysis;
using Location = Agoda.CodeCompass.MSBuild.Sarif.Location;

namespace Agoda.CodeCompass.MSBuild;

public class SarifReporter
{
    public static string GenerateSarifReport(IEnumerable<Diagnostic> diagnostics)
    {
        var report = new SarifReport
        {
            Runs = new[]
            {
                new Run
                {
                    Tool = new Tool
                    {
                        Driver = new ToolDriver
                        {
                            Rules = diagnostics
                                .Select(d => d.Descriptor)
                                .Distinct()
                                .Select(d => new Rule
                                {
                                    Id = d.Id,
                                    Name = d.Title.ToString(),
                                    ShortDescription = new Message { Text = d.Title.ToString() },
                                    FullDescription = new Message { Text = d.Description.ToString() },
                                    Help = new Message { Text = d.Description.ToString() },
                                    Properties = GetTechDebtProperties(d.Id)
                                })
                                .ToArray()
                        }
                    },
                    Results = diagnostics.Select(d => new Result
                    {
                        RuleId = d.Id,
                        Message = new Message { Text = d.GetMessage() },
                        Locations = new[]
                        {
                            new Location
                            {
                                PhysicalLocation = new PhysicalLocation
                                {
                                    ArtifactLocation = new ArtifactLocation
                                    {
                                        Uri = d.Location.GetLineSpan().Path
                                    },
                                    Region = new Region
                                    {
                                        StartLine = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                                        StartColumn = d.Location.GetLineSpan().StartLinePosition.Character + 1,
                                        EndLine = d.Location.GetLineSpan().EndLinePosition.Line + 1,
                                        EndColumn = d.Location.GetLineSpan().EndLinePosition.Character + 1
                                    }
                                }
                            }
                        },
                        Properties = GetTechDebtProperties(d.Id)
                    }).ToList()
                }
            }
        };

        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static TechDebtProperties GetTechDebtProperties(string ruleId)
    {
        var techDebtInfo = TechDebtMetadata.GetTechDebtInfo(ruleId);
        return new TechDebtProperties
        {
            TechDebt = techDebtInfo
        };
    }
}