using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text.Json;
using Task = Microsoft.Build.Utilities.Task;

namespace Agoda.CodeCompass.MSBuild;
// TechDebtSarifTask.cs
public class TechDebtSarifTask : Task
{
    [Required]
    public string InputPath { get; set; } = string.Empty;

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            var inputSarif = File.ReadAllText(InputPath);
            var inputDiagnostics = ParseSarifDiagnostics(inputSarif);
            var techDebtSarif = SarifReporter.GenerateSarifReport(inputDiagnostics);
            File.WriteAllText(OutputPath, techDebtSarif);
            return true;
        }
        catch (InvalidDataException iex)
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to process SARIF report: {ex.Message}");
            return false;
        }
    }

    private IEnumerable<Diagnostic> ParseSarifDiagnostics(string sarifContent)
    {
        // Parse SARIF JSON into list of Diagnostics
        var sarif = JsonSerializer.Deserialize<SarifReport>(sarifContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return sarif.Runs.SelectMany(r => r.Results)
            .Select(r => CreateDiagnosticFromSarif(r));
    }

    private Diagnostic CreateDiagnosticFromSarif(Result result)
    {
        if (result.Locations.Length == 0) throw new InvalidDataException("Sarif input was wrong");

        var lineSpan = result.Locations.FirstOrDefault()?.PhysicalLocation?.Region;
        var linePosition = new LinePositionSpan(
            new LinePosition(
                (lineSpan?.StartLine ?? 1) - 1,
                (lineSpan?.StartColumn ?? 1) - 1),
            new LinePosition(
                (lineSpan?.EndLine ?? 1) - 1,
                (lineSpan?.EndColumn ?? 1) - 1)
        );

        var filePath = result.Locations.FirstOrDefault()?.PhysicalLocation?.ArtifactLocation?.Uri ?? "";
        var sourceText = SourceText.From(File.ReadAllText(filePath));
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);

        var location = Microsoft.CodeAnalysis.Location.Create(
            syntaxTree,
            new TextSpan(0, 0));

        var descriptor = new DiagnosticDescriptor(
            id: result.RuleId,
            title: result.Message.Text,
            messageFormat: result.Message.Text,
            category: result.Properties?.TechDebt?.Category ?? "Default",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        if (result.Properties?.TechDebt != null)
        {
            properties.Add("techDebtMinutes", result.Properties.TechDebt.Minutes.ToString());
            properties.Add("techDebtCategory", result.Properties.TechDebt.Category);
            properties.Add("techDebtPriority", result.Properties.TechDebt.Priority);
            properties.Add("techDebtRationale", result.Properties.TechDebt.Rationale);
            properties.Add("techDebtRecommendation", result.Properties.TechDebt.Recommendation);
        }

        return Diagnostic.Create(
            descriptor,
            location,
            properties.ToImmutable(),
            result.Message.Text);
    }
}