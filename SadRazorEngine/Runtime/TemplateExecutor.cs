using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Core.Models;

namespace SadRazorEngine.Runtime;

public class TemplateExecutor
{
    private readonly ICompiledTemplate _template;
    private object? _model;

    public TemplateExecutor(ICompiledTemplate template)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
    }

    public TemplateExecutor WithModel<T>(T model)
    {
        _model = model;
        return this;
    }

    public async Task<TemplateResult> ExecuteAsync()
    {
        var result = await _template.RenderAsync(_model);
        return new TemplateResult(result);
    }
}