using Core.Client;
using Core.IO;
using Core.Models;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.Swagger;
using SwagMatch.Core.Models.UserInput;
using System.Text.Json;
namespace SwagMatch.Core.Domain;

public sealed class SwagGet(ILogger logger, IRestClient client, string path)
{
    private readonly SwagCheck _swagCheck = new(logger);
    private readonly JsonFile _jsonFile = new(logger, path);
    private readonly SwagMap _swagMap = new(logger);
    private readonly SwagGen _swagGen = new(logger);

    public async Task<(List<Endpoint>?, string?, bool)> GetEndpoints(string path, MockConfig genConfig)
    {
        (Document? Value, string? Name) swagDoc = await GetSwaggerDocument(path, genConfig);
        return (_swagMap.MapDocument(swagDoc.Value), swagDoc.Name, _swagCheck.IsHealthy(swagDoc.Value));
    }
    public async Task<(List<List<Endpoint>>, List<string>)> GetSwaggers(List<string> swagDefinitions, MockConfig genConfig)
    {
        List<List<Endpoint>> swagEndpoints = new();
        List<string> healthyEndpoints = new();

        foreach (string swagDefinition in swagDefinitions)
        {
            (List<Endpoint>? EndPoints, string? Name, bool IsHealthy) swagger = await GetEndpoints(swagDefinition, genConfig);
            if (swagger.EndPoints is null || !swagger.IsHealthy) continue;

            swagEndpoints.Add(swagger.EndPoints);
            healthyEndpoints.Add(swagger.Name ?? "unknown");
        }

        return (swagEndpoints, healthyEndpoints);
    }
    private async Task<(Document?, string?)> GetSwaggerDocument(string url, MockConfig genConfig)
    {
        (string? Value, string? Name) json = await GetJson(url, genConfig);
        return (DeserializeSwagger(json.Value), json.Name);
    }
    private async Task<(string? value, string? name)> GetJson(string swaggerPath, MockConfig genConfig)
    {
        Uri? url = client.GetUrl(swaggerPath);

        if (genConfig.IsEnabled)
        {
            string path = url is null ? _jsonFile.GetPath(swaggerPath) : client.GetUrlName(url);
            logger.LogInformation("[GetJson] Generating Mocked[Type={0}] Swagger: {1}.", genConfig.Type, path);

            Dictionary<string, PathItem>? paths = new();
            switch (genConfig.Type)
            {
                case 0: paths = _swagGen.GeneratePaths(genConfig); break;
                case 1: paths = _swagGen.CreateEndPointPairs(); break;
            }

            Document? swagger = _swagGen.CreateSwagger(paths);
            return (SerializeSwagger(swagger), path);
        }
        else
        {
            bool isUrl = client.IsValidUrl(url?.AbsoluteUri);
            if (url is null || isUrl == false)
            {
                return await _jsonFile.GetJson(swaggerPath);
            }
            else if (isUrl)
            {
                return await client.GetJson(url?.AbsoluteUri!);
            }
        }

        return (null, null);
    }
    private Document? DeserializeSwagger(string? json)
    {
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<Document>(json!, _jsonFile.SerelizerOptions);
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "[DeserializeSwagger] JSON deserialization error: {0}", jex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DeserializeSwagger] Unexpected error during deserialization: {0}", ex.Message);
        }

        return null;
    }
    private string? SerializeSwagger(Document? swagger)
    {
        if (swagger is null) return null;

        try
        {
            return JsonSerializer.Serialize(swagger!, _jsonFile.SerelizerOptions);
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "[SerializeSwagger] JSON serialization error: {0}", jex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SerializeSwagger] Unexpected error during serialization: {0}", ex.Message);
        }

        return null;
    }
}