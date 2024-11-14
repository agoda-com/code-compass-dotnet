namespace Agoda.CodeCompass.MSBuild;

public class PhysicalLocation
{
    public ArtifactLocation ArtifactLocation { get; set; } = new();
    public Region Region { get; set; } = new();
}