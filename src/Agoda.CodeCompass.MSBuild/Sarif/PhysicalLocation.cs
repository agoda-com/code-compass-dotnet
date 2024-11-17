namespace Agoda.CodeCompass.MSBuild.Sarif;

public class PhysicalLocation
{
    public ArtifactLocation ArtifactLocation { get; set; } = new();
    public Region Region { get; set; } = new();
}