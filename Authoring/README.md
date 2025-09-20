Authoring project

This small project helps author SadRazor templates with editor tooling (Razor/C# support) and a tiny runtime preview.

Quickstart

1. Open the workspace in VS Code. The Razor language features (C# extension) will provide syntax highlighting and some IntelliSense for `.cshtml` files in this project.
2. Add reference to your library to import types for the model.
2. Edit `Views\*.cshtml` files, but don't touch `Views\_ViewImports.cshtml`.
3. Preview by running:

   dotnet run -- Views/Samples/Example.cshtml

   The rendered output will be printed to stdout.

Tips

- Because `Authoring.csproj` references the main `SadRazorEngine` project, template base type and helpers defined there will be available to the editor for basic IntelliSense.
- Use `@model` at the top of your template to get strongly-typed editing if you define model types in the referenced projects.
- Add snippets in VS Code to scaffold common patterns (partials, sections, @model, etc.)
