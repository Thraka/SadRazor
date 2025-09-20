# SadRazorEngine Development Plan

## Overview
SadRazorEngine is a lightweight, Razor-based Markdown templating system that lets developers compose Markdown documents using C# and strongly-typed models. The engine focuses on: fast authoring, faithful Markdown output, and a pleasant developer experience in editors (especially VS Code). Key runtime features already implemented include:

- Razor-based templates with optional @model declarations and runtime model binding
- Layout support: child templates can declare a @layout and the engine composes child output into the layout at render time
- Includes and partials: load-time `@include` preprocessing plus runtime `Partial`/`PartialAsync` helpers for reusable fragments
- Authoring ergonomics: an `Authoring` project (Microsoft.NET.Sdk.Web) is included to enable Razor tooling and editor intellisense for template authoring in VS Code
- Helper exposure: templates get a small set of built-in helpers (via `TemplateHelpers` static helpers and instance wrappers on `TemplateBase`) so authors can call `Partial(...)` and other helpers naturally
- Model parity: the loader/compiler attempts to detect `@model` types in templates and compile templates with the concrete model type when possible (reduces design-time vs runtime differences)
- Safety & sanitization: a targeted post-render normalization removes injected whitespace that was previously added to avoid C# preprocessor collisions, ensuring Markdown output like `#` headers are not accidentally indented

## Core Components

### Project layout (repo structure)
```
SadRazor.slnx                         # Solution file for the projects

SadRazorEngine/                       # Core engine project
├── SadRazorEngine.csproj
├── SadRazorEngine.csproj.user
├── TemplateEngine.cs                 # High-level API: load/compile/render
├── TemplateEngine.RuntimeHelpers.cs  # (auxiliary helpers)
├── Runtime/
│   ├── TemplateBase.cs               # Base template class used by generated templates
│   ├── TemplateHelpers.cs            # Small static helpers (Partial, PartialAsync, path resolution)
│   ├── TemplateExecutor.cs           # Orchestrates execution, layout composition and normalization
│   └── TemplateRuntime/              # runtime helpers used by compiled templates
├── Compilation/
│   ├── RazorTemplateCompiler.cs      # RazorProjectEngine + generation glue
│   ├── CompiledTemplate.cs           # Reflection wrapper around compiled template types
│   └── generated_template.cs         # example generated code (build-time artifact)
├── Core/
│   ├── Interfaces/
│   │   ├── ITemplateEngine.cs
│   │   ├── ITemplateCompiler.cs
│   │   └── ITemplateContext.cs
│   └── Models/
│       ├── TemplateContext.cs
│       └── TemplateResult.cs
├── obj/
└── bin/

Testbed/                              # Top-level sample/demo app used during development and authoring
├── Testbed.csproj
├── Program.cs                         # Small runner used to invoke templates, capture preview output, and exercise the engine
├── Models/                             # Example object model types consumed by sample templates
│   ├── BlogPost.cs                     # Simple blog post model used in many samples
│   └── Zoo.cs                          # More complex sample model (enclosures, animals, caretakers) used for richer demos
├── Views/                              # (optional) sample template files used by the runner and authoring preview
└── README.md                           # Notes on how to run the sample and where sample templates live

Authoring/                            # Editor/authoring project (Web SDK) to enable VS Code Razor tooling
├── Authoring.csproj
├── Views/
│   └── Samples/
│       ├── Example.cshtml            # Example template used by the authoring runner
│       ├── _staff.cshtml
│       └── _ViewImports.cshtml       # static usings for helpers to improve authoring ergonomics
└── .vscode/                           # optional editor task/launch configs for previewing templates

Tests/
└── SadRazorEngine.Tests/
    ├── SadRazorEngine.Tests.csproj
    ├── TemplateEngineTests.cs
    └── AdditionalTemplateTests.cs

```

