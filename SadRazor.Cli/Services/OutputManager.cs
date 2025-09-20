using System.Text;

namespace SadRazor.Cli.Services;

/// <summary>
/// Service for managing output file operations
/// </summary>
public class OutputManager
{
    /// <summary>
    /// Write content to a file, creating directories as needed
    /// </summary>
    public static async Task WriteFileAsync(string filePath, string content, bool force = false, Encoding? encoding = null)
    {
        // Check if file exists and force is not set
        if (File.Exists(filePath) && !force)
        {
            throw new InvalidOperationException($"Output file already exists: {filePath}. Use --force to overwrite.");
        }

        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Use UTF-8 by default
        encoding ??= Encoding.UTF8;

        // Write file
        await File.WriteAllTextAsync(filePath, content, encoding);
    }

    /// <summary>
    /// Generate output file path based on template and model paths
    /// </summary>
    public static string GenerateOutputPath(string templatePath, string? modelPath, string? outputPath, string? outputDirectory)
    {
        // If explicit output path is provided, use it
        if (!string.IsNullOrEmpty(outputPath))
        {
            return Path.GetFullPath(outputPath);
        }

        // Determine base name for output file
        string baseName;
        if (!string.IsNullOrEmpty(modelPath))
        {
            baseName = Path.GetFileNameWithoutExtension(modelPath);
        }
        else
        {
            baseName = Path.GetFileNameWithoutExtension(templatePath);
        }

        // Determine output directory
        string outputDir;
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            outputDir = outputDirectory;
        }
        else if (!string.IsNullOrEmpty(modelPath))
        {
            outputDir = Path.GetDirectoryName(modelPath) ?? Environment.CurrentDirectory;
        }
        else
        {
            outputDir = Path.GetDirectoryName(templatePath) ?? Environment.CurrentDirectory;
        }

        // Generate output file path with .md extension
        return Path.Combine(outputDir, $"{baseName}.md");
    }

    /// <summary>
    /// Generate multiple output paths for batch processing
    /// </summary>
    public static Dictionary<string, string> GenerateBatchOutputPaths(
        IEnumerable<string> modelPaths,
        string templatePath,
        string? outputDirectory,
        string outputPattern = "{name}.md"
    )
    {
        var result = new Dictionary<string, string>();
        
        foreach (var modelPath in modelPaths)
        {
            var modelName = Path.GetFileNameWithoutExtension(modelPath);
            var modelDir = Path.GetDirectoryName(modelPath) ?? "";
            
            // Replace placeholders in output pattern
            var fileName = outputPattern
                .Replace("{name}", modelName)
                .Replace("{ext}", ".md")
                .Replace("{dir}", Path.GetFileName(modelDir));

            // Determine output directory
            string outputDir;
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                outputDir = outputDirectory;
            }
            else
            {
                outputDir = modelDir;
            }

            var outputPath = Path.Combine(outputDir, fileName);
            result[modelPath] = Path.GetFullPath(outputPath);
        }

        return result;
    }

    /// <summary>
    /// Check if file can be written (doesn't exist or force is enabled)
    /// </summary>
    public static bool CanWriteFile(string filePath, bool force)
    {
        return !File.Exists(filePath) || force;
    }

    /// <summary>
    /// Get relative path for display purposes
    /// </summary>
    public static string GetDisplayPath(string filePath, string? basePath = null)
    {
        basePath ??= Environment.CurrentDirectory;
        
        try
        {
            return Path.GetRelativePath(basePath, filePath);
        }
        catch
        {
            return filePath;
        }
    }

    /// <summary>
    /// Ensure directory exists for the given file path
    /// </summary>
    public static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Get safe filename by removing invalid characters
    /// </summary>
    public static string GetSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new StringBuilder(fileName);
        
        foreach (var invalidChar in invalidChars)
        {
            safeName.Replace(invalidChar, '_');
        }
        
        return safeName.ToString();
    }

    /// <summary>
    /// Copy file with optional overwrite protection
    /// </summary>
    public static async Task CopyFileAsync(string sourcePath, string destinationPath, bool force = false)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        if (File.Exists(destinationPath) && !force)
        {
            throw new InvalidOperationException($"Destination file already exists: {destinationPath}. Use --force to overwrite.");
        }

        EnsureDirectoryExists(destinationPath);
        
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        
        await sourceStream.CopyToAsync(destinationStream);
    }
}