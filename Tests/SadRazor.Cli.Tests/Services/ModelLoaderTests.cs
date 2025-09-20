using FluentAssertions;
using SadRazor.Cli.Services;
using Xunit;

namespace SadRazor.Cli.Tests.Services;

public class ModelLoaderTests
{
    private readonly string _testDataPath;

    public ModelLoaderTests()
    {
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
    }

    [Fact]
    public async Task LoadFromFileAsync_WithValidJsonFile_ShouldLoadModel()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDataPath, "simple-model.json");

        // Act
        var model = await ModelLoader.LoadFromFileAsync(jsonPath);

        // Assert
        model.Should().NotBeNull();
        var dict = model as IDictionary<string, object>;
        dict.Should().NotBeNull();
        dict!["Title"].ToString().Should().Be("Test Document");
        dict["Description"].ToString().Should().Be("This is a test document for SadRazor CLI testing.");
        dict["Author"].ToString().Should().Be("Test Author");
    }

    [Fact]
    public async Task LoadFromFileAsync_WithValidYamlFile_ShouldLoadModel()
    {
        // Arrange
        var yamlPath = Path.Combine(_testDataPath, "simple-model.yml");

        // Act
        var model = await ModelLoader.LoadFromFileAsync(yamlPath);

        // Assert
        model.Should().NotBeNull();
        var dict = model as IDictionary<string, object>;
        dict.Should().NotBeNull();
        dict!["Title"].ToString().Should().Be("YAML Test Document");
        dict["Description"].ToString().Should().Be("This is a YAML test document for SadRazor CLI testing.");
        dict["Author"].ToString().Should().Be("YAML Test Author");
    }

    [Fact]
    public async Task LoadFromFileAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "nonexistent.json");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => ModelLoader.LoadFromFileAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadFromFileAsync_WithUnsupportedFormat_ShouldThrowNotSupportedException()
    {
        // Arrange
        var unsupportedPath = Path.Combine(_testDataPath, "test.txt");
        File.WriteAllText(unsupportedPath, "test content");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => ModelLoader.LoadFromFileAsync(unsupportedPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(unsupportedPath))
                File.Delete(unsupportedPath);
        }
    }

    [Theory]
    [InlineData(".json", true)]
    [InlineData(".yml", true)]
    [InlineData(".yaml", true)]
    [InlineData(".xml", true)]
    [InlineData(".txt", false)]
    [InlineData(".md", false)]
    public void IsFormatSupported_WithVariousExtensions_ShouldReturnCorrectResult(string extension, bool expected)
    {
        // Arrange
        var testPath = $"test{extension}";

        // Act
        var result = ModelLoader.IsFormatSupported(testPath);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSupportedExtensions_ShouldReturnAllSupportedFormats()
    {
        // Act
        var extensions = ModelLoader.GetSupportedExtensions();

        // Assert
        extensions.Should().Contain([".json", ".yml", ".yaml", ".xml"]);
        extensions.Length.Should().Be(4);
    }

    [Fact]
    public void LoadFromJson_WithValidJson_ShouldReturnExpandoObject()
    {
        // Arrange
        var json = """{"name": "test", "value": 42}""";

        // Act
        var result = ModelLoader.LoadFromJson(json);

        // Assert
        result.Should().NotBeNull();
        var dict = result as IDictionary<string, object>;
        dict.Should().NotBeNull();
        dict!["name"].ToString().Should().Be("test");
        dict["value"].ToString().Should().Be("42");
    }

    [Fact]
    public void LoadFromJson_WithEmptyString_ShouldReturnNull()
    {
        // Act
        var result = ModelLoader.LoadFromJson("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LoadFromJson_WithInvalidJson_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidJson = """{"name": "test", "value":}""";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ModelLoader.LoadFromJson(invalidJson));
    }
}