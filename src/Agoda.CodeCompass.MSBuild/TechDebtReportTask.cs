using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Task = Microsoft.Build.Utilities.Task;

namespace Agoda.CodeCompass.MSBuild;

public class TechDebtReportTask : Task
{
    [Required]
    public string[] AnalyzerAssemblyPaths { get; set; } = Array.Empty<string>();

    [Required]
    public string[] CompilationAssemblyPaths { get; set; } = Array.Empty<string>();

    [Required]
    public string[] SourceFiles { get; set; } = Array.Empty<string>();

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            // Load all analyzer assemblies
            var analyzers = LoadAnalyzers();
            
            // Create compilation
            var compilation = CreateCompilation();
            
            // Run analysis
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create(analyzers.ToArray()));
            
            var diagnostics = compilationWithAnalyzers
                .GetAnalyzerDiagnosticsAsync()
                .GetAwaiter()
                .GetResult();

            // Generate and save SARIF report
            var sarifOutput = SarifReporter.GenerateSarifReport(diagnostics);
            File.WriteAllText(OutputPath, sarifOutput);

            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to generate tech debt report: {ex.Message}");
            return false;
        }
    }

    private IEnumerable<DiagnosticAnalyzer> LoadAnalyzers()
    {
        foreach (var path in AnalyzerAssemblyPaths)
        {
            var assembly = System.Runtime.Loader.AssemblyLoadContext
                .Default
                .LoadFromAssemblyPath(path);

            var analyzerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && 
                           typeof(DiagnosticAnalyzer).IsAssignableFrom(t));

            foreach (var analyzerType in analyzerTypes)
            {
                if (Activator.CreateInstance(analyzerType) is DiagnosticAnalyzer analyzer)
                {
                    yield return analyzer;
                }
            }
        }
    }

    private Compilation CreateCompilation()
    {
        var syntaxTrees = SourceFiles
            .Select(file => Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree
                .ParseText(File.ReadAllText(file), path: file))
            .ToArray();

        var references = CompilationAssemblyPaths
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

        return Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TechDebtAnalysis",
            syntaxTrees,
            references,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                (OutputKind)Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest));
    }
}