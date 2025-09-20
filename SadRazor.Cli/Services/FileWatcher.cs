namespace SadRazor.Cli.Services;

/// <summary>
/// Service for watching file system changes
/// </summary>
public class FileWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly Dictionary<string, DateTime> _lastChangeTime = new();
    private readonly object _lock = new();
    private readonly int _debounceMs;
    private readonly string[] _excludePatterns;
    private bool _disposed;

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public FileWatcher(int debounceMs = 500, string[]? excludePatterns = null)
    {
        _debounceMs = debounceMs;
        _excludePatterns = excludePatterns ?? Array.Empty<string>();
    }

    /// <summary>
    /// Start watching the specified directories
    /// </summary>
    public void StartWatching(IEnumerable<string> watchPaths, bool recursive = true)
    {
        foreach (var path in watchPaths)
        {
            StartWatchingPath(path, recursive);
        }
    }

    /// <summary>
    /// Start watching a specific path
    /// </summary>
    public void StartWatchingPath(string path, bool recursive = true)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Watch path not found: {path}");
        }

        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = recursive,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Changed += OnFileSystemEvent;
        watcher.Created += OnFileSystemEvent;
        watcher.Deleted += OnFileSystemEvent;
        watcher.Renamed += OnFileSystemEvent;

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    /// <summary>
    /// Stop watching all paths
    /// </summary>
    public void StopWatching()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        // Skip if file should be excluded
        if (ShouldExclude(e.FullPath))
            return;

        // Debounce rapid changes
        lock (_lock)
        {
            var now = DateTime.Now;
            if (_lastChangeTime.TryGetValue(e.FullPath, out var lastChange))
            {
                if ((now - lastChange).TotalMilliseconds < _debounceMs)
                    return;
            }
            _lastChangeTime[e.FullPath] = now;
        }

        // Determine file type
        var fileType = GetFileType(e.FullPath);
        if (fileType == FileType.Unknown)
            return;

        // Raise event
        FileChanged?.Invoke(this, new FileChangedEventArgs
        {
            FilePath = e.FullPath,
            ChangeType = e.ChangeType,
            FileType = fileType
        });
    }

    private bool ShouldExclude(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var relativePath = filePath;

        foreach (var pattern in _excludePatterns)
        {
            // Simple pattern matching - in a full implementation, use proper glob matching
            if (pattern.Contains("**"))
            {
                var simplePattern = pattern.Replace("**", "*");
                if (relativePath.Contains(simplePattern.Replace("*", "")))
                    return true;
            }
            else if (pattern.Contains("*"))
            {
                var simplePattern = pattern.Replace("*", "");
                if (fileName.Contains(simplePattern) || relativePath.Contains(simplePattern))
                    return true;
            }
            else if (relativePath.Contains(pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static FileType GetFileType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".cshtml" => FileType.Template,
            ".json" or ".yml" or ".yaml" or ".xml" => FileType.Model,
            _ => FileType.Unknown
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopWatching();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event arguments for file change notifications
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; init; } = "";
    public WatcherChangeTypes ChangeType { get; init; }
    public FileType FileType { get; init; }
}

/// <summary>
/// Types of files we're interested in watching
/// </summary>
public enum FileType
{
    Unknown,
    Template,
    Model
}