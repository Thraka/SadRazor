using SadRazorEngine.Compilation;
using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;
using SadRazorEngine.Core.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SadRazorEngine;

public class TemplateEngine : ITemplateEngine
{
    private readonly ITemplateCompiler _compiler;
    private readonly TemplateCache? _cache;

    public TemplateEngine(ITemplateCompiler? compiler = null, bool enableCaching = true, int maxCacheSize = 100, TimeSpan? cacheExpirationTime = null)
    {
        _compiler = compiler ?? new RazorTemplateCompiler();
        _cache = enableCaching ? new TemplateCache(maxCacheSize, cacheExpirationTime) : null;
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
        var modelMatch = Regex.Match(processed, @"(?m)^\s*@model\s+(?<type>[^\r\n]+)");
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

        // Check cache first if caching is enabled
        ICompiledTemplate? compiledTemplate = _cache?.Get(processed, modelType);
        
        if (compiledTemplate == null)
        {
            // Compile the template, passing the model type when resolved so the generated
            // C# code references the concrete model type instead of dynamic.
            compiledTemplate = _compiler.CompileAsync(processed, modelType).Result;
            
            // Store in cache if caching is enabled
            _cache?.Set(processed, modelType, compiledTemplate);
        }

        // If the resulting compiled template is our internal CompiledTemplate type,
        // record the base path so runtime partials can resolve relative file paths.
        if (compiledTemplate is SadRazorEngine.Compilation.CompiledTemplate ct)
        {
            ct.TemplateBasePath = basePath;
        }

        // Create and return the context
        return new TemplateContext(compiledTemplate);
    }

    /// <summary>
    /// Clear the template cache
    /// </summary>
    public void ClearCache()
    {
        _cache?.Clear();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics? GetCacheStatistics()
    {
        return _cache?.GetStatistics();
    }
}