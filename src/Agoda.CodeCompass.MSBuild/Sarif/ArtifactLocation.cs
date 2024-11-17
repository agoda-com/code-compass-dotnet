namespace Agoda.CodeCompass.MSBuild.Sarif;

public class ArtifactLocation
{
    public string Uri { get; set; } = string.Empty;
    public string UriBaseId { get; set; } = "%SRCROOT%";
}