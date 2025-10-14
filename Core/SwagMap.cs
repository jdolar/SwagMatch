using Core.Client;
using Core.Models;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.Swagger;
using System.Text.Json;
namespace SwagMatch.Core;
public sealed class SwagMap(ILogger logger)
{
    public (string, Operation)? MapOperation(PathItem pathItem)
    {
        if (pathItem.Get != null) return ("GET", pathItem.Get);
        if (pathItem.Post != null) return ("POST", pathItem.Post);
        if (pathItem.Put != null) return ("PUT", pathItem.Put);
        if (pathItem.Delete != null) return ("DELETE", pathItem.Delete);
        if (pathItem.Patch != null) return ("PATCH", pathItem.Patch);
        if (pathItem.Options != null) return ("OPTIONS", pathItem.Options);
        if (pathItem.Head != null) return ("HEAD", pathItem.Head);
        if (pathItem.Trace != null) return ("TRACE", pathItem.Trace);

        return null;
    }
    public List<Endpoint>? MapDocument(Document? swagger)
    {
        if (swagger?.Paths is null || swagger.Paths.Count == 0)
            return null;

        List<Endpoint>? endpoints = null;
        foreach (KeyValuePair<string, PathItem> path in swagger?.Paths!)
        {
            if (path.Value is null || path.Key is null)
                continue;

            (string method, Operation operation)? operation = MapOperation(path.Value!);
            if (operation is null)
                continue;

            string? method = operation?.method;
            if (method == null)
                continue;

            bool boldyRequired = false;
            if (operation!.Value.operation.RequestBody?.Required is not null) boldyRequired = (bool)operation!.Value.operation.RequestBody?.Required!;

            endpoints ??= new List<Endpoint>();
            endpoints.Add(new()
            {
                Path = path.Key,
                Name = MapName(path.Key),
                Method = method ??= string.Empty,
                Operation = operation!.Value.operation.OperationId,
                Parameters = operation.Value.operation.Parameters,
                Tags = operation.Value.operation.Tags is not null ? string.Join(", ", operation.Value.operation.Tags) : string.Empty,
                Title = operation.Value.operation.Title ??= string.Empty,
                BodyRequired = boldyRequired,
                RequestBody = MapRequestBody
                (
                    operation.Value.operation.OperationId!,
                    operation.Value.operation.RequestBody,
                    swagger?.Components?.Schemas
                )
            });
        }

        return endpoints;
    }
    private string MapName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "-";

        // Normalize and split by slash
        string[] parts = path.Trim('/').Split('/');

        if (parts.Length == 0)
            return "-";

        // Check if the last segment is a route parameter like "{id}"
        string lastSegment = parts[^1];
        bool isParam = lastSegment.StartsWith("{") && lastSegment.EndsWith("}");

        string targetSegment = isParam && parts.Length >= 2 ? parts[^2] : lastSegment;

        // Remove extension if present
        return Path.GetFileNameWithoutExtension(targetSegment);
    }
    private List<Parameter>? MapRequestBody(string operationId, RequestBody? reqBody, Dictionary<string, Schema>? schemas)
    {
        if (reqBody is null)
            return null;

        MediaType? mediaType = null;     
        string? contentType = MockConstants.MediaTypes!.FirstOrDefault(ct => reqBody.Content!.TryGetValue(ct, out mediaType));
        if (contentType is null || mediaType is null)
            return null;

        JsonElement contentTypeRefSchema = new();
        mediaType?.Schema.TryGetProperty("$ref", out contentTypeRefSchema);

        Schema? componentSchema = null;
        schemas?.TryGetValue(operationId, out componentSchema);

        List<Parameter> parameters = new();
        if (componentSchema?.Type?.ToLower() == mediaType?.Schema.ValueKind.ToString().ToLower())
        {
            foreach (var prop in componentSchema!.Properties!)
            {
                parameters.Add(new Parameter
                {
                    Type = prop.Value.Type,
                    Name = prop.Key,
                    In = "body",
                    Required = prop.Value.Nullable ??= false,
                    Schema = JsonDocument.Parse($"{{ \"type\": \"{prop.Value.Type}\" }}").RootElement
                });
                ;
            }         
        }
        else
        {
            logger.LogError("[MapRequestBody] Schema type mismatch or missing for operationId: {0}, {1} NOT {2}", 
                operationId, componentSchema?.Type, mediaType?.Schema.ValueKind.ToString());
        }

        return parameters;
    }
}
