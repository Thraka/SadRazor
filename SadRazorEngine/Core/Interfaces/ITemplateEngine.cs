using SadRazorEngine.Core.Models;

namespace SadRazorEngine.Core.Interfaces;

/// <summary>
/// Main entry point for the template engine
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Loads a template from a file path
    /// </summary>
    ITemplateContext LoadTemplate(string templatePath);

    /// <summary>
    /// Loads a template from raw template content
    /// </summary>
    ITemplateContext LoadTemplateFromContent(string templateContent);
}