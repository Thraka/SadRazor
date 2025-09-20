using FluentAssertions;
using SadRazor.Cli.Services;
using Xunit;

namespace SadRazor.Cli.Tests.Services;

public class OutputManagerTests : IDisposable
{
    private readonly string _tempDirectory;

    public OutputManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "SadRazorCliTests", Guid.NewGuid().ToString());
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
    public async Task WriteFileAsync_WithNewFile_ShouldCreateFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.md");
        var content = "# Test Content\n\nThis is a test.";

        // Act
        await OutputManager.WriteFileAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task WriteFileAsync_WithExistingFileAndForce_ShouldOverwriteFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "existing.md");
        await File.WriteAllTextAsync(filePath, "Original content");
        var newContent = "# New Content\n\nThis is new.";

        // Act
        await OutputManager.WriteFileAsync(filePath, newContent, force: true);

        // Assert
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(newContent);
    }

    [Fact]
    public async Task WriteFileAsync_WithExistingFileAndNoForce_ShouldThrowException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "existing.md");
        await File.WriteAllTextAsync(filePath, "Original content");
        var newContent = "# New Content\n\nThis is new.";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => OutputManager.WriteFileAsync(filePath, newContent, force: false));
    }

    [Fact]
    public async Task WriteFileAsync_WithNonExistentDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "subdir", "nested", "file.md");
        var content = "# Test Content";

        // Act
        await OutputManager.WriteFileAsync(filePath, content);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);
    }

    [Theory]
    [InlineData("template.cshtml", "model.json", null, null, "model.md")]
    [InlineData("template.cshtml", null, null, null, "template.md")]
    [InlineData("template.cshtml", "model.json", "output.md", null, "output.md")]
    [InlineData("template.cshtml", "model.json", null, "output-dir", "output-dir/model.md")]
    public void GenerateOutputPath_WithVariousInputs_ShouldGenerateCorrectPath(
        string templatePath, string? modelPath, string? outputPath, string? outputDirectory, string expectedFileName)
    {
        // Arrange
        var fullTemplatePath = Path.Combine(_tempDirectory, templatePath);
        var fullModelPath = string.IsNullOrEmpty(modelPath) ? null : Path.Combine(_tempDirectory, modelPath);
        var fullOutputDirectory = string.IsNullOrEmpty(outputDirectory) ? null : Path.Combine(_tempDirectory, outputDirectory);

        // Act
        var result = OutputManager.GenerateOutputPath(fullTemplatePath, fullModelPath, outputPath, fullOutputDirectory);

        // Assert
        var expectedPath = Path.IsPathRooted(expectedFileName) 
            ? expectedFileName 
            : Path.Combine(_tempDirectory, expectedFileName);
        
        Path.GetFullPath(result).Should().Be(Path.GetFullPath(expectedPath));
    }

    [Fact]
    public void GenerateBatchOutputPaths_WithMultipleModels_ShouldGenerateCorrectPaths()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "template.cshtml");
        var modelPaths = new[]
        {
            Path.Combine(_tempDirectory, "model1.json"),
            Path.Combine(_tempDirectory, "model2.json"),
            Path.Combine(_tempDirectory, "subdir", "model3.json")
        };
        var outputDirectory = Path.Combine(_tempDirectory, "output");

        // Act
        var result = OutputManager.GenerateBatchOutputPaths(modelPaths, templatePath, outputDirectory);

        // Assert
        result.Should().HaveCount(3);
        result[modelPaths[0]].Should().EndWith("output/model1.md");
        result[modelPaths[1]].Should().EndWith("output/model2.md");
        result[modelPaths[2]].Should().EndWith("output/model3.md");
    }

    [Theory]
    [InlineData("test.md", false, false)]
    [InlineData("nonexistent.md", false, true)]
    [InlineData("existing.md", true, true)]
    public void CanWriteFile_WithVariousConditions_ShouldReturnCorrectResult(
        string fileName, bool createFile, bool expectedResult)
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, fileName);
        if (createFile)
        {
            File.WriteAllText(filePath, "test content");
        }

        // Act
        var result = OutputManager.CanWriteFile(filePath, force: true);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetDisplayPath_WithAbsolutePath_ShouldReturnRelativePath()
    {
        // Arrange
        var basePath = _tempDirectory;
        var filePath = Path.Combine(_tempDirectory, "subdir", "file.md");

        // Act
        var result = OutputManager.GetDisplayPath(filePath, basePath);

        // Assert
        result.Should().Be(Path.Combine("subdir", "file.md"));
    }

    [Fact]
    public void EnsureDirectoryExists_WithNonExistentDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "new", "nested", "file.md");

        // Act
        OutputManager.EnsureDirectoryExists(filePath);

        // Assert
        Directory.Exists(Path.GetDirectoryName(filePath)).Should().BeTrue();
    }

    [Theory]
    [InlineData("valid-filename.md", "valid-filename.md")]
    [InlineData("file<with>invalid:chars.md", "file_with_invalid_chars.md")]
    [InlineData("file|with|pipes.md", "file_with_pipes.md")]
    public void GetSafeFileName_WithVariousInputs_ShouldReturnSafeFileName(string input, string expected)
    {
        // Act
        var result = OutputManager.GetSafeFileName(input);

        // Assert
        result.Should().Be(expected);
    }
}