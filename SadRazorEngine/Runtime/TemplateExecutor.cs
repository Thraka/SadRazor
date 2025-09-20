using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;
using System.Text.RegularExpressions;

namespace SadRazorEngine.Runtime;

public class TemplateExecutor
{
    private readonly ICompiledTemplate _template;
    private readonly string? _layoutPath;
    private object? _model;

    public TemplateExecutor(ICompiledTemplate template, string? layoutPath = null)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
        _layoutPath = layoutPath;
    }

    public TemplateExecutor WithModel<T>(T model)
    {
        _model = model;
        return this;
    }

    public async Task<TemplateResult> ExecuteAsync()
    {
        // If no layout is specified, just render the template directly
        if (string.IsNullOrEmpty(_layoutPath))
        {
            var result = await _template.RenderAsync(_model);

            // Remove the single leading space we added to lines starting with '#' during
            // sanitization so markdown headers are not indented.
            // This targets exactly one space immediately before a '#' at the start of a line.
            var normalized = Regex.Replace(result, "(?m)^[ ](?=#)", "");

            return new TemplateResult(normalized);
        }

        // Render the child template
        var childContent = await _template.RenderAsync(_model);

        // Load the layout file
        if (!File.Exists(_layoutPath))
            throw new FileNotFoundException("Layout file not found", _layoutPath);

        var layoutContent = File.ReadAllText(_layoutPath);

        // Replace common placeholders with the child content. Support:
        // - {{RenderBody}}
        // - {{Body}}
        // - @RenderBody()
        layoutContent = Regex.Replace(layoutContent, @"\{\{\s*RenderBody\s*\}\}", childContent);
        layoutContent = Regex.Replace(layoutContent, @"\{\{\s*Body\s*\}\}", childContent);
        layoutContent = layoutContent.Replace("@RenderBody()", childContent);

        // Normalize any lines in the composed layout that begin with the space + '#'
        layoutContent = Regex.Replace(layoutContent, "(?m)^[ ](?=#)", "");

        return new TemplateResult(layoutContent);
    }
}