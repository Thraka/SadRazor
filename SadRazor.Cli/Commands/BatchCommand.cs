using System.CommandLine;
using System.CommandLine.Invocation;
using SadRazor.Cli.Models;
using SadRazor.Cli.Services;
using SadRazorEngine;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for batch processing multiple models with a template
/// </summary>
public class BatchCommand : Command
{
    public BatchCommand() : base("batch", "Batch process multiple model files with a template")
    {
        // Template argument (required unless in config)
        var templateArgument = new Argument<string?>(
            name: "template",
            description: "Path to the Razor template file (.cshtml) - can be specified in config file"
        );
        templateArgument.SetDefaultValue(null);

        // Model directory option (can come from config)
        var modelDirOption = new Option<string?>(
            aliases: ["--model-dir", "-m"],
            description: "Directory containing model files to process (can be specified in config file)"
        );

        // Output directory option
        var outputDirOption = new Option<string?>(
            aliases: ["--output-dir", "-o"],
            description: "Output directory (defaults to model directory)"
        );

        // Model glob pattern option
        var modelPatternOption = new Option<string?>(
            aliases: ["--model-pattern", "-p"],
            description: "Glob pattern for model files (default: **/*.{json,yml,yaml,xml})"
        );

        // Output pattern option
        var outputPatternOption = new Option<string?>(
            aliases: ["--output-pattern"],
            description: "Output filename pattern. Supports {name}, {ext}, {dir} placeholders (default: {name}.md)"
        );

        // Recursive option
        var recursiveOption = new Option<bool>(
            aliases: ["--recursive", "-r"],
            getDefaultValue: () => true,
            description: "Process subdirectories recursively (default: true)"
        );

        // Force option
        var forceOption = new Option<bool>(
            aliases: ["--force"],
            description: "Overwrite existing output files"
        );

        // Parallel option
        var parallelOption = new Option<bool>(
            aliases: ["--parallel"],
            getDefaultValue: () => true,
            description: "Process files in parallel (default: true)"
        );

        // Max parallelism option
        var maxParallelismOption = new Option<int>(
            aliases: ["--max-parallel"],
            getDefaultValue: () => Environment.ProcessorCount,
            description: "Maximum number of parallel operations"
        );

        // Verbose option
        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output"
        );

