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

    /// <summary>
    /// Validates the template against the current model by attempting to render it
    /// and capturing any errors that occur during rendering
    /// </summary>
    public async Task<ValidationResult> ValidateAsync()
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        try
        {
            // Attempt to render the template to catch runtime errors
            await _template.RenderAsync(_model);
        }
        catch (Exception ex)
        {
            // Parse different types of exceptions into validation errors
            var error = ex switch
            {
                NullReferenceException => new ValidationError
                {
                    Message = "Null reference exception - likely accessing a property that doesn't exist in the model",
                    Type = ValidationErrorType.ModelAccess,
                    Code = ex.Message
                },
                ArgumentException => new ValidationError
                {
                    Message = $"Argument exception: {ex.Message}",
                    Type = ValidationErrorType.TypeMismatch,
                    Code = ex.Message
                },
                FileNotFoundException => new ValidationError
                {
                    Message = $"Partial template not found: {ex.Message}",
                    Type = ValidationErrorType.PartialNotFound,
                    Code = ex.Message
                },
                _ => new ValidationError
                {
                    Message = $"Template rendering error: {ex.Message}",
                    Type = ValidationErrorType.Syntax,
                    Code = ex.Message
                }
            };

            errors.Add(error);
        }

        // Additional validation could be added here:
        // - Check for common template patterns that might cause issues
        // - Validate model property access patterns
        // - Check for required model properties based on template usage

        return ValidationResult.Create(errors, warnings);
    }
}