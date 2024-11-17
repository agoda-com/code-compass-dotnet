using Agoda.CodeCompass.MSBuild.Sarif;
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

public class TechDebtSarifTask : Task
{
    [Required]
    public string InputPath { get; set; } = string.Empty;

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Normal, "Running TechDebtSarifTask");
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
            Log.LogMessage("InvalidDataException was thrown");
            return false;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to process SARIF report: {ex.Message}");
            return false;
        }
    }

    private Diagnostic CreateDiagnosticFromSarifV2(Result result)
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

    public IEnumerable<Diagnostic> ParseSarifDiagnostics(string sarifContent)
    {
        // Detect SARIF version from the JSON
        using var doc = JsonDocument.Parse(sarifContent);
        var version = doc.RootElement.GetProperty("version").GetString();

        return version switch
        {
            "1.0.0" => ParseSarifV1(sarifContent),
            "2.1.0" => ParseSarifV2(sarifContent),
            _ => throw new NotSupportedException($"Unsupported SARIF version: {version}")
        };
    }

    private IEnumerable<Diagnostic> ParseSarifV1(string sarifContent)
    {
        var sarif = JsonSerializer.Deserialize<SarifV1Report>(sarifContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return sarif.Runs.SelectMany(r => r.Results)
            .Select(r => CreateDiagnosticFromSarifV1(r));
    }

    private IEnumerable<Diagnostic> ParseSarifV2(string sarifContent)
    {
        var sarif = JsonSerializer.Deserialize<SarifReport>(sarifContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return sarif.Runs.SelectMany(r => r.Results)
            .Select(r => CreateDiagnosticFromSarifV2(r));
    }

    private Diagnostic CreateDiagnosticFromSarifV1(V1Result result)
    {
        var lineSpan = result.Locations.FirstOrDefault()?.ResultFile?.Region;
        var linePosition = new LinePositionSpan(
            new LinePosition(
                (lineSpan?.StartLine ?? 1) - 1,
                (lineSpan?.StartColumn ?? 1) - 1),
            new LinePosition(
                (lineSpan?.EndLine ?? 1) - 1,
                (lineSpan?.EndColumn ?? 1) - 1)
        );

        var filePath = result.Locations.FirstOrDefault()?.ResultFile?.Uri ?? "";
        var sourceText = SourceText.From(File.ReadAllText(filePath));
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var location = Microsoft.CodeAnalysis.Location.Create(
            syntaxTree,
            new TextSpan(0, 0));

        var descriptor = new DiagnosticDescriptor(
            id: result.RuleId,
            title: result.Message,
            messageFormat: result.Message,
            category: result.Properties?.TechDebt?.Category ?? "Default",
            defaultSeverity: MapV1SeverityToDiagnosticSeverity(result.Level),
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
            result.Message);
    }

    private DiagnosticSeverity MapV1SeverityToDiagnosticSeverity(string level)
    {
        return level?.ToLowerInvariant() switch
        {
            "error" => DiagnosticSeverity.Error,
            "warning" => DiagnosticSeverity.Warning,
            "info" => DiagnosticSeverity.Info,
            "note" => DiagnosticSeverity.Hidden,
            _ => DiagnosticSeverity.Warning
        };
    }
}