        // Dry run option
        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run"],
            description: "Show what would be processed without actually doing it"
        );

        AddArgument(templateArgument);
        AddOption(modelDirOption);
        AddOption(outputDirOption);
        AddOption(modelPatternOption);
        AddOption(outputPatternOption);
        AddOption(recursiveOption);
        AddOption(forceOption);
        AddOption(parallelOption);
        AddOption(maxParallelismOption);
        AddOption(verboseOption);
        AddOption(dryRunOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var template = context.ParseResult.GetValueForArgument(templateArgument);
            var modelDir = context.ParseResult.GetValueForOption(modelDirOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption);
            var modelPattern = context.ParseResult.GetValueForOption(modelPatternOption);
            var outputPattern = context.ParseResult.GetValueForOption(outputPatternOption);
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var parallel = context.ParseResult.GetValueForOption(parallelOption);
            var maxParallel = context.ParseResult.GetValueForOption(maxParallelismOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            
            context.ExitCode = await ExecuteAsync(template, modelDir, outputDir, modelPattern, outputPattern, recursive, force, parallel, maxParallel, verbose, dryRun);
        });
    }

    private static async Task<int> ExecuteAsync(
        string? templatePath,
        string? modelDirectory,
        string? outputDirectory,
        string? modelPattern,
        string? outputPattern,
        bool recursive,
        bool force,
        bool parallel,
        int maxParallelism,
        bool verbose,
        bool dryRun)
    {
        try
        {
            // Load configuration file if available
            var config = await ConfigService.LoadConfigAsync();
            var configPath = ConfigService.FindConfigFile();
            
            // Create CLI options object
            var cliOptions = new BatchOptions
            {
                TemplatePath = templatePath,
                ModelDirectory = modelDirectory,
                OutputDirectory = outputDirectory,
                ModelGlobPattern = modelPattern,
                TemplateGlobPattern = "**/*.cshtml", // Not exposed in CLI but used in config
                OutputPattern = outputPattern,
                Recursive = recursive,
                Force = force,
                Verbose = verbose
            };

            // Merge with config file settings
            var options = ConfigService.MergeBatchOptions(cliOptions, config, configPath);

            if (options.Verbose)
            {
                if (config != null)
                    Console.WriteLine($"Using config file: {configPath}");
                else
                    Console.WriteLine("No config file found, using command line options only.");
            }

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

            // Validate required parameters (from CLI or config)
            if (string.IsNullOrEmpty(resolvedTemplatePath))
            {
                Console.Error.WriteLine("Error: Template path is required. Specify it as:");
                Console.Error.WriteLine("  - Command line argument: batch <template-path> <model-directory>");
                Console.Error.WriteLine("  - Config file: set templateDirectory and use: batch <template-filename> <model-directory>");
                return 1;
            }

            if (string.IsNullOrEmpty(options.ModelDirectory))
            {
                Console.Error.WriteLine("Error: Model directory is required. Specify it via:");
                Console.Error.WriteLine("  - Command line argument: batch <template> <model-directory>");
                Console.Error.WriteLine("  - Config file: set modelDirectory and use: batch <template>");
                return 1;
            }

            // Update options with resolved template path
            options.TemplatePath = resolvedTemplatePath;

            // Validate template file
            if (!File.Exists(options.TemplatePath!))
            {
                Console.Error.WriteLine($"Error: Template file not found: {options.TemplatePath}");
                return 1;
            }

            // Validate model directory
            if (!Directory.Exists(options.ModelDirectory!))
            {
                Console.Error.WriteLine($"Error: Model directory not found: {options.ModelDirectory}");
                return 1;
            }

            if (options.Verbose)
            {
                Console.WriteLine($"Template: {options.TemplatePath}");
                Console.WriteLine($"Model Directory: {options.ModelDirectory}");
                Console.WriteLine($"Output Directory: {options.OutputDirectory ?? "(same as model directory)"}");
                Console.WriteLine($"Model Pattern: {options.ModelGlobPattern}");
                Console.WriteLine($"Output Pattern: {options.OutputPattern}");
                Console.WriteLine($"Recursive: {options.Recursive}");
                Console.WriteLine($"Parallel: {parallel} (max: {maxParallelism})");
                Console.WriteLine($"Dry Run: {dryRun}");
                Console.WriteLine();
            }

            // Find model files
            var modelFiles = FindModelFiles(options.ModelDirectory!, options.ModelGlobPattern ?? "**/*.{json,yml,yaml,xml}", options.Recursive);

            if (modelFiles.Count == 0)
            {
                Console.WriteLine($"No model files found matching pattern: {options.ModelGlobPattern}");
                return 0;
            }

            Console.WriteLine($"Found {modelFiles.Count} model file(s) to process");

            // Generate output paths
            var processingPairs = OutputManager.GenerateBatchOutputPaths(
                modelFiles,
                options.TemplatePath!,
                options.OutputDirectory,
                options.OutputPattern! // Set by ConfigService.MergeBatchOptions
            );

            if (options.Verbose || dryRun)
            {
                Console.WriteLine("\nProcessing plan:");
                foreach (var pair in processingPairs)
                {
                    var modelDisplay = OutputManager.GetDisplayPath(pair.Key, options.ModelDirectory!);
                    var outputDisplay = OutputManager.GetDisplayPath(pair.Value);
                    Console.WriteLine($"  {modelDisplay} → {outputDisplay}");
                }
                Console.WriteLine();
            }

            if (dryRun)
            {
                Console.WriteLine("Dry run completed. No files were processed.");
                return 0;
            }

            // Check for existing files if force is not set
            if (!options.Force)
            {
                var existingFiles = processingPairs.Values.Where(File.Exists).ToList();
                if (existingFiles.Count > 0)
                {
                    Console.Error.WriteLine($"Error: {existingFiles.Count} output file(s) already exist:");
                    foreach (var file in existingFiles.Take(5)) // Show first 5
                    {
                        Console.Error.WriteLine($"  {OutputManager.GetDisplayPath(file)}");
                    }
                    if (existingFiles.Count > 5)
                    {
                        Console.Error.WriteLine($"  ... and {existingFiles.Count - 5} more");
                    }
                    Console.Error.WriteLine("Use --force to overwrite existing files.");
                    return 1;
                }
            }

            // Process files
            var startTime = DateTime.Now;
            var results = await ProcessFiles(
                processingPairs,
                options.TemplatePath!,
                parallel,
                maxParallelism,
                options.Force,
                options.Verbose
            );

            var duration = DateTime.Now - startTime;

            // Report results
            var successful = results.Count(r => r.Success);
            var failed = results.Count - successful;

            Console.WriteLine($"\nBatch processing completed in {duration.TotalSeconds:F1}s");
            Console.WriteLine($"✅ Successful: {successful}");
            
            if (failed > 0)
            {
                Console.WriteLine($"❌ Failed: {failed}");
                
                if (options.Verbose)
                {
                    Console.WriteLine("\nFailed files:");
                    foreach (var result in results.Where(r => !r.Success))
                    {
                        Console.WriteLine($"  {OutputManager.GetDisplayPath(result.ModelPath)}: {result.Error}");
                    }
                }
                
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            
            // Try to get verbose setting from config/options
            var verboseMode = false;
            try
            {
                var tempConfig = await ConfigService.LoadConfigAsync();
                var tempOptions = ConfigService.MergeBatchOptions(new BatchOptions { Verbose = verbose }, tempConfig);
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

    private static List<string> FindModelFiles(string directory, string pattern, bool recursive)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = new List<string>();

        // Handle glob patterns
        if (pattern.Contains("**") || pattern.Contains("*") || pattern.Contains("?"))
        {
            // For now, use a simple implementation
            // In a full implementation, you'd use a proper glob library
            var extensions = ModelLoader.GetSupportedExtensions();
            
            foreach (var ext in extensions)
            {
                var searchPattern = $"*{ext}";
                files.AddRange(Directory.GetFiles(directory, searchPattern, searchOption));
            }
        }
        else
        {
            // Direct pattern
            files.AddRange(Directory.GetFiles(directory, pattern, searchOption));
        }

        return files.Distinct().OrderBy(f => f).ToList();
    }

    private static async Task<List<BatchResult>> ProcessFiles(
        Dictionary<string, string> processingPairs,
        string templatePath,
        bool parallel,
        int maxParallelism,
        bool force,
        bool verbose)
    {
        var results = new List<BatchResult>();
        var engine = new TemplateEngine();

        if (parallel)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallelism
            };

            var concurrentResults = new System.Collections.Concurrent.ConcurrentBag<BatchResult>();

            await Parallel.ForEachAsync(processingPairs, parallelOptions, async (pair, cancellationToken) =>
            {
                var result = await ProcessSingleFile(engine, templatePath, pair.Key, pair.Value, force, verbose);
                concurrentResults.Add(result);
            });

            results.AddRange(concurrentResults);
        }
        else
        {
            foreach (var pair in processingPairs)
            {
                var result = await ProcessSingleFile(engine, templatePath, pair.Key, pair.Value, force, verbose);
                results.Add(result);
            }
        }

        return results.OrderBy(r => r.ModelPath).ToList();
    }

    private static async Task<BatchResult> ProcessSingleFile(
        TemplateEngine engine,
        string templatePath,
        string modelPath,
        string outputPath,
        bool force,
        bool verbose)
    {
        try
        {
            if (verbose)
            {
                var modelDisplay = OutputManager.GetDisplayPath(modelPath);
                Console.WriteLine($"Processing: {modelDisplay}");
            }

            // Load model
            var model = await ModelLoader.LoadFromFileAsync(modelPath);

            // Render template
            var result = await engine.LoadTemplate(templatePath)
                .WithModel(model)
                .RenderAsync();

            // Write output
            await OutputManager.WriteFileAsync(outputPath, result.Content, force);

            return new BatchResult
            {
                ModelPath = modelPath,
                OutputPath = outputPath,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new BatchResult
            {
                ModelPath = modelPath,
                OutputPath = outputPath,
                Success = false,
                Error = ex.Message
            };
        }
    }

    private record BatchResult
    {
        public string ModelPath { get; init; } = "";
        public string OutputPath { get; init; } = "";
        public bool Success { get; init; }
        public string? Error { get; init; }
    }
}