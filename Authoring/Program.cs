using System;
using System.IO;
using System.Threading.Tasks;
using SadRazorEngine;

// Simple authoring runner — renders a template file (path or default) and prints output.
// Usage: dotnet run --project Authoring/Authoring.csproj -- <path-to-template>

var templatePath = args.Length > 0 ? args[0] : Path.Combine("Views", "Samples", "Example.cshtml");
if (!File.Exists(templatePath))
{
    Console.Error.WriteLine($"Template not found: {templatePath}");
    return;
}

var engine = new TemplateEngine();
var result = await engine.LoadTemplate(templatePath)
                         .WithModel(Testbed.Zoo.CreateSample())
                         .RenderAsync();

Console.WriteLine(result.Content);
