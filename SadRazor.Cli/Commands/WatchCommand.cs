using System.CommandLine;
using System.CommandLine.Invocation;
using SadRazor.Cli.Models;
using SadRazor.Cli.Services;
using SadRazorEngine;
using SadRazorEngine.Extensions;

namespace SadRazor.Cli.Commands;

/// <summary>
/// Command for watching files and auto-regenerating output
/// </summary>
public class WatchCommand : Command
{
    public WatchCommand() : base("watch", "Watch for file changes and automatically regenerate output")
    {
        // Template argument (required)
        var templateArgument = new Argument<string>(
            name: "template",
            description: "Path to the Razor template file (.cshtml) to watch"
        );

        // Watch paths argument
        var watchPathsArgument = new Argument<string[]>(
            name: "watch-paths",
            description: "Directories to watch for changes (defaults to template and model directories)"
        );

        // Model option
        var modelOption = new Option<string?>(
            aliases: ["--model", "-m"],
            description: "Path to the model file (for single model watch)"
        );

        // Output option
        var outputOption = new Option<string?>(
            aliases: ["--output", "-o"],
            description: "Output file path (for single model watch)"
        );

        // Output directory option
        var outputDirOption = new Option<string?>(
            aliases: ["--output-dir", "-d"],
            description: "Output directory for batch processing"
        );

        // Model directory option
        var modelDirOption = new Option<string?>(
            aliases: ["--model-dir"],
            description: "Model directory to watch for batch processing"
        );

        // Batch mode option
        var batchOption = new Option<bool>(
            aliases: ["--batch", "-b"],
            description: "Enable batch mode (watch model directory and process all files)"
        );

        // Exclude patterns option
        var excludeOption = new Option<string[]>(
            aliases: ["--exclude", "-x"],
            getDefaultValue: () => new[] { "**/node_modules/**", "**/bin/**", "**/obj/**", "**/.git/**" },
            description: "Patterns to exclude from watching"
        );

        // Debounce option
        var debounceOption = new Option<int>(
            aliases: ["--debounce"],
            getDefaultValue: () => 500,
            description: "Debounce time in milliseconds (default: 500)"
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

        // Initial render option
        var initialRenderOption = new Option<bool>(
            aliases: ["--initial"],
            getDefaultValue: () => true,
            description: "Perform initial render before watching (default: true)"
        );

        AddArgument(templateArgument);
        AddArgument(watchPathsArgument);
        AddOption(modelOption);
        AddOption(outputOption);
        AddOption(outputDirOption);
        AddOption(modelDirOption);
        AddOption(batchOption);
        AddOption(excludeOption);
        AddOption(debounceOption);
        AddOption(forceOption);
        AddOption(verboseOption);
        AddOption(initialRenderOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var template = context.ParseResult.GetValueForArgument(templateArgument);
            var watchPaths = context.ParseResult.GetValueForArgument(watchPathsArgument);
            var model = context.ParseResult.GetValueForOption(modelOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption);
            var modelDir = context.ParseResult.GetValueForOption(modelDirOption);
            var batch = context.ParseResult.GetValueForOption(batchOption);
            var exclude = context.ParseResult.GetValueForOption(excludeOption) ?? [];
            var debounce = context.ParseResult.GetValueForOption(debounceOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var initial = context.ParseResult.GetValueForOption(initialRenderOption);
            
            context.ExitCode = await ExecuteAsync(template, watchPaths, model, output, outputDir, modelDir, batch, exclude, debounce, force, verbose, initial);
        });
    }

    private static async Task<int> ExecuteAsync(
        string templatePath,
        string[] watchPaths,
        string? modelPath,
        string? outputPath,
        string? outputDirectory,
        string? modelDirectory,
        bool batchMode,
        string[] excludePatterns,
        int debounceMs,
        bool force,
        bool verbose,
        bool initialRender)
    {
        try
        {
            // Load configuration file if available
            var config = await ConfigService.LoadConfigAsync();
            var configPath = ConfigService.FindConfigFile();
            
            // Create CLI options object
            var cliOptions = new WatchOptions
            {
                TemplatePath = templatePath,
                ModelPath = modelPath,
                OutputPath = outputPath,
                OutputDirectory = outputDirectory,
                WatchPaths = watchPaths,
                ExcludePatterns = excludePatterns,
                DebounceMs = debounceMs,
                Force = force,
                Verbose = verbose
            };

            // Merge with config file settings
            var options = ConfigService.MergeWatchOptions(cliOptions, config, configPath);

            if (options.Verbose)
            {
                if (config != null)
                    Console.WriteLine($"Using config file: {configPath}");
                else
                    Console.WriteLine("No config file found, using command line options only.");
            }
            // Validate template
            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine($"Error: Template file not found: {templatePath}");
                return 1;
            }

            // Determine watch paths if not provided
            if (watchPaths.Length == 0)
            {
                var paths = new List<string>();
                
                // Add template directory
                var templateDir = Path.GetDirectoryName(templatePath);
                if (!string.IsNullOrEmpty(templateDir))
                    paths.Add(templateDir);

                // Add model directory or model file directory
                if (batchMode && !string.IsNullOrEmpty(modelDirectory))
                {
                    paths.Add(modelDirectory);
                }
                else if (!string.IsNullOrEmpty(modelPath))
                {
                    var modelDir = Path.GetDirectoryName(modelPath);
                    if (!string.IsNullOrEmpty(modelDir))
                        paths.Add(modelDir);
                }

                watchPaths = paths.Distinct().ToArray();
            }

            if (verbose)
            {
                Console.WriteLine($"Template: {templatePath}");
                Console.WriteLine($"Watch Paths: {string.Join(", ", watchPaths)}");
                Console.WriteLine($"Batch Mode: {batchMode}");
                Console.WriteLine($"Debounce: {debounceMs}ms");
                Console.WriteLine($"Exclude Patterns: {string.Join(", ", excludePatterns)}");
                Console.WriteLine();
            }

            // Validate directories
            foreach (var path in watchPaths)
            {
                if (!Directory.Exists(path))
                {
                    Console.Error.WriteLine($"Error: Watch directory not found: {path}");
                    return 1;
                }
            }

            var engine = new TemplateEngine(enableCaching: true);
            var processingQueue = new HashSet<string>();
            var queueLock = new object();

            // Perform initial render if requested
            if (initialRender)
            {
                Console.WriteLine("Performing initial render...");
                await PerformRender(engine, templatePath, modelPath, outputPath, outputDirectory, 
                    modelDirectory, batchMode, force, verbose);
                Console.WriteLine("Initial render complete.");
                Console.WriteLine();
            }

            // Set up file watcher
            using var watcher = new FileWatcher(debounceMs, excludePatterns);
            
            watcher.FileChanged += async (sender, e) =>
            {
                lock (queueLock)
                {
                    if (processingQueue.Contains(e.FilePath))
                        return;
                    processingQueue.Add(e.FilePath);
                }

                try
                {
                    if (verbose)
                        Console.WriteLine($"File changed: {OutputManager.GetDisplayPath(e.FilePath)} ({e.ChangeType})");

                    // Determine what to regenerate based on the changed file
                    if (e.FileType == FileType.Template || Path.GetFullPath(e.FilePath) == Path.GetFullPath(templatePath))
                    {
                        // Template changed - regenerate everything
                        Console.WriteLine("Template changed, regenerating all output...");
                        await PerformRender(engine, templatePath, modelPath, outputPath, outputDirectory,
                            modelDirectory, batchMode, force, verbose);
                    }
                    else if (e.FileType == FileType.Model)
                    {
                        // Model changed - regenerate specific output
                        if (batchMode)
                        {
                            Console.WriteLine($"Model changed: {OutputManager.GetDisplayPath(e.FilePath)}");
                            await ProcessSingleModel(engine, templatePath, e.FilePath, outputDirectory, force, verbose);
                        }
                        else if (!string.IsNullOrEmpty(modelPath) && Path.GetFullPath(e.FilePath) == Path.GetFullPath(modelPath))
                        {
                            Console.WriteLine("Model changed, regenerating output...");
                            await PerformRender(engine, templatePath, modelPath, outputPath, outputDirectory,
                                modelDirectory, batchMode, force, verbose);
                        }
                    }

                    Console.WriteLine("✅ Regeneration complete");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error during regeneration: {ex.Message}");
                    if (verbose)
                        Console.Error.WriteLine(ex.ToString());
                }
                finally
                {
                    lock (queueLock)
                    {
                        processingQueue.Remove(e.FilePath);
                    }
                }
            };

            // Start watching
            watcher.StartWatching(watchPaths, recursive: true);

            Console.WriteLine($"👀 Watching for changes in: {string.Join(", ", watchPaths.Select(p => OutputManager.GetDisplayPath(p)))}");
            Console.WriteLine("Press Ctrl+C to stop watching...");
            Console.WriteLine();

            // Wait for cancellation
            var cancellationToken = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationToken.Cancel();
            };

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("Watch cancelled.");
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

    private static async Task PerformRender(
        TemplateEngine engine,
        string templatePath,
        string? modelPath,
        string? outputPath,
        string? outputDirectory,
        string? modelDirectory,
        bool batchMode,
        bool force,
        bool verbose)
    {
        if (batchMode && !string.IsNullOrEmpty(modelDirectory))
        {
            // Batch processing
            var modelFiles = Directory.GetFiles(modelDirectory, "*.json", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(modelDirectory, "*.yml", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(modelDirectory, "*.yaml", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(modelDirectory, "*.xml", SearchOption.AllDirectories))
                .ToList();

            if (verbose)
                Console.WriteLine($"Processing {modelFiles.Count} model files...");

            foreach (var modelFile in modelFiles)
            {
                await ProcessSingleModel(engine, templatePath, modelFile, outputDirectory, force, verbose);
            }
        }
        else if (!string.IsNullOrEmpty(modelPath))
        {
            // Single model processing
            await ProcessSingleModel(engine, templatePath, modelPath, outputDirectory, force, verbose, outputPath);
        }
        else
        {
            // No model processing
            var result = await engine.LoadTemplate(templatePath)
                .RenderAsync();

            var finalOutputPath = outputPath ?? Path.ChangeExtension(templatePath, ".md");
            await OutputManager.WriteFileAsync(finalOutputPath, result.Content, force);

            if (verbose)
                Console.WriteLine($"Generated: {OutputManager.GetDisplayPath(finalOutputPath)}");
        }
    }

    private static async Task ProcessSingleModel(
        TemplateEngine engine,
        string templatePath,
        string modelPath,
        string? outputDirectory,
        bool force,
        bool verbose,
        string? explicitOutputPath = null)
    {
        try
        {
            var context = engine.LoadTemplate(templatePath);
            context = await context.WithModelFromFileAsync(modelPath);
            
            var result = await context.RenderAsync();

            var outputPath = explicitOutputPath ?? OutputManager.GenerateOutputPath(templatePath, modelPath, null, outputDirectory);
            await OutputManager.WriteFileAsync(outputPath, result.Content, force);

            if (verbose)
                Console.WriteLine($"Generated: {OutputManager.GetDisplayPath(outputPath)}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing {OutputManager.GetDisplayPath(modelPath)}: {ex.Message}");
        }
    }
}