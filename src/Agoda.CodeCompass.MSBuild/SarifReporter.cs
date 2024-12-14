using Agoda.CodeCompass.Data;
using Agoda.CodeCompass.MSBuild.Sarif;
using Agoda.CodeCompass.MSBuild;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using Formatting = System.Xml.Formatting;

public class SarifReporter
{
    private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Error = HandleDeserializationError,
        Formatting = Newtonsoft.Json.Formatting.Indented,
    };

    private static void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
    {
        // Log the error but don't throw it
        var currentError = errorArgs.ErrorContext.Error.Message;
        Console.WriteLine($"Warning during SARIF processing: {currentError}");
        errorArgs.ErrorContext.Handled = true;
    }

    public static string AddTechDebtToSarif(string sarifContent)
    {
        // Detect version
        var jObject = JObject.Parse(sarifContent);
        var version = jObject["version"]?.ToString();

        return version switch
        {
            "1.0.0" => AddTechDebtToSarifV1(sarifContent),
            "2.1.0" => AddTechDebtToSarifV2(sarifContent),
            _ => throw new NotSupportedException($"Unsupported SARIF version: {version}")
        };
    }

    private static string AddTechDebtToSarifV1(string sarifContent)
    {
        var report = JsonConvert.DeserializeObject<SarifV1Report>(sarifContent, _jsonSettings);

        // Add tech debt properties to results for V1
        foreach (var run in report.Runs)
        {
            foreach (var result in run.Results)
            {
                result.Properties = GetTechDebtProperties(result.RuleId);
            }
        }

        return JsonConvert.SerializeObject(report, _jsonSettings);
    }

    private static string AddTechDebtToSarifV2(string sarifContent)
    {
        var report = JsonConvert.DeserializeObject<SarifReport>(sarifContent, _jsonSettings);

        // Add tech debt properties to rules
        if (report.Runs?.FirstOrDefault()?.Tool?.Driver?.Rules != null)
        {
            foreach (var rule in report.Runs[0].Tool.Driver.Rules)
            {
                rule.Properties = GetTechDebtProperties(rule.Id);
            }
        }

        // Add tech debt properties to results
        foreach (var run in report.Runs)
        {
            foreach (var result in run.Results)
            {
                result.Properties = GetTechDebtProperties(result.RuleId);
            }
        }

        return JsonConvert.SerializeObject(report, _jsonSettings);
    }

    private static TechDebtProperties GetTechDebtProperties(string ruleId)
    {
        var techDebtInfo = TechDebtMetadata.GetTechDebtInfo(ruleId);
        return new TechDebtProperties
        {
            TechDebt = techDebtInfo
        };
    }
}