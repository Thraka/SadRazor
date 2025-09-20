# SadRazorEngine Development Plan

## Overview
SadRazorEngine will be a lightweight, flexible Markdown templating system built on Razor technology, allowing developers to create Markdown documents using C# code and model binding.

## Core Components

### 1. Template Processing Pipeline
- **Template Loading**: Read Markdown files with Razor syntax
- **Compilation**: Transform Razor + Markdown into executable code ?
- **Execution**: Run compiled templates with model data
- **Output Generation**: Produce final Markdown output

### 2. Architecture

```
SadRazorEngine/
├── Core/
│   ├── Interfaces/
│   │   ├── ITemplateEngine.cs       # Main entry point interface
│   │   ├── ITemplateCompiler.cs     # Compilation abstraction
│   │   └── ITemplateRenderer.cs     # Rendering abstraction
│   └── Models/
│       ├── TemplateContext.cs       # Template execution context
│       └── TemplateResult.cs        # Result wrapper (Implemented in TemplateContext.cs)
├── Compilation/
│   ├── RazorTemplateCompiler.cs     # Razor compilation implementation
│   └── CompiledTemplate.cs          # Compilation output wrapper
└── Runtime/
    ├── TemplateBase.cs              # Base template class
    ├── TemplateExecutor.cs          # Template execution engine (Next)
    └── ModelBinder.cs               # Data binding implementation
```

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
- [ ] Custom Razor helpers for Markdown

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