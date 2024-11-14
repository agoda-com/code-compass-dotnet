using Agoda.CodeCompass.MSBuild;

public class Run
{
    public Tool Tool { get; set; } = new();
    public List<Result> Results { get; set; } = new List<Result>();
}