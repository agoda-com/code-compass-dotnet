using System.Text.Json;
using Agoda.CodeCompass.MSBuild.Sarif;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Shouldly;

namespace Agoda.CodeCompass.MSBuild.Tests;

[TestFixture]
public class SarifConversionTests
{
    private readonly string _writeSarifPath = "TestData/write.sarif";
    private readonly string _sampleSarifPath = "TestData/sample.sarif";
    private readonly IBuildEngine _buildEngine = Substitute.For<IBuildEngine>();

    [Test]
    public async Task ConvertSarif_WithValidInput_ShouldAddTechDebtProperties()
    {
        var outfile = "TestData/" + Guid.NewGuid();
        // Arrange
        var task = new TechDebtSarifTask
        {
            InputPath = _sampleSarifPath,
            OutputPath = outfile,
            BuildEngine = _buildEngine
        };

        // Act
        var result = task.Execute();

        // Assert
        result.ShouldBeTrue();

        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonSerializer.Deserialize<SarifReport>(outputJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

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
    public void ConvertSarif_WithInvalidPath_ShouldReturnFalse()
    {
        var task = new TechDebtSarifTask
        {
            InputPath = "TestData/invalid.sarif",
            OutputPath = Guid.NewGuid().ToString(),
            BuildEngine = _buildEngine
        };
        
        var result = task.Execute();

        result.ShouldBeFalse();
    }

    [Test]
    public async Task ConvertSarif_WithV1FromTestData_ShouldHave1Violation()
    {
        var outfile = "TestData/" + Guid.NewGuid();
        var task = new TechDebtSarifTask
        {
            InputPath = "TestData/v1.sarif",
            OutputPath = outfile,
            BuildEngine = _buildEngine
        };

        var result = task.Execute();

        result.ShouldBeTrue();

        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonSerializer.Deserialize<SarifReport>(outputJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        output.Runs[0].Results.Count.ShouldBe(1);

        var results = output.Runs[0].Results;
        results[0].RuleId.ShouldBe("CA1707");

    }
    [Test]
    public async Task ConvertSarif_WithMultipleRules_ShouldPreserveRuleMetadata()
    {
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

        await File.WriteAllTextAsync(_writeSarifPath,
            JsonSerializer.Serialize(sarif, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var outfile = "TestData/" + Guid.NewGuid();
        var task = new TechDebtSarifTask
        {
            InputPath = _writeSarifPath,
            OutputPath = outfile,
            BuildEngine = _buildEngine
        };

        // Act
        task.Execute();

        // Assert
        var outputJson = await File.ReadAllTextAsync(outfile);
        var output = JsonSerializer.Deserialize<SarifReport>(outputJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

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