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
            var outputSarif = SarifReporter.AddTechDebtToSarif(inputSarif);
            File.WriteAllText(OutputPath, outputSarif);
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
}

