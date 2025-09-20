namespace SadRazorEngine.Core.Models;

/// <summary>
/// Represents the result of template validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors found
    /// </summary>
    public List<ValidationError> Errors { get; init; } = new();

    /// <summary>
    /// List of validation warnings found
    /// </summary>
    public List<ValidationWarning> Warnings { get; init; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };

    /// <summary>
    /// Creates a validation result with both errors and warnings
    /// </summary>
    public static ValidationResult Create(List<ValidationError> errors, List<ValidationWarning> warnings) => new()
    {
        IsValid = errors.Count == 0,
        Errors = errors,
        Warnings = warnings
    };
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The line number where the error occurred (if applicable)
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// The column number where the error occurred (if applicable)
    /// </summary>
    public int? ColumnNumber { get; init; }

    /// <summary>
    /// The error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The code that caused the error (if applicable)
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// The type of validation error
    /// </summary>
    public ValidationErrorType Type { get; init; } = ValidationErrorType.ModelAccess;

    public override string ToString()
    {
        var location = LineNumber.HasValue ? $"Line {LineNumber}" : "";
        if (ColumnNumber.HasValue)
            location += $", Column {ColumnNumber}";
        
        return string.IsNullOrEmpty(location) ? Message : $"{location}: {Message}";
    }
}

/// <summary>
/// Represents a validation warning
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// The line number where the warning occurred (if applicable)
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// The warning message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The code that caused the warning (if applicable)
    /// </summary>
    public string? Code { get; init; }

    public override string ToString()
    {
        var location = LineNumber.HasValue ? $"Line {LineNumber}" : "";
        return string.IsNullOrEmpty(location) ? Message : $"{location}: {Message}";
    }
}

/// <summary>
/// Types of validation errors
/// </summary>
public enum ValidationErrorType
{
    /// <summary>
    /// Error accessing model properties
    /// </summary>
    ModelAccess,
    
    /// <summary>
    /// Syntax error in template
    /// </summary>
    Syntax,
    
    /// <summary>
    /// Missing required model property
    /// </summary>
    MissingProperty,
    
    /// <summary>
    /// Type mismatch
    /// </summary>
    TypeMismatch,
    
    /// <summary>
    /// Partial template not found
    /// </summary>
    PartialNotFound
}