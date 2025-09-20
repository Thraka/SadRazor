using System.Text;

namespace SadRazorEngine.Runtime;

/// <summary>
/// Base class for all generated templates
/// </summary>
public abstract class TemplateBase<TModel>
{
    private readonly StringBuilder _content = new();

    /// <summary>
    /// The model passed to the template
    /// </summary>
    protected TModel? Model { get; private set; }

    internal void SetModel(object? model)
    {
        Model = model is TModel typedModel ? typedModel : default;
    }

    /// <summary>
    /// Writes content to the template output
    /// </summary>
    protected void Write(object? content)
    {
        if (content != null)
        {
            _content.Append(content);
        }
    }

    /// <summary>
    /// Writes literal content to the template output
    /// </summary>
    protected void WriteLiteral(string? content)
    {
        if (content != null)
        {
            _content.Append(content);
        }
    }

    /// <summary>
    /// Writes a line of content to the template output
    /// </summary>
    protected void WriteLine(object? content)
    {
        if (content != null)
        {
            _content.AppendLine(content.ToString());
        }
    }

    /// <summary>
    /// Executes the template
    /// </summary>
    public abstract Task ExecuteAsync();

    /// <summary>
    /// Gets the current content
    /// </summary>
    protected string GetContent() => _content.ToString();

    /// <summary>
    /// Optional base path (set by executor) to resolve relative partial paths
    /// </summary>
    protected string? _templateBasePath;

    internal void SetTemplateBasePath(string? path)
    {
        _templateBasePath = path;
    }

    /// <summary>
    /// Renders a partial template at runtime. The partial path may be absolute or
    /// relative to the current template's base path; when not available the current
    /// working directory is used.
    /// </summary>
    public async Task<string> PartialAsync(string relativePath, object? model = null)
    {
        string resolved;
        if (Path.IsPathRooted(relativePath))
        {
            resolved = relativePath;
        }
        else if (!string.IsNullOrEmpty(_templateBasePath))
        {
            resolved = Path.Combine(_templateBasePath, relativePath);
        }
        else
        {
            resolved = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        }

        var engine = new SadRazorEngine.TemplateEngine();
        var ctx = engine.LoadTemplate(resolved!);

        if (model != null)
            ctx = ctx.WithModel(model);
        else
            ctx = ctx.WithModel((TModel?)(object?)Model);

        var result = await ctx.RenderAsync();
        return result.Content;
    }

    /// <summary>
    /// Synchronous convenience wrapper that resolves a partial path relative to the current
    /// template base path and renders it.
    /// </summary>
    public string Partial(string relativePath, object? model = null)
    {
        string resolved = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_templateBasePath ?? Directory.GetCurrentDirectory(), relativePath);

        return TemplateHelpers.Partial(resolved, model);
    }
}