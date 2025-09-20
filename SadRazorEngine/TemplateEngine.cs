using SadRazorEngine.Compilation;
using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;

namespace SadRazorEngine;

public class TemplateEngine : ITemplateEngine
{
    private readonly ITemplateCompiler _compiler;

    public TemplateEngine(ITemplateCompiler? compiler = null)
    {
        _compiler = compiler ?? new RazorTemplateCompiler();
    }

    public Core.Interfaces.ITemplateContext LoadTemplate(string templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
            throw new ArgumentNullException(nameof(templatePath));

        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template file not found", templatePath);

        // Read the template content
        var templateContent = File.ReadAllText(templatePath);
        
        // Compile the template without model type - it will be provided later
        var compiledTemplate = _compiler.CompileAsync(templateContent).Result;
        
        // Create and return the context
        return new TemplateContext(compiledTemplate);
    }
}