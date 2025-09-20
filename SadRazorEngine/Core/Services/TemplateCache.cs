using SadRazorEngine.Core.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SadRazorEngine.Core.Services;

/// <summary>
/// Cache for compiled templates to improve performance on repeated renders
/// </summary>
public class TemplateCache
{
    private readonly ConcurrentDictionary<string, CachedTemplate> _cache = new();
    private readonly int _maxCacheSize;
    private readonly TimeSpan _expirationTime;

    public TemplateCache(int maxCacheSize = 100, TimeSpan? expirationTime = null)
    {
        _maxCacheSize = maxCacheSize;
        _expirationTime = expirationTime ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Get a cached template if available and not expired
    /// </summary>
    public ICompiledTemplate? Get(string templateContent, Type? modelType)
    {
        var key = GenerateKey(templateContent, modelType);
        
        if (_cache.TryGetValue(key, out var cachedTemplate))
        {
            if (DateTime.UtcNow - cachedTemplate.CreatedAt <= _expirationTime)
            {
                cachedTemplate.LastAccessed = DateTime.UtcNow;
                return cachedTemplate.Template;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(key, out _);
            }
        }

        return null;
    }

    /// <summary>
    /// Store a compiled template in the cache
    /// </summary>
    public void Set(string templateContent, Type? modelType, ICompiledTemplate template)
    {
        var key = GenerateKey(templateContent, modelType);
        
        // Ensure cache doesn't exceed max size
        if (_cache.Count >= _maxCacheSize)
        {
            EvictOldestEntries();
        }

        var cachedTemplate = new CachedTemplate
        {
            Template = template,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow
        };

        _cache.TryAdd(key, cachedTemplate);
    }

    /// <summary>
    /// Clear all cached templates
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            EntryCount = _cache.Count,
            MaxSize = _maxCacheSize,
            ExpirationTime = _expirationTime
        };
    }

    private string GenerateKey(string templateContent, Type? modelType)
    {
        using var sha256 = SHA256.Create();
        var input = templateContent + (modelType?.FullName ?? "");
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    private void EvictOldestEntries()
    {
        // Remove 25% of entries, prioritizing least recently accessed
        var entriesToRemove = Math.Max(1, _cache.Count / 4);
        var oldestEntries = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(entriesToRemove)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldestEntries)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private class CachedTemplate
    {
        public required ICompiledTemplate Template { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastAccessed { get; set; }
    }
}

/// <summary>
/// Statistics about the template cache
/// </summary>
public class CacheStatistics
{
    public int EntryCount { get; init; }
    public int MaxSize { get; init; }
    public TimeSpan ExpirationTime { get; init; }
}