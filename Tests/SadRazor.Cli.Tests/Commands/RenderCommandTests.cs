using FluentAssertions;
using SadRazor.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace SadRazor.Cli.Tests.Commands;

public class RenderCommandTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _templatePath;
    private readonly string _modelPath;

    public RenderCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "SadRazorRenderCommandTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        // Create test template
        _templatePath = Path.Combine(_tempDirectory, "test-template.cshtml");
        File.WriteAllText(_templatePath, """
@model dynamic

# @Model.Title

@Model.Description

## Author: @Model.Author
""");

        // Create test model
        _modelPath = Path.Combine(_tempDirectory, "test-model.json");
        File.WriteAllText(_modelPath, """
{
  "Title": "Test Document",
  "Description": "This is a test document for CLI testing.",
  "Author": "Test Author"
}
""");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RenderCommand_WithValidTemplateAndModel_ShouldGenerateOutput()
    {
        // Arrange
        var command = new RenderCommand();
        var outputPath = Path.Combine(_tempDirectory, "output.md");
        var args = new[] { _templatePath, "--model", _modelPath, "--output", outputPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("# Test Document");
        content.Should().Contain("This is a test document for CLI testing.");
        content.Should().Contain("## Author: Test Author");
    }

    [Fact]
    public async Task RenderCommand_WithNonExistentTemplate_ShouldReturnError()
    {
        // Arrange
        var command = new RenderCommand();
        var nonExistentTemplate = Path.Combine(_tempDirectory, "nonexistent.cshtml");
        var args = new[] { nonExistentTemplate };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task RenderCommand_WithNonExistentModel_ShouldReturnError()
    {
        // Arrange
        var command = new RenderCommand();
        var nonExistentModel = Path.Combine(_tempDirectory, "nonexistent.json");
        var args = new[] { _templatePath, "--model", nonExistentModel };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task RenderCommand_WithExistingOutputAndNoForce_ShouldReturnError()
    {
        // Arrange
        var command = new RenderCommand();
        var outputPath = Path.Combine(_tempDirectory, "existing-output.md");
        File.WriteAllText(outputPath, "existing content");

        var args = new[] { _templatePath, "--model", _modelPath, "--output", outputPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Be("existing content"); // Should not be overwritten
    }

    [Fact]
    public async Task RenderCommand_WithExistingOutputAndForce_ShouldOverwrite()
    {
        // Arrange
        var command = new RenderCommand();
        var outputPath = Path.Combine(_tempDirectory, "existing-output.md");
        File.WriteAllText(outputPath, "existing content");

        var args = new[] { _templatePath, "--model", _modelPath, "--output", outputPath, "--force" };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("# Test Document");
    }

    [Fact]
    public async Task RenderCommand_WithoutModel_ShouldRenderWithoutModel()
    {
        // Arrange
        var templateWithoutModel = Path.Combine(_tempDirectory, "no-model-template.cshtml");
        File.WriteAllText(templateWithoutModel, """
# Static Document

This template doesn't use a model.

## Static Content

Some static markdown content.
""");

        var command = new RenderCommand();
        var outputPath = Path.Combine(_tempDirectory, "no-model-output.md");
        var args = new[] { templateWithoutModel, "--output", outputPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("# Static Document");
        content.Should().Contain("This template doesn't use a model.");
    }

    [Fact]
    public async Task RenderCommand_WithYamlModel_ShouldProcessCorrectly()
    {
        // Arrange
        var yamlModelPath = Path.Combine(_tempDirectory, "test-model.yml");
        File.WriteAllText(yamlModelPath, """
Title: YAML Test Document
Description: This is a YAML test document.
Author: YAML Author
""");

        var command = new RenderCommand();
        var outputPath = Path.Combine(_tempDirectory, "yaml-output.md");
        var args = new[] { _templatePath, "--model", yamlModelPath, "--output", outputPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("# YAML Test Document");
        content.Should().Contain("This is a YAML test document.");
        content.Should().Contain("## Author: YAML Author");
    }

    [Fact]
    public async Task RenderCommand_WithInvalidJson_ShouldReturnError()
    {
        // Arrange
        var invalidJsonPath = Path.Combine(_tempDirectory, "invalid.json");
        File.WriteAllText(invalidJsonPath, """{"title": "test", "invalid":}""");

        var command = new RenderCommand();
        var args = new[] { _templatePath, "--model", invalidJsonPath };

        // Act
        var result = await command.InvokeAsync(args);

        // Assert
        result.Should().Be(1);
    }
}