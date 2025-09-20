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
- TemplateEngine: load templates from disk, detect `@model` declarations and - when possible - resolve concrete model Types and compile templates with that type. The engine prepares template content (e.g. adding runtime imports) so templates authored with shorthand helpers work at runtime. The engine intentionally does not perform compile-time inlining of fragments; authors should use the runtime `Partial`/`PartialAsync` helpers for composing reusable fragments.
- RazorTemplateCompiler: configure the RazorProjectEngine and code generation pipeline, populate default imports (including any required static imports for runtime helpers) and generate the C# source fed to Roslyn for compilation.
- CompiledTemplate: reflective wrapper around compiled template types; instantiates the generated template class, sets the runtime model via reflection, and injects a `TemplateBasePath` on the instance so runtime partial lookups resolve relative to the originating template file.
- TemplateBase: runtime base class for generated templates; exposes the model to generated code, provides protected helpers such as `Partial`/`PartialAsync` (which resolve relative paths and invoke the engine to render partials), and exposes a small API surface (`GetContent`, `SetTemplateBasePath`) useful for helpers and testing.
- TemplateExecutor: orchestrates execution of compiled templates (instantiation, model wiring, ExecuteAsync) and applies targeted post-render normalizations (for example, reversing the single-space prefix that was injected to avoid C# preprocessor collisions so Markdown headers like `#` are not indented).
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
- [ ] Layout templates support (removed from scope)
- [x] Partial templates
- [ ] Ability to indent partials by either:
      - [ ] Specific amount of whitespace
      - [ ] Inherit the column that the partial was called from (regardless of whitespace or not)

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

### Next Steps — Partials & Indentation (detailed)

Goal

- Provide authors a predictable way to render partial templates with correct indentation so embedded Markdown remains syntactically correct (e.g. nested lists, blockquotes and code fences) when a partial is rendered from an indented call site.

Design overview

- Two complementary indentation modes should be supported:
  1. Explicit indent amount: the caller supplies the number of spaces to prefix each line of the rendered partial.
  2. Inherit column: the engine infers the call-site column (the column position in the parent template where the partial was invoked) and indents the partial to align with that column.

- We'll add a small options model to represent indentation choices and pass it through the rendering pipeline so the compiler/runtime do not need to rewrite templates at compile time.

API proposals

- A new `PartialOptions` model (C#):

  - int? IndentAmount — If set, indent the rendered partial by this many spaces.
  - bool InheritColumn — If true, compute the caller's current column and indent to that column. If both are provided, `IndentAmount` wins.

- TemplateHelpers / TemplateBase: add overloads:
  - `Task<string> PartialAsync(string path, object? model = null, PartialOptions? options = null)`
  - `string Partial(string path, object? model = null, PartialOptions? options = null)`

- TemplateBase instrumentation:
  - Expose an internal API that exposes the current write cursor column (e.g. `protected int CurrentColumn { get; }`) so calling code can determine where the partial was invoked relative to the start of the current line. The implementation should be conservative and track literal output writes so the computed column reflects the character offset since the most recent newline.

Implementation notes

- Runtime flow:
  1. When a template calls `Partial(...)`, the TemplateBase implementation will compute the indent to apply: use `IndentAmount` if provided; otherwise if `InheritColumn` is true, use `CurrentColumn`.
  2. The engine will render the partial into an in-memory buffer (no layout composition for partials at this step), then apply indentation to that buffer. Indentation should:
     - Prefix each non-empty line with the requested number of spaces.
     - Preserve intentional leading blank lines in the partial output (do not add indentation to purely blank lines unless the author explicitly wants it).
     - Avoid breaking fenced code blocks: because fenced code uses explicit fence markers (```) the engine may safely indent fenced blocks in Markdown — but we should add tests confirming common Markdown renderers still treat indented fenced blocks correctly. If this proves problematic, we will detect fenced blocks and leave them unindented.
     - Respect trailing newline semantics. If the partial output does not end in a newline, preserve that behaviour.

- Inherit-column computation:
  - TemplateBase will update `CurrentColumn` whenever literal content is written (e.g. WriteLiteral or Write calls that contain no newline). When a newline is written, `CurrentColumn` resets to zero.
  - When Partial is invoked with `InheritColumn`, the Partial helper reads `CurrentColumn` and uses that as the indent amount.
  - This approach avoids compile-time source analysis and works across compiled templates because it uses runtime writer state.

- Integration with TemplateExecutor and Layouts:
  - Partial rendering will be performed by the engine in the same way existing Partials work today; after indentation is applied, the resulting content will be returned to the caller template's writer.
  - TemplateExecutor should continue to perform layout composition and the existing targeted post-render normalization (e.g. reversing the injected single-space used to avoid preprocessor collisions) after indentation is applied.

Edge cases and acceptance criteria

- Acceptance criteria:
  - Explicit indent: calling `Partial(..., new PartialOptions{ IndentAmount = 4 })` indents every non-empty line of the partial by 4 spaces; nested lists remain valid Markdown.
  - Inherit column: calling `Partial(..., new PartialOptions{ InheritColumn = true })` from a line that already has N characters after the most recent newline results in each non-empty line of the partial being prefixed with N spaces.
  - Partial calls inside list items (e.g. `- @Partial("_item.md", options: new PartialOptions{ InheritColumn = true })`) produce valid nested lists in the final Markdown.
  - Rendering performance: the extra in-memory buffer and line-by-line processing should not introduce large overhead for typical partial sizes. Add tests that verify render time within acceptable bounds for representative partials.
  - Tests for fenced code blocks will assert common Markdown renderers still recognize code fences after optional indentation.

Testing plan

- Unit tests:
  - Explicit indent basic (single-line, multi-line partials)
  - Inherit column basic (call site at several columns)
  - Multi-line partials with leading/trailing blank lines
  - Partials invoked within list items and blockquotes
  - Interaction with existing normalization that removes injected whitespace

- Integration tests:
  - Combine partials and verify partial indentation and normalization yields correct final Markdown
  - Authoring project templates to demonstrate usage patterns and for manual verification in VS Code

Rollout steps

1. Add the `PartialOptions` model and initial TemplateBase instrumentation to expose `CurrentColumn`.
2. Add Partial overloads and plumbing in `TemplateHelpers` to accept and forward `PartialOptions`.
3. Implement the runtime indentation logic inside the Partial helper (render to buffer, indent lines, return result).
4. Add unit tests for explicit indent and inherit-column behaviors.
5. Extend TemplateExecutor normalization and run integration tests; adjust edge-case handling as necessary.
6. Update the `Authoring` project and `PLAN.md` with final usage examples and docs.