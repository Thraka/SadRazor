using System.Text.Json;
using SadRazor.Cli.Models;

namespace SadRazor.Cli.Services;

/// <summary>
/// Service for loading and resolving configuration files
/// </summary>
public class ConfigService
{
    private const string ConfigFileName = "sadrazor.json";
    
    /// <summary>
    /// Loads configuration from sadrazor.json file, searching from current directory upwards
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from (defaults to current directory)</param>
    /// <returns>Configuration object or null if no config file found</returns>
    public static async Task<ConfigFile?> LoadConfigAsync(string? startDirectory = null)
    {
        var configPath = FindConfigFile(startDirectory);
        if (configPath == null)
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            return JsonSerializer.Deserialize<ConfigFile>(json, options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load config file '{configPath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Finds the sadrazor.json config file by searching upwards from the specified directory
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from (defaults to current directory)</param>
    /// <returns>Full path to config file or null if not found</returns>
    public static string? FindConfigFile(string? startDirectory = null)
    {
        var directory = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());
        
        while (directory != null)
        {
            var configPath = Path.Combine(directory.FullName, ConfigFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }
            
            directory = directory.Parent;
        }
        
        return null;
    }



    /// <summary>
    /// Merges command-line options with configuration file settings for render command
    /// Command-line options take precedence over config file settings
    /// </summary>
    public static RenderOptions MergeRenderOptions(RenderOptions cliOptions, ConfigFile? config, string? configPath = null)
    {
        if (config == null)
        {
            return cliOptions;
        }

        var result = new RenderOptions
        {
            TemplatePath = cliOptions.TemplatePath,
            ModelPath = cliOptions.ModelPath,
            OutputPath = cliOptions.OutputPath,
            OutputDirectory = ResolveDirectory(cliOptions.OutputDirectory, config.OutputDirectory, configPath),
            ModelFormat = cliOptions.ModelFormat ?? config.DefaultModelFormat,
            Force = cliOptions.Force || (config.Settings?.Force ?? false),
            Verbose = cliOptions.Verbose || (config.Settings?.Verbose ?? false)
        };

        return result;
    }

    /// <summary>
    /// Merges command-line options with configuration file settings for batch command
    /// Command-line options take precedence over config file settings
    /// </summary>
    public static BatchOptions MergeBatchOptions(BatchOptions cliOptions, ConfigFile? config, string? configPath = null)
    {
        if (config == null)
        {
            return cliOptions;
        }

        var result = new BatchOptions
        {
            TemplatePath = cliOptions.TemplatePath,
            ModelPath = cliOptions.ModelPath,
            OutputPath = cliOptions.OutputPath,
            OutputDirectory = ResolveDirectory(cliOptions.OutputDirectory, config.OutputDirectory, configPath),
            ModelDirectory = ResolveDirectory(cliOptions.ModelDirectory, config.ModelDirectory, configPath),
            TemplateDirectory = ResolveDirectory(cliOptions.TemplateDirectory, config.TemplateDirectory, configPath),
            ModelGlobPattern = cliOptions.ModelGlobPattern ?? config.Batch?.ModelGlobPattern,
            TemplateGlobPattern = cliOptions.TemplateGlobPattern ?? config.Batch?.TemplateGlobPattern ?? "**/*.{json,yml,yaml,xml}",
            OutputPattern = cliOptions.OutputPattern ?? config.Batch?.OutputPattern ?? "{name}.md",
            Recursive = cliOptions.Recursive || (config.Batch?.Recursive ?? true),  // Use CLI if set, else config, else default true
            Force = cliOptions.Force || (config.Settings?.Force ?? false),
            Verbose = cliOptions.Verbose || (config.Settings?.Verbose ?? false)
        };

        return result;
    }

    /// <summary>
    /// Resolves template path for batch command - can use template directory from config
    /// </summary>
    public static string? ResolveTemplatePath(string? cliTemplatePath, ConfigFile? config, string? configPath = null)
    {
        // CLI template path takes precedence
        if (!string.IsNullOrEmpty(cliTemplatePath))
        {
            return cliTemplatePath;
        }

        // If config has a template directory, we can't auto-resolve without knowing the specific template name
        // This would need to be specified by the user or we'd need a default template name in config
        return null;
    }

    /// <summary>
    /// Merges command-line options with configuration file settings for validate command
    /// Command-line options take precedence over config file settings
    /// </summary>
    public static ValidateOptions MergeValidateOptions(ValidateOptions cliOptions, ConfigFile? config, string? configPath = null)
    {
        if (config == null)
        {
            return cliOptions;
        }

        var result = new ValidateOptions
        {
            TemplatePath = cliOptions.TemplatePath,
            ModelPath = cliOptions.ModelPath,
            OutputPath = cliOptions.OutputPath,
            OutputDirectory = ResolveDirectory(cliOptions.OutputDirectory, config.OutputDirectory, configPath),
            CheckSyntax = cliOptions.CheckSyntax,
            CheckModel = cliOptions.CheckModel,
            CheckOutput = cliOptions.CheckOutput,
            Force = cliOptions.Force || (config.Settings?.Force ?? false),
            Verbose = cliOptions.Verbose || (config.Settings?.Verbose ?? false)
        };

        return result;
    }

    private static string? ResolveDirectory(string? cliDir, string? configDir, string? configPath)
    {
        // CLI option takes precedence
        if (!string.IsNullOrEmpty(cliDir))
        {
            return cliDir;
        }

        // Use config directory if specified
        if (!string.IsNullOrEmpty(configDir))
        {
            return ResolvePath(configDir, configPath);
        }

        return null;
    }

    private static string ResolvePath(string relativePath, string? configPath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        configPath ??= FindConfigFile();
        if (configPath == null)
        {
            return Path.GetFullPath(relativePath);
        }

        var configDir = Path.GetDirectoryName(configPath)!;
        return Path.GetFullPath(Path.Combine(configDir, relativePath));
    }


}