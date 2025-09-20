using System.CommandLine;
using SadRazor.Cli.Models;
using SadRazor.Cli.Services;
using SadRazorEngine;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for validating templates and models
/// </summary>
public class ValidateCommand : Command
{
    public ValidateCommand() : base("validate", "Validate templates and models for syntax and compatibility")
    {
        // Template argument
        var templateArgument = new Argument<string>(
            name: "template",
            description: "Path to the Razor template file (.cshtml) to validate"
        );

        // Model option
        var modelOption = new Option<string?>(
            aliases: ["--model", "-m"],
            description: "Path to the model file to validate against the template"
        );

        // Check syntax option
        var syntaxOption = new Option<bool>(
            aliases: ["--syntax"],
            getDefaultValue: () => true,
            description: "Check template syntax (default: true)"
        );

        // Check model option
        var modelCheckOption = new Option<bool>(
            aliases: ["--model-check"],
            getDefaultValue: () => true,
            description: "Check model compatibility with template (default: true)"
        );

        // Check output option
        var outputCheckOption = new Option<bool>(
            aliases: ["--output-check"],
            description: "Attempt to render and validate output (default: false)"
        );

        // Format option
        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            getDefaultValue: () => "auto",
            description: "Model format: json, yaml, xml, or auto (default: auto)"
        );

