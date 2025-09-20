using SadRazorEngine.Core.Interfaces;
using SadRazorEngine.Runtime;
using System.Reflection;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace SadRazorEngine.Compilation;

internal class CompiledTemplate : ICompiledTemplate
{
    private readonly Type _templateType;
    private readonly MethodInfo _executeMethod;

    /// <summary>
    /// Optional base directory for the template that produced this compiled type.
    /// When set, CompiledTemplate will attempt to call SetTemplateBasePath on the
    /// instantiated template prior to execution so the template runtime can resolve
    /// relative paths (eg. for partials).
    /// </summary>
    internal string? TemplateBasePath { get; set; }

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

        // Attempt to set the template base path if available
        var setBasePathMethod = _templateType.GetMethod("SetTemplateBasePath", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? _templateType.GetMethod("SetTemplateBasePath", BindingFlags.Public | BindingFlags.Instance);
        if (setBasePathMethod != null && TemplateBasePath != null)
        {
            setBasePathMethod.Invoke(template, new[] { TemplateBasePath });
        }

        // Use reflection to call SetModel
        var setModelMethod = _templateType.GetMethod("SetModel", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Template does not have a SetModel method");

        // Convert anonymous types to ExpandoObject so dynamic access from another assembly works
        object? modelToPass = model;
        if (model != null && IsAnonymousType(model.GetType()))
        {
            var expando = new ExpandoObject() as IDictionary<string, object?>;
            foreach (var prop in model.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                expando[prop.Name] = prop.GetValue(model);
            }
            modelToPass = expando;
        }

        // Set the model if provided
        if (modelToPass != null)
        {
            setModelMethod.Invoke(template, new[] { modelToPass });
        }

        // Execute the template (expects a Task)
        var task = (Task)_executeMethod.Invoke(template, null)!;
        await task;

        // Retrieve the rendered content from the protected GetContent method
        var getContentMethod = _templateType.GetMethod("GetContent", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Template does not have a GetContent method");

        return (string)getContentMethod.Invoke(template, null)!;
    }

    private static bool IsAnonymousType(Type type)
    {
        if (type == null) return false;

        // Anonymous types are compiler-generated, generic, not public, and their name contains "AnonymousType"
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
            && type.IsGenericType
            && (type.Name.Contains("AnonymousType") || type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
    }
}