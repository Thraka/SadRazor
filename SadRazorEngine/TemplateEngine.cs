using SadRazorEngine.Compilation;
using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

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
        var basePath = Path.GetDirectoryName(templatePath);

        // Delegate to the content-based overload with base path for resolving includes
        return LoadTemplateFromContent(templateContent, basePath);
    }

    /// <summary>
    /// Loads a template from raw template content and returns a context.
    /// This public overload is part of the interface.
    /// </summary>
    public Core.Interfaces.ITemplateContext LoadTemplateFromContent(string templateContent)
    {
        return LoadTemplateFromContent(templateContent, null);
    }

    /// <summary>
    /// Internal overload that accepts a base path used to resolve relative include paths.
    /// </summary>
    public Core.Interfaces.ITemplateContext LoadTemplateFromContent(string templateContent, string? basePath = null)
    {
        if (string.IsNullOrEmpty(templateContent))
            throw new ArgumentNullException(nameof(templateContent));

        // @include feature removed — compile the template content as provided
        var processed = templateContent;

        // Detect a @model directive so we can compile the template with a concrete model type
        Type? modelType = null;
        var modelMatch = Regex.Match(processed, @"(?m)^\s*@model\s+(?<type>[^
]+)");
        if (modelMatch.Success)
        {
            var typeName = modelMatch.Groups["type"].Value.Trim();

            // Try resolve the type by scanning loaded assemblies
            modelType = Type.GetType(typeName, false)
                        ?? AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType(typeName, false))
                            .FirstOrDefault(t => t != null);

            if (modelType == null)
            {
                // Try simple (unqualified) name match as a last resort
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    modelType = asm.GetTypes().FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
                    if (modelType != null) break;
                }
            }
        }

        // Compile the template, passing the model type when resolved so the generated
        // C# code references the concrete model type instead of dynamic.
        var compiledTemplate = _compiler.CompileAsync(processed, modelType).Result;

        // If the resulting compiled template is our internal CompiledTemplate type,
        // record the base path so runtime partials can resolve relative file paths.
        if (compiledTemplate is SadRazorEngine.Compilation.CompiledTemplate ct)
        {
            ct.TemplateBasePath = basePath;
        }

        // Create and return the context
        return new TemplateContext(compiledTemplate);
    }
}