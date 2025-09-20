using System.CommandLine;
using FluentAssertions;
using SadRazor.Cli.Commands;
using Xunit;

namespace SadRazor.Cli.Tests.Commands;

public class ValidateCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public ValidateCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "SadRazorValidateCommandTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateCommand_WithValidTemplateAndModel_ShouldReturnSuccess()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "valid-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title

@Model.Description
""");

        var modelPath = Path.Combine(_tempDirectory, "valid-model.json");
        File.WriteAllText(modelPath, """
{
  "Title": "Test Document",
  "Description": "Test description"
}
""");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model", modelPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ValidateCommand_WithNonExistentTemplate_ShouldReturnError()
    {
        // Arrange
        var command = new ValidateCommand();
        var nonExistentTemplate = Path.Combine(_tempDirectory, "nonexistent.cshtml");
        var args = new[] { nonExistentTemplate };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ValidateCommand_WithInvalidTemplateSyntax_ShouldReturnError()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "invalid-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title

@{ // Unclosed code block
   var test = "incomplete
""");

        var command = new ValidateCommand();
        var args = new[] { templatePath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ValidateCommand_WithInvalidJsonModel_ShouldReturnError()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "valid-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title
""");

        var modelPath = Path.Combine(_tempDirectory, "invalid-model.json");
        File.WriteAllText(modelPath, """{"title": "test", "invalid":}""");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model", modelPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task ValidateCommand_WithValidYamlModel_ShouldReturnSuccess()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "valid-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title

@Model.Description
""");

        var modelPath = Path.Combine(_tempDirectory, "valid-model.yml");
        File.WriteAllText(modelPath, """
Title: YAML Test Document
Description: This is a valid YAML model
""");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model", modelPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ValidateCommand_WithOutputCheck_ShouldValidateGeneration()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "output-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title

@if (Model.Description != null) {
@Model.Description
}
""");

        var modelPath = Path.Combine(_tempDirectory, "output-model.json");
        File.WriteAllText(modelPath, """
{
  "Title": "Test Document",
  "Description": "Test description"
}
""");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model", modelPath, "--output-check" };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ValidateCommand_WithSyntaxOnlyCheck_ShouldSkipModelValidation()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "syntax-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# Valid Template

This template has valid syntax.
""");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model-check", "false" };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ValidateCommand_WithUnsupportedModelFormat_ShouldReturnError()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "valid-template.cshtml");
        File.WriteAllText(templatePath, """
@model dynamic

# @Model.Title
""");

        var modelPath = Path.Combine(_tempDirectory, "unsupported-model.txt");
        File.WriteAllText(modelPath, "This is not a supported format");

        var command = new ValidateCommand();
        var args = new[] { templatePath, "--model", modelPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }
}