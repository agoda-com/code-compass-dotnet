namespace Agoda.CodeCompass.MSBuild.Sarif;

public class V1ResultFile
{
    public string Uri { get; set; } = string.Empty;
    public V1Region Region { get; set; } = new();
}