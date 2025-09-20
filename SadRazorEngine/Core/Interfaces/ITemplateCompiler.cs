namespace SadRazorEngine.Core.Interfaces;

/// <summary>
/// Handles the compilation of Razor templates to executable code
/// </summary>
public interface ITemplateCompiler
{
    /// <summary>
    /// Compiles a template into executable code
    /// </summary>
    /// <param name="template">The template content</param>
    /// <param name="modelType">The type of the model (can be null)</param>
    /// <returns>A compiled template that can be executed</returns>
    Task<ICompiledTemplate> CompileAsync(string template, Type? modelType = null);
}

/// <summary>
/// Represents a compiled template that can be executed
/// </summary>
public interface ICompiledTemplate
{
    /// <summary>
    /// Executes the template with the given model
    /// </summary>
    Task<string> RenderAsync(object? model = null);
}