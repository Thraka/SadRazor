using System.Text.Json.Serialization;

namespace SadRazor.Cli.Models;

/// <summary>
/// Configuration file model for SadRazor CLI
/// </summary>
public class ConfigFile
{
    /// <summary>
    /// Default template directory
    /// </summary>
    public string? TemplateDirectory { get; set; }

    /// <summary>
    /// Default model directory
    /// </summary>
    public string? ModelDirectory { get; set; }

    /// <summary>
    /// Default output directory
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Default model format (json, yaml, xml, auto)
    /// </summary>
    public string? DefaultModelFormat { get; set; } = "auto";



    /// <summary>
    /// Batch processing configuration
    /// </summary>
    public BatchConfig? Batch { get; set; }

    /// <summary>
    /// Global settings
    /// </summary>
    public GlobalSettings? Settings { get; set; }
}

/// <summary>
/// Batch processing configuration
/// </summary>
public class BatchConfig
{
    /// <summary>
    /// Default glob pattern for model files
    /// </summary>
    public string? ModelGlobPattern { get; set; } = "**/*.{json,yml,yaml,xml}";

    /// <summary>
    /// Default glob pattern for template files
    /// </summary>
    public string? TemplateGlobPattern { get; set; } = "**/*.cshtml";

    /// <summary>
    /// Whether to process subdirectories recursively
    /// </summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Output filename pattern (supports placeholders like {name}, {ext})
    /// </summary>
    public string? OutputPattern { get; set; } = "{name}.md";
}

/// <summary>
/// Global settings
/// </summary>
public class GlobalSettings
{
    /// <summary>
    /// Whether to enable verbose logging by default
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Whether to force overwrite existing files by default
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Default file encoding
    /// </summary>
    public string? Encoding { get; set; } = "utf-8";

    /// <summary>
    /// Template cache settings
    /// </summary>
    public CacheSettings? Cache { get; set; }
}

/// <summary>
/// Template cache configuration
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Whether to enable template caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache directory path
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Maximum cache age in minutes
    /// </summary>
    public int MaxAgeMinutes { get; set; } = 60;
}