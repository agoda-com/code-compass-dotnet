using Agoda.CodeCompass.Data;
using Agoda.CodeCompass.MSBuild;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.MSBuild;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System.Reflection;
using System.Text.Json;
using Agoda.CodeCompass.MSBuild.Sarif;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Agoda.CodeCompass.MSBuild.Tests;

[TestFixture]
public class TechDebtSarifTaskTests
{
    private string _tempInputPath;
    private string _tempOutputPath;
    private string _tempSolutionDir;
    private IBuildEngine _buildEngine;
    private MSBuildWorkspace _workspace;
    private JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Error = HandleDeserializationError,
        Formatting = Formatting.Indented,
    };

    private static void HandleDeserializationError(object? sender, ErrorEventArgs e)
    {
        Console.WriteLine(e.ErrorContext.Error.ToString());
    }

    static TechDebtSarifTaskTests()
    {
        // Register assembly resolution handler for Microsoft.Build assemblies
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var requestedAssembly = new AssemblyName(args.Name);

            // If it's looking for an older version of Microsoft.Build.Framework, redirect to the new one
            if (requestedAssembly.Name == "Microsoft.Build.Framework" && requestedAssembly.Version.Major == 15)
            {
                return Assembly.Load("Microsoft.Build.Framework, Version=17.8.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }

            // If it's looking for an older version of Microsoft.Build, redirect to the new one
            if (requestedAssembly.Name == "Microsoft.Build" && requestedAssembly.Version.Major == 15)
            {
                return Assembly.Load("Microsoft.Build, Version=17.8.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }

            return null;
        };
    }
    [SetUp]
    public void Setup()
    {
        // Create temporary directories and files
        _tempSolutionDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempSolutionDir);

        // Set up temporary paths for input/output
        _tempInputPath = Path.Combine(_tempSolutionDir, "input.sarif");
        _tempOutputPath = Path.Combine(_tempSolutionDir, "output.sarif");

        // Set up substitute build engine
        _buildEngine = Substitute.For<IBuildEngine>();

        // Set environment variable
        Environment.SetEnvironmentVariable("SolutionDir", _tempSolutionDir);

        // Configure MSBuildWorkspace
        var properties = new Dictionary<string, string>
        {
            { "Configuration", "Debug" },
            { "Platform", "AnyCPU" }
        };

        _workspace = MSBuildWorkspace.Create(properties);

        // Subscribe to workspace failed events for debugging
        _workspace.WorkspaceFailed += (sender, args) =>
        {
            Console.WriteLine($"Workspace failure: {args.Diagnostic.Message}");
        };
    }

    [TearDown]
    public void TearDown()
    {
        _workspace?.Dispose();

        if (Directory.Exists(_tempSolutionDir))
        {
            Directory.Delete(_tempSolutionDir, true);
        }

        Environment.SetEnvironmentVariable("SolutionDir", null);
    }

    [Test]
    public void Execute_ShouldUpdateTechDebtMetadata()
    {
        // Arrange
        SetupTests.SetupSolutionAndProject(_tempSolutionDir);

        var minimalSarifContent = @"{
            ""$schema"": ""https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json"",
            ""version"": ""2.1.0"",
            ""runs"": [
                {
                    ""tool"": {
                        ""driver"": {
                            ""name"": ""Test Tool"",
                            ""rules"": [
                                {
                                    ""id"": ""CS0649"",
                                    ""properties"": {
                                        ""techDebtMinutes"": ""30"",
                                        ""category"": ""TestCategory"",
                                        ""priority"": ""High""
                                    }
                                }
                            ]
                        }
                    },
                    ""results"": [
                        {
                            ""ruleId"": ""CS0649"",
                            ""ruleId"": ""AG0005""
                        }
                    ]
                }
            ]
        }";

        File.WriteAllText(_tempInputPath, minimalSarifContent);

        var task = new TechDebtSarifTask
        {
            BuildEngine = _buildEngine,
            InputPath = _tempInputPath,
            OutputPath = _tempOutputPath,
            SolutionPath = _tempSolutionDir
        };

        task.Execute().ShouldBeTrue();

        File.Exists(_tempOutputPath).ShouldBeTrue();
        _buildEngine.DidNotReceive().LogErrorEvent(Arg.Any<BuildErrorEventArgs>());

        var outputContent = File.ReadAllText(_tempOutputPath);
        var outputDoc = JsonConvert.DeserializeObject<SarifReport>(outputContent, _jsonSettings);

        // Verify the tech debt info was updated
        var techDebtInfo = outputDoc.Runs[0].Results[0].Properties.TechDebt;
        techDebtInfo.ShouldNotBeNull();
        techDebtInfo.Minutes.ShouldBe(10);
        techDebtInfo.Category.ShouldBe("Compiler");
        techDebtInfo.Priority.ShouldBe("Low");
        // Verify the tech debt info was updated
        techDebtInfo = outputDoc.Runs[0].Tool.Driver.Rules.FirstOrDefault(x => x.Id == "AG0005").Properties.TechDebt;
        techDebtInfo.ShouldNotBeNull();
        techDebtInfo.Minutes.ShouldBe(10);
    }
}