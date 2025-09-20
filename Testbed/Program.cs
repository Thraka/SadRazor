using SadRazorEngine;
using SadRazorEngine.Extensions;
using SadRazorEngine.Core.Services;
using Testbed.Models;

Console.WriteLine("=== Phase 3 Feature Tests ===\n");

// Test 1: Model loading from JSON
Console.WriteLine("Test 1: Model loading from JSON");
await TestJsonModelLoading();

// Test 2: Template caching
Console.WriteLine("\nTest 2: Template caching");
await TestTemplateCaching();

// Test 3: Conditional partials
Console.WriteLine("\nTest 3: Conditional partials");
await TestConditionalPartials();

// Test 4: Validation
Console.WriteLine("\nTest 4: Template validation");
await TestValidation();

Console.WriteLine("\n=== All Phase 3 tests completed ===");

static async Task TestJsonModelLoading()
{
    try
    {
        var engine = new TemplateEngine();
        var jsonModel = @"{""Name"": ""John"", ""Age"": 30}";
        var templateContent = @"Hello @Model.Name, you are @Model.Age years old!";

        var context = engine.LoadTemplateFromContent(templateContent);
        context = context.WithModelFromJson(jsonModel);
        var result = await context.RenderAsync();
        
        Console.WriteLine($"✓ JSON model loading successful: {result.Content}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ JSON model loading failed: {ex.Message}");
    }
}

static async Task TestTemplateCaching()
{
    try
    {
        var engine = new TemplateEngine(enableCaching: true);
        var template = @"Cache test: @Model.Value";

        // First render
        var context1 = engine.LoadTemplateFromContent(template);
        context1 = context1.WithModel(new { Value = "First" });
        await context1.RenderAsync();

        // Second render (should use cache)
        var context2 = engine.LoadTemplateFromContent(template);
        context2 = context2.WithModel(new { Value = "Second" });
        await context2.RenderAsync();

        var stats = engine.GetCacheStatistics();
        Console.WriteLine($"✓ Template caching working: {stats?.EntryCount ?? 0} entries cached");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Template caching failed: {ex.Message}");
    }
}

static async Task TestConditionalPartials()
{
    try
    {
        // Create a test partial file
        var partialPath = Path.Combine(Directory.GetCurrentDirectory(), "_test_partial.cshtml");
        await File.WriteAllTextAsync(partialPath, "This is a conditional partial!");

        var engine = new TemplateEngine();
        var template = @"
Before partial
@(PartialIf(""_test_partial.cshtml"", true))
@(PartialIf(""_test_partial.cshtml"", false))
After partial";

        var context = engine.LoadTemplateFromContent(template);
        var result = await context.RenderAsync();

        // Clean up
        if (File.Exists(partialPath))
            File.Delete(partialPath);

        Console.WriteLine("✓ Conditional partials working");
        Console.WriteLine($"Result:\n{result.Content}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Conditional partials failed: {ex.Message}");
    }
}

static async Task TestValidation()
{
    try
    {
        var engine = new TemplateEngine();
        var invalidTemplate = @"Hello @Model.NonExistentProperty!";
        var jsonModel = @"{""Name"": ""John"", ""Age"": 30}";

        var context = engine.LoadTemplateFromContent(invalidTemplate);
        context = context.WithModelFromJson(jsonModel);
        var validation = await context.ValidateAsync();

        Console.WriteLine($"✓ Validation working - Valid: {validation.IsValid}");
        if (!validation.IsValid)
        {
            Console.WriteLine($"  Found {validation.Errors.Count} error(s):");
            foreach (var error in validation.Errors)
            {
                Console.WriteLine($"    - {error.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Validation failed: {ex.Message}");
    }
}
