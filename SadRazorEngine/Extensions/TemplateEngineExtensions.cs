using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;

namespace SadRazorEngine.Extensions;

/// <summary>
/// Extension methods for template validation
/// </summary>
public static class TemplateEngineExtensions
{
    /// <summary>
    /// Validate a template file with a model
    /// </summary>
    public static async Task<ValidationResult> ValidateTemplateAsync(this TemplateEngine engine, string templatePath, object? model = null)
    {
        var context = engine.LoadTemplate(templatePath);
        if (model != null)
        {
            context = context.WithModel(model);
        }
        return await context.ValidateAsync();
    }

    /// <summary>
    /// Validate template content with a model
    /// </summary>
    public static async Task<ValidationResult> ValidateTemplateFromContentAsync(this TemplateEngine engine, string templateContent, object? model = null)
    {
        var context = engine.LoadTemplateFromContent(templateContent);
        if (model != null)
        {
            context = context.WithModel(model);
        }
        return await context.ValidateAsync();
    }

    /// <summary>
    /// Validate a template file with a model loaded from a data file
    /// </summary>
    public static async Task<ValidationResult> ValidateTemplateWithModelFileAsync(this TemplateEngine engine, string templatePath, string modelPath)
    {
        var context = engine.LoadTemplate(templatePath);
        context = await context.WithModelFromFileAsync(modelPath);
        return await context.ValidateAsync();
    }
}