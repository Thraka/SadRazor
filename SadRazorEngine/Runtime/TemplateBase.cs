using System.Text;

namespace SadRazorEngine.Runtime;

/// <summary>
/// Base class for all generated templates
/// </summary>
public abstract class TemplateBase<TModel>
{
    private readonly StringBuilder _content = new();
    private int _currentColumn = 0;

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
            var s = content.ToString();
            _content.Append(s);
            UpdateCurrentColumn(s);
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
            UpdateCurrentColumn(content);
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
            // Reset column after a newline
            _currentColumn = 0;
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
    /// <param name="relativePath">The relative or absolute path to the partial template.</param>
    /// <param name="model">An optional model to pass to the partial template.</param>
    /// <param name="options">Optional. Partial options to control indentation behavior.</param>
    /// <returns>The rendered content of the partial template.</returns>
    public async Task<string> PartialAsync(string relativePath, object? model = null, PartialOptions? options = null)
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

        // Handle a common authoring mistake: passing a PartialOptions instance positionally
        // as the 'model' argument. If model is actually a PartialOptions and options
        // is null, treat that argument as the options value and leave model unset.
        if (model is PartialOptions posOptions && options == null)
        {
            options = posOptions;
            model = null;
        }

        var engine = new SadRazorEngine.TemplateEngine();
        var ctx = engine.LoadTemplate(resolved!);

        if (model != null)
            ctx = ctx.WithModel(model);
        else
            ctx = ctx.WithModel((TModel?)(object?)Model);

        var result = await ctx.RenderAsync();
        var content = result.Content;

        // Determine indent amount if requested
        var indentAmount = options?.IndentAmount ?? (options?.InheritColumn == true ? CurrentColumn : 0);
        if (indentAmount > 0)
        {
            bool skipFirstLine = options?.SkipFirstLineIndent == true;
            content = ApplyIndent(content, indentAmount, skipFirstLine);
        }

        return content;
    }

    /// <summary>
    /// Synchronously renders a partial template (by delegating to the engine) and returns the
    /// rendered content. Accepts <see cref="PartialOptions"/> to request explicit indentation.
    /// When <see cref="PartialOptions.InheritColumn"/> is specified this method will apply the
    /// current <see cref="CurrentColumn"/> as the indentation amount unless an explicit
    /// <see cref="PartialOptions.IndentAmount"/> is provided.
    /// </summary>
    /// <param name="relativePath">The relative or absolute path to the partial template.</param>
    /// <param name="model">An optional model to pass to the partial template.</param>
    /// <param name="options">Optional. Partial options to control indentation behavior.</param>
    /// <returns>The rendered content of the partial template.</returns>
    public string Partial(string relativePath, object? model = null, PartialOptions? options = null)
    {
        string resolved = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_templateBasePath ?? Directory.GetCurrentDirectory(), relativePath);

        // When called synchronously from TemplateBase, support indent options when provided.
        // Support positional passing of PartialOptions in the model slot (authoring convenience).
        if (model is PartialOptions posOpt && options == null)
        {
            options = posOpt;
            model = null;
        }

        // Determine the indent amount before calling the static helper
        int indentAmount = 0;
        if (options != null)
        {
            if (options.IndentAmount.HasValue)
            {
                indentAmount = options.IndentAmount.Value;
            }
            else if (options.InheritColumn)
            {
                indentAmount = CurrentColumn;
            }
        }

        // Create a new options object for the static helper that doesn't include InheritColumn
        // since the static helper can't handle it and we've already computed the indent amount
        PartialOptions? staticOptions = null;
        if (options != null && options.IndentAmount.HasValue)
        {
            staticOptions = new PartialOptions { IndentAmount = options.IndentAmount };
        }

        var content = TemplateHelpers.Partial(resolved, model, staticOptions);

        // Apply indentation if needed
        if (indentAmount > 0)
        {
            bool skipFirstLine = options?.SkipFirstLineIndent == true;
            content = ApplyIndent(content, indentAmount, skipFirstLine);
        }

        return content;
    }

    /// <summary>
    /// The conservative current column (character offset since last newline) of the writer.
    /// Template code may use this value for "inherit column" indentation semantics.
    /// </summary>
    protected int CurrentColumn => _currentColumn;

    private void UpdateCurrentColumn(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Use last newline position to determine trailing column length. Works for both LF and CRLF.
        var lastNewline = text.LastIndexOf('\n');
        if (lastNewline < 0)
        {
            _currentColumn += text.Length;
        }
        else
        {
            // Characters after the last newline
            var after = text.Length - (lastNewline + 1);
            _currentColumn = after;
        }
    }

    /// <summary>
    /// Applies indentation to the provided content using the shared helper implementation.
    /// </summary>
    private static string ApplyIndent(string content, int indentAmount, bool skipFirstLine = false)
    {
        return IndentationHelper.ApplyIndent(content, indentAmount, skipFirstLine);
    }
}