using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Runtime;
using System.Reflection;

namespace SadRazorEngine.Compilation;

internal class CompiledTemplate : ICompiledTemplate
{
    private readonly Type _templateType;
    private readonly MethodInfo _executeMethod;

    public CompiledTemplate(Type templateType)
    {
        _templateType = templateType;
        _executeMethod = templateType.GetMethod("ExecuteAsync") 
            ?? throw new InvalidOperationException("Template does not have an ExecuteAsync method");
    }

    public async Task<string> RenderAsync(object? model = null)
    {
        // Create an instance of the template
        var template = Activator.CreateInstance(_templateType)!;

        // Use reflection to call SetModel
        var setModelMethod = _templateType.GetMethod("SetModel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Template does not have a SetModel method");

        // Set the model if provided
        if (model != null)
        {
            setModelMethod.Invoke(template, new[] { model });
        }

        // Execute the template (expects a Task)
        var task = (Task)_executeMethod.Invoke(template, null)!;
        await task;

        // Retrieve the rendered content from the protected GetContent method
        var getContentMethod = _templateType.GetMethod("GetContent", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Template does not have a GetContent method");

        return (string)getContentMethod.Invoke(template, null)!;
    }
}