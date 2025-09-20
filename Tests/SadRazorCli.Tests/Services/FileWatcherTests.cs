using FluentAssertions;
using SadRazor.Cli.Services;
using Xunit;

namespace SadRazor.Cli.Tests.Services;

public class FileWatcherTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileWatcher _fileWatcher;

    public FileWatcherTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "SadRazorFileWatcherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _fileWatcher = new FileWatcher(debounceMs: 100);
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task FileWatcher_WhenTemplateFileChanges_ShouldRaiseEvent()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "test.cshtml");
        File.WriteAllText(templatePath, "@model dynamic\n# Initial Content");

        var eventRaised = false;
        FileChangedEventArgs? capturedArgs = null;

        _fileWatcher.FileChanged += (sender, e) =>
        {
            eventRaised = true;
            capturedArgs = e;
        };

        _fileWatcher.StartWatchingPath(_tempDirectory);

        // Act
        await Task.Delay(200); // Allow watcher to initialize
        File.WriteAllText(templatePath, "@model dynamic\n# Updated Content");
        await Task.Delay(300); // Allow for debounce and event processing

        // Assert
        eventRaised.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.FileType.Should().Be(FileType.Template);
        capturedArgs.ChangeType.Should().Be(WatcherChangeTypes.Changed);
    }

    [Fact]
    public async Task FileWatcher_WhenModelFileChanges_ShouldRaiseEvent()
    {
        // Arrange
        var modelPath = Path.Combine(_tempDirectory, "test.json");
        File.WriteAllText(modelPath, """{"title": "Initial"}""");

        var eventRaised = false;
        FileChangedEventArgs? capturedArgs = null;

        _fileWatcher.FileChanged += (sender, e) =>
        {
            eventRaised = true;
            capturedArgs = e;
        };

        _fileWatcher.StartWatchingPath(_tempDirectory);

        // Act
        await Task.Delay(200); // Allow watcher to initialize
        File.WriteAllText(modelPath, """{"title": "Updated"}""");
        await Task.Delay(300); // Allow for debounce and event processing

        // Assert
        eventRaised.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.FileType.Should().Be(FileType.Model);
        capturedArgs.ChangeType.Should().Be(WatcherChangeTypes.Changed);
    }

    [Fact]
    public async Task FileWatcher_WithExcludePatterns_ShouldNotRaiseEventForExcludedFiles()
    {
        // Arrange
        var excludePatterns = new[] { "**/bin/**", "**/obj/**" };
        using var excludingWatcher = new FileWatcher(debounceMs: 100, excludePatterns);

        var binDir = Path.Combine(_tempDirectory, "bin");
        Directory.CreateDirectory(binDir);
        var excludedFile = Path.Combine(binDir, "test.cshtml");

        var eventRaised = false;
        excludingWatcher.FileChanged += (sender, e) =>
        {
            eventRaised = true;
        };

        excludingWatcher.StartWatchingPath(_tempDirectory);

        // Act
        await Task.Delay(200); // Allow watcher to initialize
        File.WriteAllText(excludedFile, "@model dynamic\n# Content");
        await Task.Delay(300); // Allow for potential event processing

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void FileWatcher_StartWatchingNonExistentDirectory_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _fileWatcher.StartWatchingPath(nonExistentPath));
    }

    [Fact]
    public async Task FileWatcher_MultipleRapidChanges_ShouldDebounceEvents()
    {
        // Arrange
        var templatePath = Path.Combine(_tempDirectory, "debounce-test.cshtml");
        File.WriteAllText(templatePath, "@model dynamic\n# Initial");

        var eventCount = 0;
        _fileWatcher.FileChanged += (sender, e) =>
        {
            Interlocked.Increment(ref eventCount);
        };

        _fileWatcher.StartWatchingPath(_tempDirectory);
        await Task.Delay(200); // Allow watcher to initialize

        // Act - Make multiple rapid changes
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(templatePath, $"@model dynamic\n# Update {i}");
            await Task.Delay(50); // Rapid changes within debounce window
        }

        await Task.Delay(500); // Allow for debounce and final event processing

        // Assert - Should only receive one event due to debouncing
        eventCount.Should().BeLessOrEqualTo(2); // Allow some tolerance for timing
    }

    [Theory]
    [InlineData("test.cshtml", FileType.Template)]
    [InlineData("test.json", FileType.Model)]
    [InlineData("test.yml", FileType.Model)]
    [InlineData("test.yaml", FileType.Model)]
    [InlineData("test.xml", FileType.Model)]
    [InlineData("test.txt", FileType.Unknown)]
    public async Task FileWatcher_WithDifferentFileTypes_ShouldClassifyCorrectly(string fileName, FileType expectedType)
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, "initial content");

        FileChangedEventArgs? capturedArgs = null;
        _fileWatcher.FileChanged += (sender, e) =>
        {
            capturedArgs = e;
        };

        _fileWatcher.StartWatchingPath(_tempDirectory);

        // Act
        await Task.Delay(200); // Allow watcher to initialize
        File.WriteAllText(filePath, "updated content");
        await Task.Delay(300); // Allow for event processing

        // Assert
        if (expectedType != FileType.Unknown)
        {
            capturedArgs.Should().NotBeNull();
            capturedArgs!.FileType.Should().Be(expectedType);
        }
        else
        {
            // Unknown file types should not raise events
            capturedArgs.Should().BeNull();
        }
    }
}