using System.ComponentModel;
using System.Dynamic;
using System.Text.Json;
using System.Xml;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SadRazorEngine.Core.Services;

/// <summary>
/// Service for loading model data from various formats (JSON, YAML, XML)
/// </summary>
public static class ModelLoader
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
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            var expandoObject = XmlToExpandoObject(doc.DocumentElement);
            
            // Convert ExpandoObject to strongly-typed model
            var json = JsonConvert.SerializeObject(expandoObject);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert XML element to ExpandoObject
    /// </summary>
    private static ExpandoObject? XmlToExpandoObject(XmlElement? element)
    {
        if (element == null)
            return null;

        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;

        // Handle attributes
        foreach (XmlAttribute attr in element.Attributes)
        {
            expandoDict[attr.Name] = attr.Value;
        }

        // Handle child elements
        var childElementGroups = element.ChildNodes.OfType<XmlElement>()
            .GroupBy(e => e.Name);

        foreach (var group in childElementGroups)
        {
            var elements = group.ToList();
            if (elements.Count == 1)
            {
                var child = elements[0];
                if (child.HasChildNodes && child.ChildNodes.OfType<XmlElement>().Any())
                {
                    expandoDict[child.Name] = XmlToExpandoObject(child);
                }
                else
                {
                    expandoDict[child.Name] = child.InnerText;
                }
            }
            else
            {
                // Multiple elements with the same name - create an array
                var array = new List<object?>();
                foreach (var child in elements)
                {
                    if (child.HasChildNodes && child.ChildNodes.OfType<XmlElement>().Any())
                    {
                        array.Add(XmlToExpandoObject(child));
                    }
                    else
                    {
                        array.Add(child.InnerText);
                    }
                }
                expandoDict[group.Key] = array;
            }
        }

        // Handle text content
        if (!element.HasChildNodes || !element.ChildNodes.OfType<XmlElement>().Any())
        {
            if (!string.IsNullOrWhiteSpace(element.InnerText))
            {
                expandoDict["_text"] = element.InnerText;
            }
        }

        return expando;
    }
}