using SwagMatch.Core.Data.Models.SwaggerDocument;

namespace SwagMatch.Core.Data.Models.Match;
public sealed class Endpoint
{  
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MachParameter { get; set; } = string.Empty;   
    public string? Operation { get; set; } = string.Empty;
    public string? Title { get; set; } = null;
    public string? Tags { get; set; } = string.Empty;
    public bool? BodyRequired { get; set; } = false;
    public List<Parameter>? Parameters { get; set; } = new();
    public List<Parameter>? RequestBody { get; set; } = new();
    public override string ToString()
    {
        return string.Format(
            "0: Path = {0}, 1: Method = {1}, 2: Name = {2}, 3: MachParameter = {3}, " +
            "4: Operation = {4}, 5: Title = {5}, 6: Tags = {6}, " +
            "7: Parameters.Count = {7}, 8: RequestBody.Count = {8}",
            Path,
            Method,
            Name,
            MachParameter,
            Operation,
            Title,
            Tags,
            Parameters?.Count ?? 0,
            RequestBody?.Count ?? 0
        );
    }
}