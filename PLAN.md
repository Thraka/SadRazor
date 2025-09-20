# SadRazorEngine Development Plan

## Overview
SadRazorEngine is a lightweight, Razor-based Markdown templating system that lets developers compose Markdown documents using C# and strongly-typed models. The engine focuses on: fast authoring, faithful Markdown output, and a pleasant developer experience in editors (especially VS Code). Key runtime features already implemented include:

- Razor-based templates with optional @model declarations and runtime model binding
- (Layout support removed) The engine intentionally does not provide automatic @layout composition; templates should use runtime `Partial` helpers for composition when needed.
- Partials: runtime `Partial`/`PartialAsync` helpers for reusable fragments (no compile-time @include preprocessing)
- Authoring ergonomics: an `Authoring` project (Microsoft.NET.Sdk.Web) is included to enable Razor tooling and editor intellisense for template authoring in VS Code
- Authoring has `_ViewImports.cshtml` to import defaults, and these are replicated in the compiler with the `AddDefaultImports` method.
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
│   ├── TemplateExecutor.cs           # Orchestrates execution and normalization
│   ├── PartialOptions.cs             # Configuration for partial indentation behavior
│   └── IndentationHelper.cs          # Utility for applying indentation to multi-line content
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

SadRazor.Cli/
├── SadRazor.Cli.csproj
├── Program.cs (entry point)
├── Commands/
│   ├── RenderCommand.cs
│   ├── InitCommand.cs (scaffold new projects)
│   └── ValidateCommand.cs
├── Models/
│   ├── CliOptions.cs
│   └── ConfigFile.cs
└── Services/
    ├── ModelLoader.cs (JSON/YAML/XML)
    └── OutputManager.cs

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
    ├── AdditionalTemplateTests.cs
    ├── PartialTests.cs                       # Basic partial template functionality
    ├── PartialIndentationTests.cs            # Advanced indentation scenarios
    └── SkipFirstLineIndentTests.cs           # Comprehensive SkipFirstLineIndent feature tests

```

### Responsibilities (concise)
- TemplateEngine: load templates from disk, detect `@model` declarations and - when possible - resolve concrete model Types and compile templates with that type. The engine prepares template content (e.g. adding runtime imports) so templates authored with shorthand helpers work at runtime. The engine intentionally does not perform compile-time inlining of fragments; authors should use the runtime `Partial`/`PartialAsync` helpers for composing reusable fragments.
- RazorTemplateCompiler: configure the RazorProjectEngine and code generation pipeline, populate default imports (including any required static imports for runtime helpers) and generate the C# source fed to Roslyn for compilation.
- CompiledTemplate: reflective wrapper around compiled template types; instantiates the generated template class, sets the runtime model via reflection, and injects a `TemplateBasePath` on the instance so runtime partial lookups resolve relative to the originating template file.
- TemplateBase: runtime base class for generated templates; exposes the model to generated code, provides protected helpers such as `Partial`/`PartialAsync` (which resolve relative paths and invoke the engine to render partials), and exposes a small API surface (`GetContent`, `SetTemplateBasePath`) useful for helpers and testing.
- TemplateExecutor: orchestrates execution of compiled templates (instantiation, model wiring, ExecuteAsync) and applies targeted post-render normalizations (for example, reversing the single-space prefix that was injected to avoid C# preprocessor collisions so Markdown headers like `#` are not indented).
- TemplateHelpers: small static utility methods for resolving file paths and providing a synchronous partial rendering helper used by the Authoring project and other callers (convenience plumbing for demos and quick authoring scenarios).
- Authoring project: a separate Web-SDK project included to enable Razor tooling in editors (not part of runtime), containing sample templates, `_ViewImports` and example static usings so authors get intellisense and a comfortable authoring experience in VS Code.
- SadRazor.CLI is a reusable tool to load model data from JSON/YML/XML files, apply them to templates, and output rendered content to files.

## Implementation Progress

### Phase 1: Core Infrastructure [COMPLETED]
- [x] Project setup with correct SDK
- [x] Core interfaces defined
- [x] Basic models structure
- [x] NuGet dependencies added
- [x] Razor compilation pipeline
- [x] Basic template loading and parsing
- [x] Simple model binding
- [x] File output handling
- [x] Basic error handling and validation implemented

