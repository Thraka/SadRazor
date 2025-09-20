# SadRazorEngine Development Plan

## Overview
SadRazorEngine will be a lightweight, flexible Markdown templating system built on Razor technology, allowing developers to create Markdown documents using C# code and model binding.

## Core Components

### 1. Template Processing Pipeline
- **Template Loading**: Read Markdown files with Razor syntax
- **Compilation**: Transform Razor + Markdown into executable code
- **Execution**: Run compiled templates with model data
- **Output Generation**: Produce final Markdown output

### 2. Architecture

```
SadRazorEngine/
??? Core/
?   ??? Interfaces/
?   ?   ??? ITemplateEngine.cs       # Main entry point interface
?   ?   ??? ITemplateCompiler.cs     # Compilation abstraction
?   ?   ??? ITemplateRenderer.cs     # Rendering abstraction
?   ??? Models/
?       ??? TemplateContext.cs       # Template execution context
?       ??? TemplateResult.cs        # Execution result wrapper
??? Compilation/
?   ??? RazorTemplateCompiler.cs     # Razor compilation implementation
?   ??? CompilationResults.cs        # Compilation output wrapper
??? Runtime/
    ??? TemplateExecutor.cs          # Template execution engine
    ??? ModelBinder.cs               # Data binding implementation
```

## Implementation Phases

### Phase 1: Core Infrastructure
- Basic template loading and parsing
- Razor compilation pipeline
- Simple model binding
- File output handling

### Phase 2: Features
- Layout templates support
- Partial templates
- Include directives
- Custom Razor helpers for Markdown

### Phase 3: Developer Experience
- Fluent API design
- Comprehensive error handling
- Debugging support
- Documentation

## Dependencies
Required NuGet packages:
- Microsoft.AspNetCore.Razor.Language
- Microsoft.CodeAnalysis.CSharp

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
1. Unit Tests
   - Template compilation
   - Model binding
   - Markdown processing
   - Helper functions

2. Integration Tests
   - Full pipeline execution
   - Layout processing
   - Complex model scenarios

3. Performance Tests
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