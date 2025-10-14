using System.Text.Json;
namespace SwagMatch.Core.Models.Swagger;
public sealed class PathItem
{
    public Operation? Get { get; set; }
    public Operation? Post { get; set; }
    public Operation? Put { get; set; }
    public Operation? Delete { get; set; }
    public Operation? Patch { get; set; }
    public Operation? Head { get; set; }
    public Operation? Options { get; set; }
    public Operation? Trace { get; set; }
    public override string ToString()
    {
        var methodMap = new Dictionary<string, Operation?>
        {
            { "GET", Get },
            { "POST", Post },
            { "PUT", Put },
            { "DELETE", Delete },
            { "PATCH", Patch },
            { "HEAD", Head },
            { "OPTIONS", Options },
            { "TRACE", Trace }
        };

        foreach (var kvp in methodMap)
        {
            if (kvp.Value != null)
            {
                var op = kvp.Value;
                var strings = new List<string>();

                if (!string.IsNullOrWhiteSpace(op.Name)) strings.Add($"Name: {op.Name}");
                if (!string.IsNullOrWhiteSpace(op.OperationId)) strings.Add($"OperationId: {op.OperationId}");
                if (!string.IsNullOrWhiteSpace(op.Title)) strings.Add($"Title: {op.Title}");
                if (!string.IsNullOrWhiteSpace(op.Summary)) strings.Add($"Summary: {op.Summary}");

                var tags = op.Tags ?? new List<string>();
                if (!tags.Contains("default")) tags.Insert(0, "default");
                strings.Add($"Tags: [{string.Join(", ", tags)}]");

                if (op.Parameters != null)
                    strings.Add($"Parameters: {op.Parameters.Count}");

                if (op.Responses != null)
                    strings.Add($"Responses: {op.Responses.Count}");

                return $"{kvp.Key} -> {string.Join("; ", strings)}";
            }
        }

        return "No operation defined.";
    }
}

public sealed class Operation
{
    public string? Name { get; set; }
    public List<string>? Tags { get; set; }
    public string? OperationId { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public List<Parameter>? Parameters { get; set; }
    public RequestBody? RequestBody { get; set; }
    public Dictionary<int, Response>? Responses { get; set; }
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(OperationId)}: {OperationId}, {nameof(Title)}: {Title}, {nameof(Summary)}: {Summary}, {nameof(Tags)}: [{string.Join(", ", Tags ?? new List<string>())}], {nameof(Parameters)}: {Parameters?.Count}, {nameof(RequestBody)}: {(RequestBody != null ? "Yes" : "No")}, {nameof(Responses)}: {Responses?.Count}";
    }
}
public sealed class Parameter
{
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? In { get; set; } // query, path, header, cookie
    public bool Required { get; set; }
    public JsonElement Schema { get; set; }
}
public sealed class RequestBody
{
    public Dictionary<string, MediaType>? Content { get; set; }
    public bool Required { get; set; }
}
public sealed class Response
{
    public string? Description { get; set; }
    public Dictionary<string, MediaType>? Content { get; set; }
}
public sealed class MediaType
{
    public JsonElement Schema { get; set; }
    public JsonElement? Example { get; set; }
}
