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
        // - {{RenderBody}} (when placed at the start of a line, inherit that line's leading whitespace)
        // - {{Body}} (same as RenderBody)
        // - @RenderBody() (inline replacement, no column inheritance)
        layoutContent = Regex.Replace(layoutContent, @"(?m)^(?<indent>[ \t]*)\{\{\s*RenderBody\s*\}\}", m =>
        {
            var indent = m.Groups["indent"].Value ?? string.Empty;
            var indented = IndentationHelper.ApplyIndent(childContent, indent.Length);
            return indented;
        });

        layoutContent = Regex.Replace(layoutContent, @"(?m)^(?<indent>[ \t]*)\{\{\s*Body\s*\}\}", m =>
        {
            var indent = m.Groups["indent"].Value ?? string.Empty;
            var indented = IndentationHelper.ApplyIndent(childContent, indent.Length);
            return indented;
        });

        // Inline placeholder (no column inheritance)
        layoutContent = layoutContent.Replace("@RenderBody()", childContent);

        // Normalize any lines in the composed layout that begin with the space + '#'
        layoutContent = Regex.Replace(layoutContent, "(?m)^[ ](?=#)", "");

        return new TemplateResult(layoutContent);
    }
}