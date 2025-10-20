using System.Text.Json;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.IO;
using SwagMatch.Core.Client;
using SwagMatch.Core.Tools.MockGenerator;
using SwagMatch.Core.Tools.HealthCheck;
using SwagMatch.Core.Data.Models.Match;
using SwagMatch.Core.Data.Models.SwaggerDocument;
using SwagMatch.Core.Data.Models.UserInput;

namespace SwagMatch.Core.Data;
public sealed class Grabber(ILogger logger, IRestClient client, string path)
{
    private readonly SwaggerHealth _swagCheck = new(logger);
    private readonly JsonFile _jsonFile = new(logger, path);
    private readonly Mapper _swagMap = new(logger);
    private readonly SwaggerGenerator _swagGen = new(logger);
    public async Task<(List<List<Endpoint>>, List<string>)> GatherInfo(List<UserInputPath> swagDefinitions)
    {
        List<List<Endpoint>> swagEndpoints = new();
        List<string> healthyEndpoints = new();

        foreach (UserInputPath swagDefinition in swagDefinitions)
        {
            (string? Value, string? Name) json = await GetJson(swagDefinition.Path, swagDefinition.GenerateMock);
            Document? swagDoc = DeserializeSwagger(json.Value);
            if (swagDoc is null) continue;

            bool isHealthy = _swagCheck.IsHealthy(swagDoc);
            if (!isHealthy) continue;

            List<Endpoint>? swagPaths = _swagMap.MapDocument(swagDoc);
            if (swagPaths is null) continue;

            swagEndpoints.Add(swagPaths);
            healthyEndpoints.Add(json.Name ?? "unknown");
        }

        return (swagEndpoints, healthyEndpoints);
    }
    private async Task<(string? value, string? name)> GetJson(string swaggerPath, bool generateMock)
    {
        Uri? url = client.GetUrl(swaggerPath);

        if (generateMock)
        {
            string path;
            if (url is null)
            {
                path = _jsonFile.GetPath(swaggerPath);
            }
            else
            {
                path = client.GetUrlName(url);
            }

            logger.LogInformation("[GetJson] Generating Mocked swagger: {0}.", path);

            Document? swagger = _swagGen.GenerateSwagger();
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