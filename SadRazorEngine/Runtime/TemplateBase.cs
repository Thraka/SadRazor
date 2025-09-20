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
}