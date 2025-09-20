using System.CommandLine;
using SadRazor.Cli.Models;
using SadRazor.Cli.Services;
using SadRazorEngine;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for rendering a single template with a model
/// </summary>
public class RenderCommand : Command
{
    public RenderCommand() : base("render", "Render a template with a model to produce Markdown output")
    {
        // Template argument (required)
        var templateArgument = new Argument<string>(
            name: "template",
            description: "Path to the Razor template file (.cshtml)"
        );

        // Model option
        var modelOption = new Option<string?>(
            aliases: ["--model", "-m"],
            description: "Path to the model file (JSON, YAML, or XML)"
        );

        // Output option
        var outputOption = new Option<string?>(
            aliases: ["--output", "-o"],
            description: "Output file path (defaults to model name + .md)"
        );

        // Output directory option
        var outputDirOption = new Option<string?>(
            aliases: ["--output-dir", "-d"],
            description: "Output directory (defaults to model directory)"
        );

        // Model format option
        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            getDefaultValue: () => "auto",
            description: "Model format: json, yaml, xml, or auto (default: auto)"
        );

        // Force option
        var forceOption = new Option<bool>(
            aliases: ["--force"],
            description: "Overwrite existing output files"
        );

        // Verbose option
        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output"
        );

        // Add options to command
        AddArgument(templateArgument);
        AddOption(modelOption);
        AddOption(outputOption);
        AddOption(outputDirOption);
        AddOption(formatOption);
        AddOption(forceOption);
        AddOption(verboseOption);

        this.SetHandler(ExecuteAsync, 
            templateArgument, 
            modelOption, 
            outputOption, 
            outputDirOption, 
            formatOption, 
            forceOption, 
            verboseOption
        );
    }

    private static async Task<int> ExecuteAsync(
        string templatePath,
        string? modelPath,
        string? outputPath,
        string? outputDirectory,
        string format,
        bool force,
        bool verbose)
    {
        try
        {
            // Validate template file
            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine($"Error: Template file not found: {templatePath}");
                return 1;
            }

            if (verbose)
            {
                Console.WriteLine($"Template: {templatePath}");
                Console.WriteLine($"Model: {modelPath ?? "(none)"}");
                Console.WriteLine($"Format: {format}");
            }

            // Load model if provided
            object? model = null;
            if (!string.IsNullOrEmpty(modelPath))
            {
                if (!File.Exists(modelPath))
                {
                    Console.Error.WriteLine($"Error: Model file not found: {modelPath}");
                    return 1;
                }

                if (!ModelLoader.IsFormatSupported(modelPath) && format == "auto")
                {
                    Console.Error.WriteLine($"Error: Unsupported model file format: {Path.GetExtension(modelPath)}");
                    Console.Error.WriteLine($"Supported formats: {string.Join(", ", ModelLoader.GetSupportedExtensions())}");
                    return 1;
                }

                if (verbose)
                    Console.WriteLine("Loading model...");

                model = await ModelLoader.LoadFromFileAsync(modelPath, format);

                if (verbose)
                    Console.WriteLine("Model loaded successfully.");
            }

            // Create template engine
            var engine = new TemplateEngine();

            if (verbose)
                Console.WriteLine("Compiling template...");

            // Load and render template
            var result = await engine.LoadTemplate(templatePath)
                .WithModel(model)
                .RenderAsync();

            if (verbose)
                Console.WriteLine("Template rendered successfully.");

            // Determine output path
            var finalOutputPath = OutputManager.GenerateOutputPath(templatePath, modelPath, outputPath, outputDirectory);

            if (verbose)
                Console.WriteLine($"Output: {finalOutputPath}");

            // Check if we can write to output file
            if (!OutputManager.CanWriteFile(finalOutputPath, force))
            {
                Console.Error.WriteLine($"Error: Output file already exists: {finalOutputPath}");
                Console.Error.WriteLine("Use --force to overwrite existing files.");
                return 1;
            }

            // Write output file
            await OutputManager.WriteFileAsync(finalOutputPath, result.Content, force);

            // Success message
            var displayPath = OutputManager.GetDisplayPath(finalOutputPath);
            Console.WriteLine($"Successfully rendered template to: {displayPath}");

            if (verbose)
            {
                Console.WriteLine($"Output size: {result.Content.Length} characters");
            }

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
}