using System.CommandLine;
using SadRazor.Cli.Services;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for initializing new SadRazor projects
/// </summary>
public class InitCommand : Command
{
    public InitCommand() : base("init", "Initialize a new SadRazor project structure")
    {
        // Directory argument
        var directoryArgument = new Argument<string>(
            name: "directory",
            getDefaultValue: () => Environment.CurrentDirectory,
            description: "Target directory for the new project"
        );

        // Project name option
        var nameOption = new Option<string?>(
            aliases: ["--name", "-n"],
            description: "Project name (defaults to directory name)"
        );

        // Template type option
        var typeOption = new Option<string>(
            aliases: ["--type", "-t"],
            getDefaultValue: () => "basic",
            description: "Project type: basic, blog, documentation (default: basic)"
        );

        // Force option
        var forceOption = new Option<bool>(
            aliases: ["--force"],
            description: "Overwrite existing files"
        );

        // Verbose option
        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output"
        );

        AddArgument(directoryArgument);
        AddOption(nameOption);
        AddOption(typeOption);
        AddOption(forceOption);
        AddOption(verboseOption);

        this.SetHandler(ExecuteAsync, directoryArgument, nameOption, typeOption, forceOption, verboseOption);
    }

    private static async Task<int> ExecuteAsync(
        string directory,
        string? projectName,
        string projectType,
        bool force,
        bool verbose)
    {
        try
        {
            var fullPath = Path.GetFullPath(directory);
            projectName ??= Path.GetFileName(fullPath);

            if (verbose)
            {
                Console.WriteLine($"Initializing project: {projectName}");
                Console.WriteLine($"Directory: {fullPath}");
                Console.WriteLine($"Type: {projectType}");
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                if (verbose)
                    Console.WriteLine($"Created directory: {fullPath}");
            }

            // Create project structure
            await CreateProjectStructure(fullPath, projectName, projectType, force, verbose);

            Console.WriteLine($"Successfully initialized SadRazor project: {projectName}");
            Console.WriteLine($"Project created in: {OutputManager.GetDisplayPath(fullPath)}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("1. Edit templates in the templates/ directory");
            Console.WriteLine("2. Add model data in the models/ directory");
            Console.WriteLine("3. Run: sadrazor render templates/example.cshtml --model models/example.json");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.ToString());
            }
            return 1;
        }
    }

    private static async Task CreateProjectStructure(
        string projectPath,
        string projectName,
        string projectType,
        bool force,
        bool verbose)
    {
        // Create directories
        var directories = new[]
        {
            "templates",
            "models",
            "output",
            "partials"
        };

        foreach (var dir in directories)
        {
            var dirPath = Path.Combine(projectPath, dir);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                if (verbose)
                    Console.WriteLine($"Created directory: {dir}/");
            }
        }

        // Create config file
        var configPath = Path.Combine(projectPath, "sadrazor.json");
        if (!File.Exists(configPath) || force)
        {
            var configContent = GetConfigFileContent(projectName);
            await OutputManager.WriteFileAsync(configPath, configContent, force);
            if (verbose)
                Console.WriteLine("Created: sadrazor.json");
        }

        // Create example files based on project type
        await CreateExampleFiles(projectPath, projectType, force, verbose);

        // Create README
        var readmePath = Path.Combine(projectPath, "README.md");
        if (!File.Exists(readmePath) || force)
        {
            var readmeContent = GetReadmeContent(projectName, projectType);
            await OutputManager.WriteFileAsync(readmePath, readmeContent, force);
            if (verbose)
                Console.WriteLine("Created: README.md");
        }

        // Create .gitignore
        var gitignorePath = Path.Combine(projectPath, ".gitignore");
        if (!File.Exists(gitignorePath) || force)
        {
            var gitignoreContent = GetGitignoreContent();
            await OutputManager.WriteFileAsync(gitignorePath, gitignoreContent, force);
            if (verbose)
                Console.WriteLine("Created: .gitignore");
        }
    }

    private static async Task CreateExampleFiles(string projectPath, string projectType, bool force, bool verbose)
    {
        switch (projectType.ToLowerInvariant())
        {
            case "basic":
                await CreateBasicExamples(projectPath, force, verbose);
                break;
            case "blog":
                await CreateBlogExamples(projectPath, force, verbose);
                break;
            case "documentation":
                await CreateDocumentationExamples(projectPath, force, verbose);
                break;
            default:
                await CreateBasicExamples(projectPath, force, verbose);
                break;
        }
    }

    private static async Task CreateBasicExamples(string projectPath, bool force, bool verbose)
    {
        // Example template
        var templatePath = Path.Combine(projectPath, "templates", "example.cshtml");
        if (!File.Exists(templatePath) || force)
        {
            var templateContent = """
@model dynamic

# @Model.Title

@Model.Description

## Details

**Created:** @Model.CreatedDate  
**Author:** @Model.Author

@if (Model.Tags != null) {
## Tags

@foreach (var tag in Model.Tags) {
- #@tag
}
}

@if (Model.Content != null) {
## Content

@Model.Content
}
""";
            await OutputManager.WriteFileAsync(templatePath, templateContent, force);
            if (verbose)
                Console.WriteLine("Created: templates/example.cshtml");
        }

        // Example model
        var modelPath = Path.Combine(projectPath, "models", "example.json");
        if (!File.Exists(modelPath) || force)
        {
            var modelContent = """
{
  "Title": "My First SadRazor Document",
  "Description": "This is an example document created with SadRazor templating engine.",
  "Author": "Your Name",
  "CreatedDate": "2025-09-19",
  "Tags": ["sadrazor", "markdown", "template"],
  "Content": "This content section demonstrates how to use dynamic models in your templates. You can add any data structure here and access it from your Razor templates."
}
""";
            await OutputManager.WriteFileAsync(modelPath, modelContent, force);
            if (verbose)
                Console.WriteLine("Created: models/example.json");
        }
    }

    private static async Task CreateBlogExamples(string projectPath, bool force, bool verbose)
    {
        // Blog post template
        var templatePath = Path.Combine(projectPath, "templates", "blog-post.cshtml");
        if (!File.Exists(templatePath) || force)
        {
            var templateContent = """
@model dynamic

# @Model.Title

*Published on @Model.PublishDate by @Model.Author*

@Model.Excerpt

---

@Model.Content

@if (Model.Tags != null && Model.Tags.Count > 0) {
## Tags

@foreach (var tag in Model.Tags) {
[#@tag](#) 
}
}

@if (Model.Category != null) {
**Category:** @Model.Category
}

---

*Originally published at [@Model.SiteName](@Model.SiteUrl)*
""";
            await OutputManager.WriteFileAsync(templatePath, templateContent, force);
            if (verbose)
                Console.WriteLine("Created: templates/blog-post.cshtml");
        }

        // Blog post model
        var modelPath = Path.Combine(projectPath, "models", "blog-post.json");
        if (!File.Exists(modelPath) || force)
        {
            var modelContent = """
{
  "Title": "Getting Started with SadRazor",
  "Author": "Jane Developer",
  "PublishDate": "September 19, 2025",
  "Category": "Tutorial",
  "Excerpt": "Learn how to create dynamic Markdown documents using SadRazor's Razor-based templating system.",
  "Content": "SadRazor makes it easy to generate Markdown documents from templates and data. In this post, we'll explore the basic concepts and show you how to get up and running quickly.\n\n## Why SadRazor?\n\nSadRazor combines the power of Razor templates with the simplicity of Markdown output. This gives you:\n\n- **Type-safe templating** with full IntelliSense support\n- **Familiar syntax** if you're coming from ASP.NET Core\n- **Clean Markdown output** that works everywhere\n\n## Getting Started\n\nTo create your first template, simply create a `.cshtml` file with your Razor markup and run the SadRazor CLI to render it with your data.",
  "Tags": ["sadrazor", "tutorial", "markdown", "razor"],
  "SiteName": "My Developer Blog",
  "SiteUrl": "https://example.com"
}
""";
            await OutputManager.WriteFileAsync(modelPath, modelContent, force);
            if (verbose)
                Console.WriteLine("Created: models/blog-post.json");
        }
    }

    private static async Task CreateDocumentationExamples(string projectPath, bool force, bool verbose)
    {
        // API documentation template
        var templatePath = Path.Combine(projectPath, "templates", "api-doc.cshtml");
        if (!File.Exists(templatePath) || force)
        {
            var templateContent = """
@model dynamic

# @Model.ApiName Documentation

@Model.Description

## Base URL

```
@Model.BaseUrl
```

## Authentication

@Model.Authentication.Description

@if (Model.Authentication.Example != null) {
**Example:**
```
@Model.Authentication.Example
```
}

## Endpoints

@foreach (var endpoint in Model.Endpoints) {
### @endpoint.Method @endpoint.Path

@endpoint.Description

@if (endpoint.Parameters != null && endpoint.Parameters.Count > 0) {
**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
@foreach (var param in endpoint.Parameters) {
| `@param.Name` | @param.Type | @(param.Required ? "Yes" : "No") | @param.Description |
}
}

@if (endpoint.Example != null) {
**Example Request:**
```@endpoint.Example.RequestType
@endpoint.Example.Request
```

**Example Response:**
```@endpoint.Example.ResponseType
@endpoint.Example.Response
```
}

---

}

## Error Codes

| Code | Description |
|------|-------------|
@foreach (var error in Model.ErrorCodes) {
| @error.Code | @error.Description |
}
""";
            await OutputManager.WriteFileAsync(templatePath, templateContent, force);
            if (verbose)
                Console.WriteLine("Created: templates/api-doc.cshtml");
        }

        // API documentation model
        var modelPath = Path.Combine(projectPath, "models", "api-doc.json");
        if (!File.Exists(modelPath) || force)
        {
            var modelContent = """
{
  "ApiName": "My API",
  "Description": "A RESTful API for managing resources in your application.",
  "BaseUrl": "https://api.example.com/v1",
  "Authentication": {
    "Description": "This API uses Bearer token authentication. Include your token in the Authorization header.",
    "Example": "Authorization: Bearer your-api-token-here"
  },
  "Endpoints": [
    {
      "Method": "GET",
      "Path": "/users",
      "Description": "Retrieve a list of all users.",
      "Parameters": [
        {
          "Name": "page",
          "Type": "integer",
          "Required": false,
          "Description": "Page number for pagination (default: 1)"
        },
        {
          "Name": "limit",
          "Type": "integer",
          "Required": false,
          "Description": "Number of results per page (default: 20, max: 100)"
        }
      ],
      "Example": {
        "RequestType": "http",
        "Request": "GET /users?page=1&limit=10",
        "ResponseType": "json",
        "Response": "{\n  \"users\": [\n    {\n      \"id\": 1,\n      \"name\": \"John Doe\",\n      \"email\": \"john@example.com\"\n    }\n  ],\n  \"pagination\": {\n    \"page\": 1,\n    \"limit\": 10,\n    \"total\": 1\n  }\n}"
      }
    },
    {
      "Method": "POST",
      "Path": "/users",
      "Description": "Create a new user.",
      "Parameters": [
        {
          "Name": "name",
          "Type": "string",
          "Required": true,
          "Description": "The user's full name"
        },
        {
          "Name": "email",
          "Type": "string",
          "Required": true,
          "Description": "The user's email address"
        }
      ],
      "Example": {
        "RequestType": "json",
        "Request": "{\n  \"name\": \"Jane Smith\",\n  \"email\": \"jane@example.com\"\n}",
        "ResponseType": "json",
        "Response": "{\n  \"id\": 2,\n  \"name\": \"Jane Smith\",\n  \"email\": \"jane@example.com\",\n  \"created_at\": \"2025-09-19T10:30:00Z\"\n}"
      }
    }
  ],
  "ErrorCodes": [
    {
      "Code": 400,
      "Description": "Bad Request - Invalid request parameters"
    },
    {
      "Code": 401,
      "Description": "Unauthorized - Invalid or missing API token"
    },
    {
      "Code": 404,
      "Description": "Not Found - Requested resource does not exist"
    },
    {
      "Code": 500,
      "Description": "Internal Server Error - Something went wrong on our end"
    }
  ]
}
""";
            await OutputManager.WriteFileAsync(modelPath, modelContent, force);
            if (verbose)
                Console.WriteLine("Created: models/api-doc.json");
        }
    }

    private static string GetConfigFileContent(string projectName)
    {
        return """
{
  "templateDirectory": "templates",
  "modelDirectory": "models",
  "outputDirectory": "output",
  "defaultModelFormat": "auto",
  "batch": {
    "modelGlobPattern": "**/*.{json,yml,yaml,xml}",
    "templateGlobPattern": "**/*.cshtml",
    "recursive": true,
    "outputPattern": "{name}.md"
  },
  "settings": {
    "verbose": false,
    "force": false,
    "encoding": "utf-8",
    "cache": {
      "enabled": true,
      "maxAgeMinutes": 60
    }
  }
}
""";
    }

    private static string GetReadmeContent(string projectName, string projectType)
    {
        return $"""
# {projectName}

This is a SadRazor project for generating Markdown documents from Razor templates.

## Project Structure

```
{projectName}/
├── templates/          # Razor template files (.cshtml)
├── models/            # Data files (JSON, YAML, XML)
├── output/            # Generated Markdown files
├── partials/          # Reusable template fragments
├── sadrazor.json      # Project configuration
└── README.md          # This file
```

## Getting Started

1. **Edit templates**: Modify files in the `templates/` directory
2. **Update models**: Add your data in the `models/` directory
3. **Generate output**: Run SadRazor commands to create Markdown files

## Common Commands

```bash
# Render a single template
sadrazor render templates/example.cshtml --model models/example.json

# Render with explicit output
sadrazor render templates/example.cshtml --model models/example.json --output README.md

# Batch process all models
sadrazor batch templates/example.cshtml models/
```

## Project Type: {projectType}

{GetProjectTypeDescription(projectType)}

## Configuration

Edit `sadrazor.json` to customize:

- Default directories
- Output patterns
- Batch processing options

## Learn More

- [SadRazor Documentation](https://github.com/your-org/sadrazor)
- [Razor Syntax Reference](https://docs.microsoft.com/aspnet/core/mvc/views/razor)
- [Markdown Guide](https://www.markdownguide.org/)
""";
    }

    private static string GetProjectTypeDescription(string projectType)
    {
        return projectType.ToLowerInvariant() switch
        {
            "blog" => "This project is configured for blog post generation with examples showing how to create blog posts from structured data.",
            "documentation" => "This project is set up for API or technical documentation generation with examples for creating comprehensive docs.",
            _ => "This is a basic SadRazor project with simple examples to get you started with template-based Markdown generation."
        };
    }

    private static string GetGitignoreContent()
    {
        return """
# SadRazor output
output/
*.md

# Temporary files
*.tmp
*.temp

# IDE files
.vs/
.vscode/
*.user
*.suo

# OS files
.DS_Store
Thumbs.db

# Logs
*.log

# Cache
.cache/
""";
    }
}