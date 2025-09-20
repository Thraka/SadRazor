using SadRazorEngine.Core.Models;

namespace SadRazorEngine.Core.Interfaces;

public interface ITemplateContext
{
    ITemplateContext WithModel<T>(T model);
    Task<TemplateResult> RenderAsync();
    Task<ValidationResult> ValidateAsync();
}