using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SadRazorEngine.Core.Interfaces;
using System.Reflection;
using System.Text;

namespace SadRazorEngine.Compilation;

public class RazorTemplateCompiler : ITemplateCompiler
{
    private static readonly RazorProjectEngine _projectEngine;
    private static readonly CSharpCompilation _baseCompilation;

    static RazorTemplateCompiler()
    {
        // Initialize the Razor engine with default configuration
        _projectEngine = RazorProjectEngine.Create(
            RazorConfiguration.Create(RazorLanguageVersion.Latest, "Template", Array.Empty<RazorExtension>()), 
            RazorProjectFileSystem.Create("."), 
            builder =>
        {
            // Configure for our markdown templates
            builder.SetNamespace("SadRazorEngine.Templates");
            
            // Configure defaults
            builder.ConfigureClass((document, @class) =>
            {
                @class.ClassName = $"Template_{Guid.NewGuid():N}";
                @class.BaseType = "SadRazorEngine.Runtime.TemplateBase<dynamic>";
                @class.Modifiers.Clear();
                @class.Modifiers.Add("public");
            });

            // Add our template imports
            builder.AddDefaultImports(@"
@using System
@using System.Threading.Tasks
@using System.Collections.Generic
@using System.Linq
");
        });

        // Setup base compilation with required references
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(RazorTemplateCompiler).Assembly,
            Assembly.Load("netstandard"),
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            Assembly.Load("System.Linq"),
            Assembly.Load("System.Linq.Expressions"),
            Assembly.Load("System.Dynamic.Runtime"),
            typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly,
            // Add reference to the executing assembly which contains the model types
            Assembly.GetEntryAssembly()!
        };

        _baseCompilation = CSharpCompilation.Create(
            "SadRazorTemplate",
            references: assemblies.Where(a => a != null).Select(x => MetadataReference.CreateFromFile(x.Location)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }

    public async Task<ICompiledTemplate> CompileAsync(string template, Type? modelType = null)
    {
        // Sanitize template: prefix lines that start with '#' with a space to avoid generating C# preprocessor directives
        var sanitizedTemplate = System.Text.RegularExpressions.Regex.Replace(template, "(?m)^(#)", " $1");

        // Write sanitized template to temp file to aid debugging
        try
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sadrazor_sanitized_template.txt");
            System.IO.File.WriteAllText(tempPath, sanitizedTemplate);
        }
        catch { /* ignore logging errors */ }

        // Write sanitized template to repository root for easy inspection
        try
        {
            var repoPath = System.IO.Path.Combine("c:", "Code", "Fun", "SadRazor", "sanitized_template.txt");
            System.IO.File.WriteAllText(repoPath, sanitizedTemplate);
        }
        catch { /* ignore logging errors */ }

        // Create a Razor code document from the template
        var codeDocument = _projectEngine.Process(
            RazorSourceDocument.Create(sanitizedTemplate, "Template.cshtml"),
            null,
            new List<RazorSourceDocument>(),
            new List<TagHelperDescriptor>());

        // Get the generated C# code
        var csharpDoc = codeDocument.GetCSharpDocument();
        var generatedCode = csharpDoc.GeneratedCode;

        if (csharpDoc.Diagnostics.Any())
        {
            var details = string.Join(Environment.NewLine, csharpDoc.Diagnostics.Select(d => $"{d.Id}: {d.GetMessage()} (Line {d.Span.LineIndex}, Char {d.Span.CharacterIndex})"));
            throw new InvalidOperationException(
                $"Template compilation failed:\n{details}\n--- Generated C# code ---\n{generatedCode}");
        }

        // Create compilation
        var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
        var compilation = _baseCompilation.AddSyntaxTrees(syntaxTree);

        // Add additional references if model type is provided
        if (modelType != null)
        {
            compilation = compilation.AddReferences(
                MetadataReference.CreateFromFile(modelType.Assembly.Location));
        }

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var failures = result.Diagnostics
                .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

            throw new InvalidOperationException(
                $"Template compilation failed: {string.Join(Environment.NewLine, failures)}");
        }

        // Load the compiled assembly
        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        
        // Find and create an instance of our template class
        var templateType = assembly.GetTypes()
            .First(t => t.BaseType?.Name.StartsWith("TemplateBase") == true);

        return new CompiledTemplate(templateType);
    }
}