using System.CommandLine;
using SadRazor.Cli.Models;
using SadRazor.Cli.Services;
using SadRazorEngine;
using SadRazorEngine.Extensions;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for rendering a single template with a model
/// </summary>
public class RenderCommand : Command
{
    public RenderCommand() : base("render", "Render a template with a model to produce Markdown output")
    {
        // Template argument (required, but can be resolved from config template directory)
        var templateArgument = new Argument<string>(
            name: "template",
            description: "Path to the Razor template file (.cshtml) - can be just filename if templateDirectory is set in config"
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
            // Load configuration file if available
            var config = await ConfigService.LoadConfigAsync();
            var configPath = ConfigService.FindConfigFile();
            
            // Create CLI options object
            var cliOptions = new RenderOptions
            {
                TemplatePath = templatePath,
                ModelPath = modelPath,
                OutputPath = outputPath,
                OutputDirectory = outputDirectory,
                ModelFormat = format,
                Force = force,
                Verbose = verbose
            };

            // Merge with config file settings
            var options = ConfigService.MergeRenderOptions(cliOptions, config, configPath);

            // Resolve template path - combine with template directory if needed
            string? resolvedTemplatePath = options.TemplatePath;
            if (!string.IsNullOrEmpty(templatePath) && !string.IsNullOrEmpty(config?.TemplateDirectory))
            {
                // If template path is just a filename and we have a template directory, combine them
                if (!Path.IsPathRooted(templatePath) && !templatePath.Contains(Path.DirectorySeparatorChar) && !templatePath.Contains(Path.AltDirectorySeparatorChar))
                {
                    var templateDir = !string.IsNullOrEmpty(config.TemplateDirectory) 
                        ? Path.IsPathRooted(config.TemplateDirectory) 
                            ? config.TemplateDirectory 
                            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath) ?? "", config.TemplateDirectory))
                        : null;
                    if (!string.IsNullOrEmpty(templateDir))
                    {
                        resolvedTemplatePath = Path.Combine(templateDir, templatePath);
                    }
                }
            }

            // Update options with resolved template path
            options.TemplatePath = resolvedTemplatePath ?? templatePath;

            if (options.Verbose)
            {
                if (config != null)
                    Console.WriteLine($"Using config file: {configPath}");
                else
                    Console.WriteLine("No config file found, using command line options only.");
            }

            // Validate template file
            if (!File.Exists(options.TemplatePath!))
            {
                Console.Error.WriteLine($"Error: Template file not found: {options.TemplatePath}");
                return 1;
            }

            if (options.Verbose)
            {
                Console.WriteLine($"Template: {options.TemplatePath}");
                Console.WriteLine($"Model: {options.ModelPath ?? "(none)"}");
                Console.WriteLine($"Format: {options.ModelFormat}");
            }

            // Create template engine with caching enabled
            var engine = new TemplateEngine(enableCaching: true);

            if (options.Verbose)
                Console.WriteLine("Compiling template...");

            // Load template
            var context = engine.LoadTemplate(options.TemplatePath!);

            // Load model if provided using engine's model loading capabilities
            if (!string.IsNullOrEmpty(options.ModelPath))
            {
                if (!File.Exists(options.ModelPath))
                {
                    // Try appending the model path to the model directory from config
                    if (config?.ModelDirectory != null && File.Exists(Path.Combine(config.ModelDirectory, options.ModelPath)))
                        options.ModelPath = Path.Combine(config.ModelDirectory, options.ModelPath);
                    else
                    {
                        Console.Error.WriteLine($"Error: Model file not found: {options.ModelPath}");
                        return 1;
                    }
                }

                if (options.Verbose)
                    Console.WriteLine("Loading model...");

                try
                {
                    // Use engine's model loading capabilities
                    context = await context.WithModelFromFileAsync(options.ModelPath);

                    if (options.Verbose)
                        Console.WriteLine("Model loaded successfully.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error loading model: {ex.Message}");
                    return 1;
                }
            }

            // Render template
            var result = await context.RenderAsync();

            if (options.Verbose)
                Console.WriteLine("Template rendered successfully.");

            // Determine output path
            var finalOutputPath = OutputManager.GenerateOutputPath(options.TemplatePath!, options.ModelPath, options.OutputPath, options.OutputDirectory);

            if (options.Verbose)
                Console.WriteLine($"Output: {finalOutputPath}");

            // Check if we can write to output file
            if (!OutputManager.CanWriteFile(finalOutputPath, options.Force))
            {
                Console.Error.WriteLine($"Error: Output file already exists: {finalOutputPath}");
                Console.Error.WriteLine("Use --force to overwrite existing files.");
                return 1;
            }

            // Write output file
            await OutputManager.WriteFileAsync(finalOutputPath, result.Content, options.Force);

            // Success message
            var displayPath = OutputManager.GetDisplayPath(finalOutputPath);
            Console.WriteLine($"Successfully rendered template to: {displayPath}");

            if (options.Verbose)
            {
                Console.WriteLine($"Output size: {result.Content.Length} characters");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            
            // Try to get verbose setting - either from merged options or fallback to CLI
            var verboseMode = false;
            try
            {
                var tempConfig = await ConfigService.LoadConfigAsync();
                var tempOptions = ConfigService.MergeRenderOptions(new RenderOptions { Verbose = verbose }, tempConfig);
                verboseMode = tempOptions.Verbose;
            }
            catch
            {
                verboseMode = verbose;
            }
            
            if (verboseMode)
            {
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.ToString());
            }
            return 1;
        }
    }
}