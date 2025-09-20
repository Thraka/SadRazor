using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Runtime;

namespace SadRazorEngine.Core.Models;

/// <summary>
/// Represents a template context for execution
/// </summary>
public class TemplateContext : ITemplateContext
{
    private readonly TemplateExecutor _executor;

    public TemplateContext(ICompiledTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _executor = new TemplateExecutor(template);
    }

    /// <summary>
    /// Sets the model to be used in the template
    /// </summary>
    public ITemplateContext WithModel<T>(T model)
    {
        _executor.WithModel(model);
        return this;
    }

    /// <summary>
    /// Renders the template asynchronously
    /// </summary>
    public Task<TemplateResult> RenderAsync()
    {
        return _executor.ExecuteAsync();
    }
}

/// <summary>
/// Represents the result of template execution
/// </summary>
public class TemplateResult
{
    /// <summary>
    /// The rendered content
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Creates a new template result
    /// </summary>
    public TemplateResult(string content)
    {
        Content = content;
    }
}