using Agoda.CodeCompass.MSBuild.Sarif;

public class Run
{
    public Tool Tool { get; set; } = new();
    public List<Result> Results { get; set; } = new List<Result>();
}