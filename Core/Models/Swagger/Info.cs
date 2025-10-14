using System.Text.Json.Serialization;

namespace SwagMatch.Core.Models.Swagger;
public sealed class Info
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
    public override string ToString()
    {
        return $"{nameof(Title)}={Title}, {nameof(Version)}={Version}, {nameof(Description)}={Description}";
    }
}
