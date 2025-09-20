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
    // Toggle to enable debug output files for template generation (disabled by default)
    public static bool EnableDebugWrites { get; set; } = false;
    
    /// <summary>
    /// Directory where debug output files are written when <see cref="EnableDebugWrites"/> is true.
    /// Defaults to .debug but can be overridden by consumers.
    /// </summary>
    public static string DebugOutputDirectory { get; set; } = ".debug";

    private static readonly RazorProjectEngine _projectEngine;
    private static readonly CSharpCompilation _baseCompilation;

    static RazorTemplateCompiler()
    {
        // Initialize the Razor engine with default configuration
        _projectEngine = RazorProjectEngine.Create(
            RazorConfiguration.Create(RazorLanguageVersion.Latest, "Default", Array.Empty<RazorExtension>()),
            RazorProjectFileSystem.Create("."),
            builder =>
        {
            // Configure for our markdown templates
            builder.SetNamespace("SadRazorEngine.Templates");
            
            // Configure defaults
            builder.ConfigureClass((document, @class) =>
            {
                @class.ClassName = $"Template_{Guid.NewGuid():N}";
                // Default to dynamic-based base type; if a concrete model type is
                // provided at compile time, the generated C# will be patched below
                // to reference the concrete model type instead of dynamic.
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
@using static SadRazorEngine.Runtime.TemplateHelpers
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
        if (EnableDebugWrites)
        {
            try
            {
                var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sadrazor_sanitized_template.txt");
                System.IO.File.WriteAllText(tempPath, sanitizedTemplate);
            }
            catch { /* ignore logging errors */ }
        }

        // Write sanitized template to repository root for easy inspection
        if (EnableDebugWrites)
        {
            try
            {
                var repoPath = System.IO.Path.Combine("c:", "Code", "Fun", "SadRazor", "sanitized_template.txt");
                System.IO.File.WriteAllText(repoPath, sanitizedTemplate);
            }
            catch { /* ignore logging errors */ }
        }

        // Create a Razor code document from the template
        // If a model type was provided, strip any `@model` directive so the Razor
        // parser doesn't treat it as text/expression; we'll patch the generated
        // base type to the concrete model type below.
        var processedTemplate = sanitizedTemplate;
        if (modelType != null)
        {
            processedTemplate = System.Text.RegularExpressions.Regex.Replace(processedTemplate, @"(?m)^\s*@model\b.*(?:\r?\n)?", "");
        }

        var codeDocument = _projectEngine.Process(
            RazorSourceDocument.Create(processedTemplate, "Template.cshtml"),
            null,
            new List<RazorSourceDocument>(),
            new List<TagHelperDescriptor>());

        // Get the generated C# code
        var csharpDoc = codeDocument.GetCSharpDocument();
        var generatedCode = csharpDoc.GeneratedCode;

        // Debug: write generated C# for inspection when tests run
        if (EnableDebugWrites)
        {
            try
            {
                var debugName = modelType != null ? $"sadrazor_generated_pre_{modelType.Name}.cs" : $"sadrazor_generated_pre_{Guid.NewGuid():N}.cs";
                var debugPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), debugName);
                System.IO.File.WriteAllText(debugPath, generatedCode);
            }
            catch { /* best-effort debug write */ }
        }

        // Also persist generated C# to the repository so the workspace can inspect it easily during debugging.
        var debugDir = DebugOutputDirectory;
        if (EnableDebugWrites)
        {
            try
            {
                System.IO.Directory.CreateDirectory(debugDir);
                var repoName = modelType != null ? $"generated_template_pre_{modelType.Name}.cs" : "generated_template_pre_untyped.cs";
                var repoPath = System.IO.Path.Combine(debugDir, repoName);
                System.IO.File.WriteAllText(repoPath, generatedCode);
            }
            catch { /* best-effort debug write */ }
        }

        // If a model type is provided, swap any placeholder generic parameter so the
        // generated C# refers to the concrete model type (e.g. TemplateBase<MyNs.MyModel>). 
        // This is done after Razor code generation because ConfigureClass uses a
        // placeholder BaseType that must be resolved here per-compilation.
        if (modelType != null)
        {
            // Replace the dynamic-based base type with the concrete model type
            // so the generated code compiles against the provided model.
            generatedCode = generatedCode.Replace("SadRazorEngine.Runtime.TemplateBase<dynamic>", $"SadRazorEngine.Runtime.TemplateBase<{modelType.FullName}>")
                                         .Replace("TemplateBase<dynamic>", $"TemplateBase<{modelType.FullName}>");
        }

        // Persist the post-processed generated C# for inspection
        if (EnableDebugWrites)
        {
            try
            {
                var repoName2 = modelType != null ? $"generated_template_after_{modelType.Name}.cs" : "generated_template_after_untyped.cs";
                var repoPath2 = System.IO.Path.Combine(debugDir, repoName2);
                System.IO.File.WriteAllText(repoPath2, generatedCode);
            }
            catch { }
        }

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