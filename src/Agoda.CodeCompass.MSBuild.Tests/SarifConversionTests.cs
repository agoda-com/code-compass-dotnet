using Agoda.CodeCompass.MSBuild.Sarif;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Shouldly;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Agoda.CodeCompass.MSBuild.Tests;

[TestFixture]
public class SarifConversionTests
{
    private readonly string _writeSarifPath = "TestData/write.sarif";
    private readonly string _sampleSarifPath = "TestData/sample.sarif";
    private readonly IBuildEngine _buildEngine = Substitute.For<IBuildEngine>();
    private JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Error = HandleDeserializationError,
        Formatting = Formatting.Indented,
    };

    public SarifConversionTests()
    {
        _tempSolutionDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempSolutionDir);
    }
    private string _tempSolutionDir;

    [Test]
    public async Task ConvertSarif_WithValidInput_ShouldAddTechDebtProperties()
    {
        SetupTests.SetupSolutionAndProject(_tempSolutionDir);
        var outfile = "TestData/" + Guid.NewGuid();
        // Arrange
        var task = new TechDebtSarifTask
        {
            InputPath = _sampleSarifPath,
            OutputPath = outfile,
            BuildEngine = _buildEngine,
            SolutionPath = _tempSolutionDir
        };

        // Act
        task.Execute().ShouldBeTrue();

        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Error = HandleDeserializationError

        };
        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonConvert.DeserializeObject<SarifReport>(outputJson, jsonSettings);

        output.ShouldNotBeNull();
        output.Runs.ShouldNotBeEmpty();
        output.Runs[0].Results.ShouldNotBeEmpty();

        var firstResult = output.Runs[0].Results[0];
        firstResult.Properties.ShouldNotBeNull();
        firstResult.Properties.TechDebt.ShouldNotBeNull();
        firstResult.Properties.TechDebt.Minutes.ShouldBeGreaterThan(0);
        firstResult.Properties.TechDebt.Category.ShouldNotBeNullOrWhiteSpace();
        firstResult.Properties.TechDebt.Priority.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task ConvertSarif_WithV1FromTestData_ShouldHave1Violation()
    {
        SetupTests.SetupSolutionAndProject(_tempSolutionDir);
        var outfile = "TestData/" + Guid.NewGuid();
        var task = new TechDebtSarifTask
        {
            InputPath = "TestData/v1.sarif",
            OutputPath = outfile,
            BuildEngine = _buildEngine,
            SolutionPath = _tempSolutionDir
        };

        task.Execute().ShouldBeTrue();

        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonConvert.DeserializeObject<SarifV1Report>(outputJson, _jsonSettings);

        output.Runs[0].Results.Length.ShouldBe(1);

        var results = output.Runs[0].Results;
        results[0].RuleId.ShouldBe("CA1707");

    }

    private static void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
    {
        // Log the error but don't throw it
        var currentError = errorArgs.ErrorContext.Error.Message;
        Console.WriteLine($"Warning during SARIF processing: {currentError}");
        errorArgs.ErrorContext.Handled = true;
    }

    [Test]
    public async Task ConvertSarif_WithMultipleRules_ShouldPreserveRuleMetadata()
    {
        SetupTests.SetupSolutionAndProject(_tempSolutionDir);
        // Arrange
        var sarif = new SarifReport
        {
            Runs = new[]
            {
               new Run
               {
                   Results = new List<Result>
                   {
                       CreateSampleResult("CS8602"),
                       CreateSampleResult("CA1822")
                   }
               }
           }
        };

        var outfile = "TestData/" + Guid.NewGuid();
        await File.WriteAllTextAsync(_writeSarifPath,
            JsonConvert.SerializeObject(sarif, _jsonSettings));

        var task = new TechDebtSarifTask
        {
            InputPath = _writeSarifPath,
            OutputPath = outfile,
            BuildEngine = _buildEngine,
            SolutionPath = _tempSolutionDir
        };

        // Act
        task.Execute().ShouldBeTrue();

        // Assert
        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonConvert.DeserializeObject<SarifReport>(outputJson, _jsonSettings);

        output.ShouldNotBeNull();
        output.Runs[0].Results.Count.ShouldBe(2);

        var results = output.Runs[0].Results;
        results[0].RuleId.ShouldBe("CS8602");
        results[1].RuleId.ShouldBe("CA1822");

        results[0].Properties.TechDebt.Category.ShouldBe("NullableReference");
        results[1].Properties.TechDebt.Category.ShouldBe("Performance");
    }

    private static Result CreateSampleResult(string ruleId) => new()
    {
        RuleId = ruleId,
        Message = new Message { Text = $"Sample message for {ruleId}" },
        Locations = new[]
        {
           new Location
           {
               PhysicalLocation = new PhysicalLocation
               {
                   ArtifactLocation = new ArtifactLocation
                   {
                       Uri = "Test.cs"
                   },
                   Region = new Region
                   {
                       StartLine = 1,
                       StartColumn = 1,
                       EndLine = 1,
                       EndColumn = 1
                   }
               }
           }
       }
    };

    [OneTimeSetUp]
    public void Setup()
    {
        string[] lines = { "public class myClass {", "// comments", "}" };

        using (StreamWriter outputFile = new StreamWriter("Test.cs"))
        {
            foreach (string line in lines)
                outputFile.WriteLine(line);
        }
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        if (Directory.Exists("TestData"))
        {
            Directory.Delete("TestData", true);
        }
    }
}