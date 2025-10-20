using System.Text.Json;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Data;
using SwagMatch.Core.Data.Models.SwaggerDocument;

namespace SwagMatch.Core.Tools.MockGenerator;
public sealed class SwaggerGenerator(ILogger logger)
{
    private readonly Mapper _swagMap = new(logger);
    private readonly Random _random = new();
    public Document? GenerateSwagger(Config? genConf = null)
    {
        Dictionary<string, PathItem> paths = GeneratePaths(genConf ??= new());

        Document? swagDoc = new Document()
        {
            Components = new Components()
            {
                Schemas = GenerateSchemas(paths)
            },
            Paths = paths,
            Info = new Info()
            {
                Title = "Generated API",
                Version = "1.0.0"
            },
            OpenApi = "3.0.0"
        };

        return swagDoc;
    }
    private string GenerateType() => Constants.DataTypes[_random.Next(Constants.DataTypes.Length)];
    private Dictionary<string, Schema> GenerateSchemas(Dictionary<string, PathItem> paths)
    {
        Dictionary<string, Schema> schemas = new();
        foreach (var path in paths.Values)
        {
            var operation = _swagMap.MapOperation(path);
            if (operation is null) continue;

            var content = operation.Value.Item2.RequestBody?.Content;
            if (content is null) continue;

            if (content.Count > 0)
            {
                for (int i = 0; i < content!.Count; i++)
                {
                    string contentType = content.Keys.ElementAt(i);
                    MediaType mediaType = content.Values.ElementAt(i);

                    JsonElement refElement;
                    if (mediaType.Schema.TryGetProperty("$ref", out refElement))
                    {
                        string? refValue = refElement.GetString();
                        const string prefix = "#/components/schemas/";

                        if (refValue != null && refValue.StartsWith(prefix))
                        {
                            string schemaName = refValue.Substring(prefix.Length);
                            Schema schema = new()
                            {
                                Type = "object",
                                Properties = new Dictionary<string, SwaggerProperty>
                                {
                                    ["id"] = new SwaggerProperty { Type = "integer", Format = "int32", Nullable = false },
                                    ["name"] = new SwaggerProperty { Type = "string" },
                                    ["description"] = new SwaggerProperty { Type = "string" },
                                    ["tags"] = new SwaggerProperty
                                    {
                                        Type = "array",
                                        Items = new Schema { Type = "string" }
                                    }
                                }
                            };

                            schemas.Add(schemaName, schema);
                        }
                    }
                }
            }
        }
        return schemas;
    }
    private RequestBody GenerateRequestBody(string operationId, string contentType)
    {
        Schema schemaRef = new()
        {
            Ref = $"#/components/schemas/{operationId}"
        };

        string json = JsonSerializer.Serialize(schemaRef);
        JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        return new RequestBody
        {
            Content = new Dictionary<string, MediaType>
            {
                [contentType] = new MediaType
                {
                    Example = JsonDocument.Parse("{ \"id\": 1, \"name\": \"Sample\", \"description\": \"This is a sample.\", \"tags\": [\"tag1\", \"tag2\"] }").RootElement,
                    Schema = JsonSerializer.Deserialize<JsonElement>(json)
                }
            },
            Required = true
        };
    }
    private Dictionary<string, PathItem> GeneratePaths(Config genConf)
    {
        Dictionary<string, PathItem> paths = new();
        for (int i = 0; i < genConf!.AppCnt; i++)
        {
            string appName = Constants.PathApplications[_random.Next(Constants.PathApplications.Count)];
            for (int j = 0; j < genConf?.PathPerAppCnt; j++)
            {
                for (int k = 0; k < genConf?.OppPerPathPerAppCnt; k++)
                {
                    PathItem pathItem = new();

                    string name = $"{Constants.Names[_random.Next(Constants.Names.Count)]}_{i + 1}{j + 1}{k + 1}";
                    string path = $"/{appName}/{name}";
                    string method = Constants.httpMethods[_random.Next(Constants.httpMethods.Length)];

                    Operation opp = GenerateOperation($"{name}_{j}{k}", method, name, genConf.ReqBodyOrParamCnt);
                    switch (method)
                    {
                        case "Get": pathItem.Get = opp; break;
                        case "Post": pathItem.Post = opp; break;
                        case "Put": pathItem.Put = opp; break;
                        case "Delete": pathItem.Delete = opp; break;
                        case "Patch": pathItem.Patch = opp; break;
                        case "Head": pathItem.Head = opp; break;
                        case "Options": pathItem.Options = opp; break;
                        case "Trace": pathItem.Trace = opp; break;
                    }

                    paths.Add(path, pathItem);
                }
            }
        }
        return paths;
    }
    private Response GenerateResponse(int statusCode)
    {
        string description = Constants.httpStatusDescriptions.ContainsKey(statusCode) ?
            Constants.httpStatusDescriptions[statusCode] :
            "No description available";
        return new Response
        {
            Description = description,
            Content = new Dictionary<string, MediaType>
            {
                ["application/json"] = new MediaType
                {
                    Schema = JsonDocument.Parse("{ \"type\": \"object\" }").RootElement
                }
            }
        };
    }
    private Dictionary<int, Response> GenerateResponses()
    {
        Dictionary<int, Response> responses = new()
        {
            [200] = GenerateResponse(200),
            [201] = GenerateResponse(201),
            [204] = GenerateResponse(204),
            [400] = GenerateResponse(400),
        };
        return responses;
    }
    private Operation GenerateOperation(string operationId, string method, string name, int reqBodyParamCnt)
    {
        string contentType = Constants.MediaTypes[_random.Next(Constants.MediaTypes.Count)];
        Operation operation = new()
        {
            OperationId = operationId,
            Tags = new List<string> { "default" },
            Name = name,
            Title = $"{method} {name}",
            Summary = $"This is the {method} operation for {name}",
            Responses = GenerateResponses()
        };

        if (method.ToLower() == "get" || method.ToLower() == "delete")
        {
            operation.Parameters = GenerateParameters(reqBodyParamCnt, operationId, "query", contentType);
        }
        else
        {
            operation.RequestBody = GenerateRequestBody(operationId, contentType);
        }

        return operation;
    }
    private List<Parameter>? GenerateParameters(int paramCount, string operationId, string inValue, string contentType)
    {
        List<Parameter> parameters = new();
        for (int i = 0; i < paramCount; i++)
        {
            string type = GenerateType();
            parameters.Add(new Parameter
            {
                Type = type,
                Name = $"{operationId}_param_{i + 1}",
                In = inValue,
                Required = _random.Next(0, 2) == 0,
                Schema = JsonDocument.Parse("{ \"type\": \"" + type + "\" }").RootElement
            });
        }
        return parameters;
    }
}
