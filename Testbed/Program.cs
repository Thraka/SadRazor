using SadRazorEngine;
using Testbed.Models;

// Create test template file
var templatePath = "test-template.md";
var templateContent = """
 # @Model.Title

Written by @Model.Author on @Model.Date.ToShortDateString()

@Model.Content

Tags:
@foreach(var tag in Model.Tags) {
    @: #@tag 
}
""";

// Write template to file
File.WriteAllText(templatePath, templateContent);

try
{
    // Initialize the template engine
    var engine = new TemplateEngine();
    var model = new BlogPost();

    // Process the template
    var result = await engine
        .LoadTemplate(templatePath)
        .WithModel(model)
        .RenderAsync();

    // Output results
    Console.WriteLine("Template rendered successfully!");
    Console.WriteLine("\nOutput:\n");
    Console.WriteLine(result.Content);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex}");
}
finally
{
    // Cleanup
    if (File.Exists(templatePath))
    {
        File.Delete(templatePath);
    }
}
