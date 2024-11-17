namespace Agoda.CodeCompass.MSBuild.Sarif;

public class SarifReport
{
    public string Schema { get; set; } = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json";
    public string Version { get; set; } = "2.1.0";
    public Run[] Runs { get; set; } = Array.Empty<Run>();
}