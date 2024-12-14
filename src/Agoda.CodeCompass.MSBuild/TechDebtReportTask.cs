using Agoda.CodeCompass.Data;
using Agoda.CodeCompass.MSBuild.Sarif;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
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
    
    public string SolutionPath { get; set; } = string.Empty;

    private string ResolveSolutionPath()
    {
        if (!string.IsNullOrEmpty(SolutionPath))
        {
            return SolutionPath;
        }

        var solutionDir = Environment.GetEnvironmentVariable("SolutionDir");
        if (string.IsNullOrEmpty(solutionDir))
        {
            throw new InvalidOperationException("Neither SolutionPath property nor SolutionDir environment variable is set.");
        }

        var solutionFile = Directory.EnumerateFiles(solutionDir, "*.sln").FirstOrDefault();
        if (solutionFile == null)
        {
            throw new FileNotFoundException("Solution file not found.", solutionDir);
        }

        return solutionFile;
    }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Normal, "Running TechDebtSarifTask");
        try
        {
            var inputSarif = File.ReadAllText(InputPath);
            var solutionPath = ResolveSolutionPath();
            if (string.IsNullOrEmpty(solutionPath))
            {
                throw new InvalidOperationException("SolutionDir is not set.");
            }
            var solutionFile = Directory.EnumerateFiles(solutionPath, "*.sln").FirstOrDefault();
            if (solutionFile == null)
            {
                throw new InvalidOperationException("Solution file not found in SolutionDir.");
            }

            using var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(solutionFile).Result;

            var allDiagnostics = new List<Diagnostic>();
            foreach (var project in solution.Projects)
            {
                var compilation = project.GetCompilationAsync().Result;
                if (compilation != null)
                {
                    allDiagnostics.AddRange(compilation.GetDiagnostics());
                }
            }

            // Update metadata with the diagnostics
            TechDebtMetadata.UpdateMetadataFromDiagnostics(allDiagnostics);

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