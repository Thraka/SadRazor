namespace SadRazor.Cli.Models;

/// <summary>
/// Common CLI options shared across commands
/// </summary>
public class CliOptions
{
    public string? TemplatePath { get; set; }
    public string? ModelPath { get; set; }
    public string? OutputPath { get; set; }
    public string? OutputDirectory { get; set; }
    public bool Verbose { get; set; }
    public bool Force { get; set; }
}

/// <summary>
/// Options specific to the render command
/// </summary>
public class RenderOptions : CliOptions
{
    public string? ModelFormat { get; set; } // json, yaml, xml, auto
    public bool Watch { get; set; }
}

/// <summary>
/// Options for batch processing
/// </summary>
public class BatchOptions : CliOptions
{
    public string? ModelGlobPattern { get; set; }
    public string? TemplateGlobPattern { get; set; }
    public bool Recursive { get; set; }
    public string? ModelDirectory { get; set; }
    public string? TemplateDirectory { get; set; }
}

/// <summary>
/// Options for watch command
/// </summary>
public class WatchOptions : CliOptions
{
    public string[]? WatchPaths { get; set; }
    public string[]? ExcludePatterns { get; set; }
    public int DebounceMs { get; set; } = 500;
}

/// <summary>
/// Options for validation command
/// </summary>
public class ValidateOptions : CliOptions
{
    public bool CheckSyntax { get; set; } = true;
    public bool CheckModel { get; set; } = true;
    public bool CheckOutput { get; set; } = false;
}