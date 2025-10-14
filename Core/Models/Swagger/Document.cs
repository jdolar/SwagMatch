using System.Text.Json.Serialization;
namespace SwagMatch.Core.Models.Swagger;
public sealed class Document
{
    [JsonPropertyName("components")]
    public Components? Components { get; set; }

    [JsonPropertyName("paths")]
    public Dictionary<string, PathItem>? Paths { get; set; }

    [JsonPropertyName("info")]
    public Info? Info { get; set; }

    [JsonPropertyName("openapi")]
    public string? OpenApi { get; set; }

    public override string ToString()
    {
        return $"{nameof(OpenApi)}: {OpenApi}, {nameof(Info.Title)}: {Info?.Title}, {nameof(Info.Version)}: {Info?.Version} {nameof(Paths)}: {Paths?.Count}, {nameof(Components.Schemas)}: {Components?.Schemas?.Count}";
    }
}
