using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Services;

namespace SadRazorEngine.Extensions;

/// <summary>
/// Extension methods for loading models from various data formats
/// </summary>
public static class TemplateContextExtensions
{
    /// <summary>
    /// Load model from a JSON, YAML, or XML file (auto-detecting format)
    /// </summary>
    public static async Task<ITemplateContext> WithModelFromFileAsync(this ITemplateContext context, string filePath)
    {
        var model = await ModelLoader.LoadFromFileAsync(filePath);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load strongly-typed model from a JSON, YAML, or XML file (auto-detecting format)
    /// </summary>
    public static async Task<ITemplateContext> WithModelFromFileAsync<T>(this ITemplateContext context, string filePath) where T : class
    {
        var model = await ModelLoader.LoadFromFileAsync<T>(filePath);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load model from JSON string
    /// </summary>
    public static ITemplateContext WithModelFromJson(this ITemplateContext context, string jsonContent)
    {
        var model = ModelLoader.LoadFromJson(jsonContent);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load strongly-typed model from JSON string
    /// </summary>
    public static ITemplateContext WithModelFromJson<T>(this ITemplateContext context, string jsonContent) where T : class
    {
        var model = ModelLoader.LoadFromJson<T>(jsonContent);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load model from YAML string
    /// </summary>
    public static ITemplateContext WithModelFromYaml(this ITemplateContext context, string yamlContent)
    {
        var model = ModelLoader.LoadFromYaml(yamlContent);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load strongly-typed model from YAML string
    /// </summary>
    public static ITemplateContext WithModelFromYaml<T>(this ITemplateContext context, string yamlContent) where T : class
    {
        var model = ModelLoader.LoadFromYaml<T>(yamlContent);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load model from XML string
    /// </summary>
    public static ITemplateContext WithModelFromXml(this ITemplateContext context, string xmlContent)
    {
        var model = ModelLoader.LoadFromXml(xmlContent);
        return context.WithModel(model);
    }

    /// <summary>
    /// Load strongly-typed model from XML string
    /// </summary>
    public static ITemplateContext WithModelFromXml<T>(this ITemplateContext context, string xmlContent) where T : class
    {
        var model = ModelLoader.LoadFromXml<T>(xmlContent);
        return context.WithModel(model);
    }
}