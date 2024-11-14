namespace Agoda.CodeCompass.MSBuild; 

public class ToolDriver
{
    public string Name { get; set; } = "Agoda.CodeCompass";
    public string SemanticVersion { get; set; } = "1.0.0";
    public string InformationUri { get; set; } = "https://agoda.github.io/code-compass";
    public Rule[] Rules { get; set; } = Array.Empty<Rule>();
}