### Responsibilities (concise)
- TemplateEngine: load templates from disk, preprocess `@include` directives (with recursion guard), detect `@model` declarations and - when possible - resolve concrete model Types and compile templates with that type; detect and strip `@layout` directives and tee layout paths into the compiled template context. It also prepares template content (e.g. adding runtime imports) so templates authored with shorthand helpers work at runtime.
- RazorTemplateCompiler: configure the RazorProjectEngine and code generation pipeline, populate default imports (including any required static imports for runtime helpers) and generate the C# source fed to Roslyn for compilation.
- CompiledTemplate: reflective wrapper around compiled template types; instantiates the generated template class, sets the runtime model via reflection, and injects a `TemplateBasePath` on the instance so runtime partial lookups resolve relative to the originating template file.
- TemplateBase: runtime base class for generated templates; exposes the model to generated code, provides protected helpers such as `Partial`/`PartialAsync` (which resolve relative paths and invoke the engine to render partials), and exposes a small API surface (`GetContent`, `SetTemplateBasePath`) useful for helpers and testing.
- TemplateExecutor: orchestrates execution of compiled templates (instantiation, model wiring, ExecuteAsync), composes child output into layouts when a `@layout` was declared (supports common placeholder tokens such as `{{RenderBody}}`), and applies targeted post-render normalizations (for example, reversing the single-space prefix that was injected to avoid C# preprocessor collisions so Markdown headers like `#` are not indented).
- TemplateHelpers: small static utility methods for resolving file paths and providing a synchronous partial rendering helper used by the Authoring project and other callers (convenience plumbing for demos and quick authoring scenarios).
- Authoring project: a separate Web-SDK project included to enable Razor tooling in editors (not part of runtime), containing sample templates, `_ViewImports` and example static usings so authors get intellisense and a comfortable authoring experience in VS Code.

## Implementation Progress

### Phase 1: Core Infrastructure [NEAR COMPLETE]
- [x] Project setup with correct SDK
- [x] Core interfaces defined
- [x] Basic models structure
- [x] NuGet dependencies added
- [x] Razor compilation pipeline
- [x] Basic template loading and parsing
- [x] Simple model binding
- [x] File output handling
- [ ] Add error handling and validation

### Phase 2: Features [NEAR COMPLETE]
- [x] Layout templates support
- [x] Partial templates
- [x] Include directives
- [x] Custom Razor helpers for Markdown

### Phase 3: Developer Experience [NOT STARTED]
- Fluent API design
- Comprehensive error handling
- Debugging support
- Documentation

## Dependencies
Required NuGet packages:
- [x] Microsoft.AspNetCore.Razor.Language
- [x] Microsoft.CodeAnalysis.CSharp

## Planned API Usage

```csharp
// Simple usage
var engine = new TemplateEngine();
var result = await engine
    .LoadTemplate("template.md")
    .WithModel(model)
    .RenderAsync();

// Advanced usage with configuration
var engine = new TemplateEngine(options => {
    options.UseLayout("_layout.md");
    options.UseCache(true);
    options.SetOutputPath("output");
});

// Template syntax example
@model BlogPost

# @Model.Title

Written by @Model.Author on @Model.Date.ToShortDateString()

@Model.Content

@foreach(var tag in Model.Tags) {
    #@tag
}
```

## Testing Strategy

### Test Project Structure (in Testbed)
1. Initial Functionality Test [x]
   - Basic template compilation
   - Simple model binding
   - Markdown with Razor syntax
   - End-to-end pipeline test

2. Unit Tests [PLANNED]
   - Template compilation
   - Model binding
   - Markdown processing
   - Helper functions

3. Integration Tests [PLANNED]
   - Full pipeline execution
   - Layout processing
   - Complex model scenarios

4. Performance Tests [PLANNED]
   - Compilation benchmarks
   - Rendering performance
   - Memory usage

## Success Criteria
- Clean, intuitive API
- Efficient template compilation
- Thread-safe execution
- Proper error handling and debugging
- Comprehensive documentation
- Example templates and usage guides

## Next Steps
1. Test the complete Phase 1 implementation
2. Add error handling and validation
3. Begin Phase 2 features (layouts, partials)
4. Add comprehensive unit tests