### Phase 2: Features [COMPLETED]
- [x] Layout templates support (removed from scope - intentional design decision)
- [x] Partial templates with comprehensive indentation support
- [x] **Partial Indentation System** - Complete implementation supporting:
      - [x] **Explicit indentation**: `IndentAmount` property for fixed space amounts
      - [x] **Inherit column**: `InheritColumn` property to match call-site column position
      - [x] **SkipFirstLineIndent**: Addresses inline partial insertion scenarios (defaults to `true`)
      - [x] **IndentationHelper**: Robust line-by-line indentation with edge case handling
      - [x] **Runtime column tracking**: `TemplateBase` tracks current column for inherit-column mode

### Phase 3: More Features [COMPLETED]

- [x] The SadRazor.CLI tool that supports the following features
  - [x] Generate output to a file in an output folder
  - [x] Input a JSON, YML, or XML file as the model
  - [x] Batch processing where you can specify a directory with a glob pattern for the model files
  - [x] Add support for the sadrazor.json config file in the existing commands

- [x] Engine improvements
  - [x] **Model Loading from Data Formats**: Added `ModelLoader` service and extension methods for loading models from JSON, YAML, and XML strings/files. Includes both dynamic and strongly-typed model loading.
  - [x] **Template Caching**: Implemented `TemplateCache` with configurable cache size, expiration time, and LRU eviction. Cache statistics available via `GetCacheStatistics()`.
  - [x] **Conditional Partial Rendering**: Added `PartialIf` and `PartialIfAsync` methods to both `TemplateHelpers` and `TemplateBase` for conditional partial rendering.
  - [x] **Template Validation**: Implemented validation mode with `ValidateAsync()` that attempts to render templates and captures errors for debugging model/template mismatches.

### Phase 4: Developer Experience [NOT STARTED]
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

### Test Project Structure (in Tests/SadRazorEngine.Tests)
1. Core Functionality Tests [COMPLETED]
   - [x] **TemplateEngineTests**: Basic template compilation, model binding, Markdown with Razor syntax
   - [x] **AdditionalTemplateTests**: Extended scenarios and edge cases
   - [x] **PartialTests**: Basic partial template functionality

2. Indentation System Tests [COMPLETED]
   - [x] **PartialIndentationTests**: Advanced indentation scenarios (12 tests)
   - [x] **SkipFirstLineIndentTests**: Comprehensive feature validation (9 tests)
   - [x] Full coverage of explicit indent, inherit column, and skip-first-line behaviors
   - [x] Tests for inline insertion, list contexts, sync/async methods

3. Test Coverage Summary [COMPLETED]
   - **21/21 tests passing** - No test failures
   - **Full backward compatibility** - Existing templates work unchanged
   - **Edge case handling** - Empty lines, trailing content, mixed scenarios
   - **Performance validation** - Tests run efficiently with realistic partial sizes

4. Performance Tests [PLANNED]
   - Compilation benchmarks
   - Rendering performance
   - Memory usage profiling

## Success Criteria
- Clean, intuitive API
- Efficient template compilation
- Thread-safe execution
- Proper error handling and debugging
- Comprehensive documentation
- Example templates and usage guides

## Current Status & Next Steps

### ✅ **COMPLETED FEATURES**
1. **Core Template Engine** - Fully functional Razor-based Markdown templating
2. **Model Binding** - Strong-typed model support with `@model` declarations
3. **Partial Templates** - Runtime `Partial`/`PartialAsync` helpers with full indentation control
4. **Indentation System** - Comprehensive support for `IndentAmount`, `InheritColumn`, and `SkipFirstLineIndent`
5. **Authoring Experience** - VS Code Razor tooling with intellisense support
6. **Testing Suite** - 21 passing tests covering all core functionality and edge cases
7. **CLI Tool** - Complete command-line interface with batch processing, multiple model formats, and configuration support
8. **Model Data Loading** - Support for JSON, YAML, and XML model loading with both dynamic and strongly-typed approaches
9. **Template Caching** - Configurable caching system with statistics and LRU eviction for improved performance
10. **Conditional Partials** - `PartialIf` and `PartialIfAsync` methods for conditional partial rendering
11. **Template Validation** - Validation mode to detect model/template mismatches before deployment

### 🎯 **NEXT PRIORITIES**
1. **Phase 4: Developer Experience** - Enhanced APIs and tooling
2. **Documentation** - Usage guides, API reference, and examples
3. **Performance Optimization** - Benchmarking and optimization opportunities
4. **Advanced Features** - Additional helper methods and workflow improvements
