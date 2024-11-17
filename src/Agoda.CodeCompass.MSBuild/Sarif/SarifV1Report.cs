namespace Agoda.CodeCompass.MSBuild.Sarif;

public class SarifV1Report
{
    public string Schema { get; set; } = "http://json.schemastore.org/sarif-1.0.0";
    public string Version { get; set; } = "1.0.0";
    public V1Run[] Runs { get; set; } = Array.Empty<V1Run>();
}