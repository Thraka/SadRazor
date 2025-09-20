using System.Dynamic;
using System.Text.Json;
using System.Xml;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SadRazor.Cli.Services;

/// <summary>
/// Service for loading model data from various file formats
/// </summary>
public class ModelLoader
{
    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Load model from file, auto-detecting format
    /// </summary>
    public static async Task<object?> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Model file not found: {filePath}");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var content = await File.ReadAllTextAsync(filePath);

        return extension switch
        {
            ".json" => LoadFromJson(content),
            ".yml" or ".yaml" => LoadFromYaml(content),
            ".xml" => LoadFromXml(content),
            _ => throw new NotSupportedException($"Unsupported model file format: {extension}")
        };
    }

    /// <summary>
    /// Load model from file with specified format
    /// </summary>
    public static async Task<object?> LoadFromFileAsync(string filePath, string format)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Model file not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath);

        return format.ToLowerInvariant() switch
        {
            "json" => LoadFromJson(content),
            "yaml" or "yml" => LoadFromYaml(content),
            "xml" => LoadFromXml(content),
            "auto" => await LoadFromFileAsync(filePath), // Recurse with auto-detection
            _ => throw new NotSupportedException($"Unsupported model format: {format}")
        };
    }

    /// <summary>
    /// Load model from JSON string
    /// </summary>
    public static object? LoadFromJson(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {
            // Use Newtonsoft.Json for better dynamic object support
            return JsonConvert.DeserializeObject<ExpandoObject>(jsonContent);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load model from YAML string
    /// </summary>
    public static object? LoadFromYaml(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return null;

        try
        {
            return _yamlDeserializer.Deserialize<ExpandoObject>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse YAML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load model from XML string
    /// </summary>
    public static object? LoadFromXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return null;

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            return XmlToExpandoObject(doc.DocumentElement);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException($"Failed to parse XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load strongly-typed model from file
    /// </summary>
    public static async Task<T?> LoadFromFileAsync<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Model file not found: {filePath}");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var content = await File.ReadAllTextAsync(filePath);

        return extension switch
        {
            ".json" => LoadFromJson<T>(content),
            ".yml" or ".yaml" => LoadFromYaml<T>(content),
            ".xml" => LoadFromXml<T>(content),
            _ => throw new NotSupportedException($"Unsupported model file format: {extension}")
        };
    }

    /// <summary>
    /// Load strongly-typed model from JSON string
    /// </summary>
    public static T? LoadFromJson<T>(string jsonContent) where T : class
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<T>(jsonContent);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load strongly-typed model from YAML string
    /// </summary>
    public static T? LoadFromYaml<T>(string yamlContent) where T : class
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return null;

        try
        {
            return _yamlDeserializer.Deserialize<T>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse YAML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Load strongly-typed model from XML string
    /// </summary>
    public static T? LoadFromXml<T>(string xmlContent) where T : class
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return null;

        try
        {
            // Convert XML to JSON first, then deserialize to T
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            var json = JsonConvert.SerializeXmlNode(doc.DocumentElement);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert XML node to ExpandoObject for dynamic access
    /// </summary>
    private static ExpandoObject? XmlToExpandoObject(XmlNode? node)
    {
        if (node == null) return null;

        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object>)expando;

        // Add attributes
        if (node.Attributes != null)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                dict[$"@{attr.Name}"] = attr.Value;
            }
        }

        // Add child nodes
        var groups = node.ChildNodes.Cast<XmlNode>()
            .Where(n => n.NodeType == XmlNodeType.Element)
            .GroupBy(n => n.Name);

        foreach (var group in groups)
        {
            var items = group.ToList();
            if (items.Count == 1)
            {
                var child = XmlToExpandoObject(items[0]);
                if (child != null)
                    dict[items[0].Name] = child;
            }
            else
            {
                var array = items.Select(XmlToExpandoObject).Where(x => x != null).ToArray();
                if (array.Length > 0)
                    dict[items[0].Name] = array;
            }
        }

        // Add text content if no child elements
        if (!node.HasChildNodes || node.ChildNodes.Cast<XmlNode>().All(n => n.NodeType != XmlNodeType.Element))
        {
            if (!string.IsNullOrWhiteSpace(node.InnerText))
                dict["#text"] = node.InnerText;
        }

        return expando;
    }

    /// <summary>
    /// Get supported file extensions
    /// </summary>
    public static string[] GetSupportedExtensions()
    {
        return [".json", ".yml", ".yaml", ".xml"];
    }

    /// <summary>
    /// Check if file format is supported
    /// </summary>
    public static bool IsFormatSupported(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return GetSupportedExtensions().Contains(extension);
    }
}