using System.Text.Json.Serialization;

namespace SwagMatch.Core.Data.Models.SwaggerDocument;
public sealed class Components
{
    [JsonPropertyName("schemas")]
    public Dictionary<string, Schema>? Schemas { get; set; }
    public override string ToString()
    {
        if (Schemas == null || Schemas.Count == 0)
            return "No schemas defined.";

        var lines = new List<string>();

        foreach (var kvp in Schemas)
        {
            var name = kvp.Key;
            var schema = kvp.Value;
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(schema.Type))
                parts.Add($"Type: {schema.Type}");

            if (!string.IsNullOrWhiteSpace(schema.Format))
                parts.Add($"Format: {schema.Format}");

            if (schema.Nullable.HasValue)
                parts.Add($"Nullable: {schema.Nullable}");

            if (!string.IsNullOrWhiteSpace(schema.Ref))
                parts.Add($"$ref: {schema.Ref}");

            if (schema.Properties != null)
                parts.Add($"Properties: {schema.Properties.Count}");

            if (schema.Items != null)
                parts.Add("Items: present");

            if (schema.AdditionalProperties != null)
                parts.Add($"AdditionalProperties: {schema.AdditionalProperties.GetType().Name}");

            lines.Add($"\n - {name} -> {string.Join("; ", parts)}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed class Schema
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, SwaggerProperty>? Properties { get; set; }

    [JsonPropertyName("items")]
    public Schema? Items { get; set; }

    [JsonPropertyName("additionalProperties")]
    public object? AdditionalProperties { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("nullable")]
    public bool? Nullable { get; set; }

    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
}
public sealed class SwaggerProperty
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("nullable")]
    public bool? Nullable { get; set; }

    [JsonPropertyName("items")]
    public Schema? Items { get; set; }

    [JsonPropertyName("additionalProperties")]
    public object? AdditionalProperties { get; set; }
}
