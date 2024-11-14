# Agoda.CodeCompass 🧭

Because technical debt is like your laundry - it piles up when you're not looking.

## What is CodeCompass?

CodeCompass is a .NET analyzer that helps you navigate the treacherous waters of technical debt. It analyzes your code and produces standardized SARIF reports that quantify technical debt in terms of estimated remediation time, categorization, and priority.

Think of it as your code's financial advisor, but instead of telling you to stop buying avocado toast, it tells you to stop ignoring those nullable reference warnings.

## Installation

```bash
dotnet add package Agoda.CodeCompass
```

That's it! No complicated setup, no configuration files, no sacrificial offerings required.

## Features

- 📊 Generates standardized SARIF reports
- ⏱️ Estimates remediation time for issues
- 🎯 Categorizes and prioritizes technical debt
- 🤝 Integrates seamlessly with existing .NET projects
- 🎨 Pretty colors in the SARIF viewer (okay, mostly different shades of blue)

## Example

Here's what CodeCompass will catch for you:

```csharp
public class UserService
{
    // This will trigger AGD001: Unused parameter
    public void UpdateUser(int userId, string name, string unusedParam)
    {
        Console.WriteLine($"Updating user {userId} to name {name}");
        // unusedParam is sitting here like that gym membership you never use
    }
}
```

The analyzer will generate a SARIF report that looks something like this:

```json
{
  "runs": [{
    "results": [{
      "ruleId": "AGD001",
      "message": {
        "text": "Parameter 'unusedParam' is unused"
      },
      "properties": {
        "techDebt": {
          "minutes": 15,
          "category": "CodeCleanup",
          "priority": "Medium",
          "rationale": "Unused parameters increase code complexity and maintenance burden",
          "recommendation": "Remove the unused parameter or add a comment explaining why it's needed"
        }
      }
    }]
  }]
}
```

## Adding it to custom Rules

When calling `Diagnostic.Create` Method pass the additional paramter proeprties and populate the dictionary with the additional meta data.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnusedParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AGD001";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Unused parameter",
        messageFormat: "Parameter '{0}' is unused",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters that are not used in method bodies should be removed.",
        helpLinkUri: "https://agoda.github.io/code-compass/rules/AGD001"
    );

    private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        //...
        if (!references.Any())
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                parameter.GetLocation(),
                properties: new Dictionary<string, string?>
                {
                    ["techDebtMinutes"] = "15",
                    ["techDebtCategory"] = "CodeCleanup",
                    ["techDebtPriority"] = "Medium",
                    ["techDebtRationale"] = "Unused parameters increase code complexity and maintenance burden",
                    ["techDebtRecommendation"] = "Remove the unused parameter or add a comment explaining why it's needed"
                }.ToImmutableDictionary(),
                parameter.Identifier.Text);
                
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

## How It Works

1. Add the package to your project
2. Build your project
3. Look at the SARIF report
4. Feel slightly guilty about that code you wrote last Friday at 4:59 PM
5. Fix the issues
6. Repeat (because let's be honest, we all write Friday code sometimes)

## Analyzing the Results

The SARIF report can be viewed using:
- Visual Studio's built-in SARIF viewer
- VS Code with the SARIF Viewer extension
- Any text editor (if you really enjoy reading JSON)

## Contributing

We welcome contributions! Whether it's:
- 🐛 Bug fixes
- ✨ New analyzers
- 📝 Documentation improvements
- 💡 Feature suggestions
- 🤔 Philosophical debates about whether that TODO comment from 2019 should be classified as technical debt

## License

MIT License - Feel free to use it, modify it, or talk about it at developer conferences.

## Acknowledgments

Special thanks to:
- The developers who write the code we analyze
- The maintainers who deal with the technical debt we find
- Coffee ☕, without which this project wouldn't exist

## Questions?

Feel free to open an issue. We promise to read it, think about it, and maybe even fix it (no pinky promises though).

---

Made with 💙 (and a bit of technical debt) by Agoda

Remember: Technical debt is like regular debt, except your bank account doesn't hate you for it - just your future self and your teammates.