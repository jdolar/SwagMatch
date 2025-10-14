using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.Swagger;
using System.IO;
namespace Core;
public sealed class SwagCheck(ILogger logger)
{
    public bool IsHealthy(Document? swagDocument)
    {
        if (swagDocument is null)
        {
            logger.LogError("[IsHealthy] Swagger JSON is empty or null");
            return false;
        }

        try
        {
            if (swagDocument.OpenApi is null)
            {
                logger.LogError("[IsHealthy] Missing 'openapi' property");
                return false;
            }

            if (swagDocument.Info is null ||
                swagDocument?.Info?.Title is null ||
                swagDocument?.Info?.Version is null)
            {
                logger.LogError("[IsHealthy] Missing 'info' or 'info.title' or 'info.version'");
                return false;
            }

            if (swagDocument.Paths is null)
            {
                logger.LogError("[IsHealthy] '{0}' Missing 'paths' section", swagDocument?.Info?.Title);
                return false;
            }

            if (swagDocument.Paths is null || swagDocument?.Paths?.Count == 0)
            {
                logger.LogError("[IsHealthy] '{0}' Empty 'paths' section", swagDocument?.Info?.Title);
                return false;
            }

            foreach (var path in swagDocument?.Paths!)
            {
                if (path.Value.Get is null && path.Value.Post is null &&
                    path.Value.Put is null && path.Value.Delete is null &&
                    path.Value.Patch is null && path.Value.Head is null &&
                    path.Value.Options is null && path.Value.Trace is null)
                {
                    logger.LogError("[IsHealthy] '{0}' has no operations defined: {1}", swagDocument?.Info?.Title, path.Key);
                }
            }

            logger.LogInformation("[IsHealthy] '{0}' is healthy:\n - OpenAPI: {1}\n - Version: {2}\n - Info: {3}\n - Endpoints: {4}\n - Components: {5}",
                swagDocument?.Info?.Title, swagDocument?.OpenApi, swagDocument?.Info?.Version, swagDocument?.Info, swagDocument?.Paths.Count, swagDocument?.Components?.Schemas?.Count);

            logger.LogDebug("[IsHealthy] '{0}' Paths:{1}", swagDocument?.Info?.Title, string.Join(" - ", swagDocument!.Paths.Select(path => $"\n - {path.Key} {path.Value.ToString()}")));
            logger.LogDebug("[IsHealthy] '{0}' Components:{1}", swagDocument?.Info?.Title, string.Join(" - ", swagDocument!.Components!.ToString()));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IsHealthy] '{0} 'Unexpected error while validating Swagger", swagDocument?.Info?.Title);
        }
        return false;
    }
}