using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agoda.CodeCompass.MSBuild.Tests;

public static class SetupTests
{
    public static void SetupSolutionAndProject(string tempSolutionDir)
    {
        // Create a minimal solution structure
        var projectDir = Path.Combine(tempSolutionDir, "TestProject");
        Directory.CreateDirectory(projectDir);

        // Create project file with explicit SDK reference
        var projectPath = Path.Combine(projectDir, "TestProject.csproj");
        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>library</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Agoda.Analyzers"" Version=""1.1.94-preview"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Microsoft.Build"" Version=""17.8.3"" ExcludeAssets=""runtime"" />
    <PackageReference Include=""Microsoft.Build.Framework"" Version=""17.8.3"" ExcludeAssets=""runtime"" />
    <PackageReference Include=""Microsoft.CodeAnalysis.Workspaces.MSBuild"" Version=""4.8.0"" />
    <PackageReference Include=""Microsoft.CodeAnalysis.CSharp.Workspaces"" Version=""4.8.0"" />
  </ItemGroup>
</Project>");

        // Create a test file with a diagnostic that should be reported
        var sourcePath = Path.Combine(projectDir, "Test.cs");
        File.WriteAllText(sourcePath, @"
using System;

public class Test
{
    // CS0649: Field is never assigned to
    private readonly string _unused;

    public void Method()
    {
        // CS0219: Variable is assigned but its value is never used
        int unused = 1;
    }
}");
        // Create solution file that references the project
        var projectGuid = Guid.NewGuid().ToString("B").ToUpper();
        var solutionPath = Path.Combine(tempSolutionDir, "test.sln");
        File.WriteAllText(solutionPath, $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""TestProject"", ""TestProject\TestProject.csproj"", ""{projectGuid}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal");
    }
}