        // Verbose option
        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output"
        );

        AddArgument(templateArgument);
        AddOption(modelOption);
        AddOption(syntaxOption);
        AddOption(modelCheckOption);
        AddOption(outputCheckOption);
        AddOption(formatOption);
        AddOption(verboseOption);

        this.SetHandler(ExecuteAsync,
            templateArgument,
            modelOption,
            syntaxOption,
            modelCheckOption,
            outputCheckOption,
            formatOption,
            verboseOption
        );
    }

    private static async Task<int> ExecuteAsync(
        string templatePath,
        string? modelPath,
        bool checkSyntax,
        bool checkModel,
        bool checkOutput,
        string format,
        bool verbose)
    {
        var validationErrors = new List<string>();
        var validationWarnings = new List<string>();

        try
        {
            // Load configuration file if available
            var config = await ConfigService.LoadConfigAsync();
            var configPath = ConfigService.FindConfigFile();
            
            // Create CLI options object
            var cliOptions = new ValidateOptions
            {
                TemplatePath = templatePath,
                ModelPath = modelPath,
                CheckSyntax = checkSyntax,
                CheckModel = checkModel,
                CheckOutput = checkOutput,
                Verbose = verbose
            };

            // Merge with config file settings
            var options = ConfigService.MergeValidateOptions(cliOptions, config, configPath);

            if (options.Verbose)
            {
                if (config != null)
                    Console.WriteLine($"Using config file: {configPath}");
                else
                    Console.WriteLine("No config file found, using command line options only.");
                
                Console.WriteLine($"Validating template: {options.TemplatePath}");
                if (!string.IsNullOrEmpty(options.ModelPath))
                    Console.WriteLine($"Model: {options.ModelPath}");
                Console.WriteLine($"Checks: Syntax={options.CheckSyntax}, Model={options.CheckModel}, Output={options.CheckOutput}");
                Console.WriteLine();
            }

            // Check if template file exists
            if (!File.Exists(options.TemplatePath!))
            {
                validationErrors.Add($"Template file not found: {options.TemplatePath}");
                return ReportResults(validationErrors, validationWarnings, options.Verbose);
            }

            // Check template syntax
            if (checkSyntax)
            {
                await ValidateTemplateSyntax(templatePath, validationErrors, validationWarnings, verbose);
            }

            // Check model if provided
            object? model = null;
            if (!string.IsNullOrEmpty(modelPath))
            {
                model = await ValidateModel(modelPath, format, validationErrors, validationWarnings, verbose);
            }

            // Check model compatibility with template
            if (checkModel && !string.IsNullOrEmpty(modelPath) && model != null)
            {
                await ValidateModelCompatibility(templatePath, model, validationErrors, validationWarnings, verbose);
            }

            // Check output generation
            if (checkOutput)
            {
                await ValidateOutput(templatePath, model, validationErrors, validationWarnings, verbose);
            }

            return ReportResults(validationErrors, validationWarnings, verbose);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Validation failed with error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.ToString());
            }
            return 1;
        }
    }

    private static async Task ValidateTemplateSyntax(
        string templatePath,
        List<string> errors,
        List<string> warnings,
        bool verbose)
    {
        if (verbose)
            Console.WriteLine("Checking template syntax...");

        try
        {
            var engine = new TemplateEngine();
            var templateContent = await File.ReadAllTextAsync(templatePath);

            // Try to load the template to check syntax
            try
            {
                engine.LoadTemplate(templatePath);
                if (verbose)
                    Console.WriteLine("✓ Template syntax is valid");
            }
            catch (Exception ex)
            {
                errors.Add($"Template syntax error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read template file: {ex.Message}");
        }
    }

    private static async Task<object?> ValidateModel(
        string modelPath,
        string format,
        List<string> errors,
        List<string> warnings,
        bool verbose)
    {
        if (verbose)
            Console.WriteLine("Validating model...");

        try
        {
            if (!File.Exists(modelPath))
            {
                errors.Add($"Model file not found: {modelPath}");
                return null;
            }

            if (!ModelLoader.IsFormatSupported(modelPath) && format == "auto")
            {
                errors.Add($"Unsupported model file format: {Path.GetExtension(modelPath)}");
                errors.Add($"Supported formats: {string.Join(", ", ModelLoader.GetSupportedExtensions())}");
                return null;
            }

            var model = await ModelLoader.LoadFromFileAsync(modelPath, format);
            
            if (model == null)
            {
                warnings.Add("Model file is empty or contains no data");
                return null;
            }

            if (verbose)
                Console.WriteLine("✓ Model loaded successfully");

            return model;
        }
        catch (Exception ex)
        {
            errors.Add($"Model validation error: {ex.Message}");
            return null;
        }
    }

    private static async Task ValidateModelCompatibility(
        string templatePath,
        object model,
        List<string> errors,
        List<string> warnings,
        bool verbose)
    {
        if (verbose)
            Console.WriteLine("Checking model compatibility...");

        try
        {
            var engine = new TemplateEngine();
            engine.LoadTemplate(templatePath);

            // Check if template has @model directive
            var templateContent = await File.ReadAllTextAsync(templatePath);
            var hasModelDirective = templateContent.Contains("@model");

            if (!hasModelDirective && model != null)
            {
                warnings.Add("Template does not declare @model but model data was provided");
            }
            else if (hasModelDirective && model == null)
            {
                warnings.Add("Template declares @model but no model data was provided");
            }

            if (verbose)
                Console.WriteLine("✓ Model compatibility checked");
        }
        catch (Exception ex)
        {
            errors.Add($"Model compatibility check failed: {ex.Message}");
        }
    }

    private static async Task ValidateOutput(
        string templatePath,
        object? model,
        List<string> errors,
        List<string> warnings,
        bool verbose)
    {
        if (verbose)
            Console.WriteLine("Validating output generation...");

        try
        {
            var engine = new TemplateEngine();
            var result = await engine.LoadTemplate(templatePath)
                .WithModel(model)
                .RenderAsync();

            if (string.IsNullOrWhiteSpace(result.Content))
            {
                warnings.Add("Template generates empty output");
            }
            else
            {
                // Basic markdown validation
                var lines = result.Content.Split('\n');
                var hasContent = lines.Any(line => !string.IsNullOrWhiteSpace(line));
                
                if (!hasContent)
                {
                    warnings.Add("Generated output contains only whitespace");
                }
                
                if (verbose)
                {
                    Console.WriteLine($"✓ Output generated successfully ({result.Content.Length} characters)");
                    
                    // Show a preview of the output
                    var preview = result.Content.Length > 200 
                        ? result.Content.Substring(0, 200) + "..." 
                        : result.Content;
                    
                    Console.WriteLine("Output preview:");
                    Console.WriteLine("--- START ---");
                    Console.WriteLine(preview);
                    Console.WriteLine("--- END ---");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Output validation failed: {ex.Message}");
        }
    }

    private static int ReportResults(
        List<string> errors,
        List<string> warnings,
        bool verbose)
    {
        Console.WriteLine();
        Console.WriteLine("=== Validation Results ===");

        if (errors.Count == 0 && warnings.Count == 0)
        {
            Console.WriteLine("✅ All validations passed!");
            return 0;
        }

        if (warnings.Count > 0)
        {
            Console.WriteLine($"⚠️  {warnings.Count} warning(s):");
            foreach (var warning in warnings)
            {
                Console.WriteLine($"   • {warning}");
            }
            Console.WriteLine();
        }

        if (errors.Count > 0)
        {
            Console.WriteLine($"❌ {errors.Count} error(s):");
            foreach (var error in errors)
            {
                Console.WriteLine($"   • {error}");
            }
            Console.WriteLine();
            Console.WriteLine("❌ Validation failed!");
            return 1;
        }

        Console.WriteLine("⚠️  Validation completed with warnings");
        return 0;
    }
}