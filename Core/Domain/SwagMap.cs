using Core.Client;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.Swagger;
using System.Collections.Generic;
using System.Text.Json;
using MediaType = SwagMatch.Core.Models.Swagger.MediaType;
namespace SwagMatch.Core.Domain;

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

        logger.LogInformation("[MapDocument] Mapping Swagger[Name={0}] Document with {1} paths",swagger.Info.Title, swagger.Paths.Count);
        List<Endpoint>? endpoints = null;
        foreach (KeyValuePair<string, PathItem> path in swagger?.Paths!)
        {
            logger.LogDebug("[MapDocument] Mapping: {0}", path.Key);
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
                Operation = operation!.Value.operation.OperationId ??= string.Empty,
                Parameters = MapParameters(operation.Value.operation.Parameters),
                Tags = operation.Value.operation.Tags is not null ? string.Join(", ", operation.Value.operation.Tags) : string.Empty,
                Title = operation.Value.operation.Title ??= string.Empty,
                BodyRequired = boldyRequired,
                RequestBody = MapRequestBody(operation.Value.operation?.RequestBody?.Content, swagger?.Components?.Schemas, "body"),
                Responses = MapResponses(operation.Value.operation?.Responses, swagger?.Components?.Schemas)
            });
        }
        logger.LogInformation("[MapDocument] Mapping Swagger Document with {PathCount} paths completed", swagger.Paths.Count);
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
    private List<Parameter>? MapParameters(List<Parameter>? parameters)
    {
        if (parameters is null)
            return null;

        foreach (var param in parameters)
        {
            // Check if can find Type as Ref
            if (string.IsNullOrEmpty(param.Type))
            {
                if (param.Schema.ValueKind == JsonValueKind.Object)
                {
                    param.Schema.TryGetProperty("type", out var type);
                    if (type.ValueKind == JsonValueKind.String)
                    {
                        param.Type = type.GetString();
                    }
                    else
                    {
                        JsonElement typeSchema = new();
                        param.Schema.TryGetProperty("$ref", out typeSchema);
                        param.Type = typeSchema.GetString()!.Split("/").Last();
                    }

                }
                else if (param.Schema.ValueKind == JsonValueKind.String)
                {
                    param.Type = param.Schema.GetString()!.Split("/").Last();
                }
            }
        }
        return parameters;
    }
    private List<Parameter> MapRequestBody(Dictionary<string, MediaType>? content, Dictionary<string, Schema>? schemas, string? inText = null)
    {
        MediaType? mediaType = null;
        List<Parameter> parameters = new();
        if (content is null)
            return parameters;

        string? contentType = content?.Keys.FirstOrDefault();
        content?.TryGetValue(contentType!, out mediaType);

        JsonElement contentTypeRefSchema = new();
        mediaType?.Schema.TryGetProperty("$ref", out contentTypeRefSchema);


        foreach (var entry in content!)
        {
            string reference = string.Empty;
            if (contentTypeRefSchema.ValueKind == JsonValueKind.String && contentTypeRefSchema.GetString() is string str)
            {
                reference = str.Split('/').Last();
            }

            Schema? componentSchema = null;
            schemas?.TryGetValue(reference, out componentSchema);

            if (componentSchema?.Type?.ToLower() == mediaType?.Schema.ValueKind.ToString().ToLower())
            {
                foreach (var prop in componentSchema!.Properties!)
                {
                    Parameter param = new()
                    {
                        Type = prop.Value?.Type,
                        Name = prop.Key,
                        Description = prop.Value?.Description,
                        In = inText ??= string.Empty,
                        Required = prop.Value!.Nullable ??= false,
                        Schema = JsonDocument.Parse($"{{ \"type\": \"{prop.Value.Type}\" }}").RootElement,
                        Schema1 = prop.Value!.Schema,
                        Format = prop.Value?.Format,
                        Ref = prop.Value?.Ref
                    };

                    if (param.Type is null)
                    {
                        if (prop.Value?.Ref is not null)
                        {
                            param.Type = prop.Value?.Ref.Split("/").Last();
                        }
                    }

                    parameters.Add(param);
                }
            }
            else
            {
                try 
                {
                    string jsonText = mediaType?.Schema.GetRawText() ?? string.Empty;
                    using JsonDocument doc = JsonDocument.Parse(jsonText);
                    JsonElement root = doc.RootElement;

                    string type = root.GetProperty("type").GetString() ?? string.Empty;

                    JsonElement itemsValue = new();
                    root.TryGetProperty("items", out itemsValue);

                    JsonElement refValue = new();
                    itemsValue.TryGetProperty("$ref", out refValue);
                    if (refValue.ValueKind == JsonValueKind.Undefined)
                    {
                        root.TryGetProperty("$ref", out refValue);


                        string? refText = refValue.GetString();
                        if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(refText))
                        {
                            type = refText.Split("/").Last();
                        }

                        Parameter param = new()
                        {
                            Type = type,
                            Name = entry.Key,
                            In = inText ??= string.Empty,
                            Ref = refValue.GetString(),
                        };

                        param.Type = param.Type switch
                        {
                            "array" when param.Ref is not null => $"{param.Type}[{param.Ref.Split("/").Last()}]",
                            "object" when param.Ref is not null => $"{param.Type}[{param.Ref.Split("/").Last()}]",
                            "dictionary" when param.Ref is not null => $"{param.Type}[{param.Ref.Split("/").Last()}]",
                            _ => param.Type
                        };

                        parameters.Add(param);
                    }
                }
                catch { 
                logger.LogWarning("Failed to parse request body schema for content type: {ContentType}:{content}", entry.Key, entry.Value.Schema.ToString());
                }
               
            }
        }

        return parameters;
    }
    private Dictionary<int, List<Parameter>>? MapResponses(Dictionary<int, Response>? responses, Dictionary<string, Schema>? schemas)
    {
        Dictionary<int, List<Parameter>> result = new();
        foreach (var response in responses!)
        {
            if (response!.Value?.Content is not null)
            {
                var parameters = MapRequestBody(response.Value.Content, schemas, "response");
                result.Add(response.Key, parameters);
            }
            else
            {
                var parameter = new Parameter
                {
                    In = "response",
                    Description = response!.Value?.Description?.Trim() ?? string.Empty
                };

                result.Add(response.Key, new List<Parameter>() { parameter });
            }
        }
        return result;
    }
}
