using Microsoft.Extensions.Logging;
using System;
namespace Core.Client;
public sealed class RestClient(HttpClient httpClient, ILogger<RestClient> logger) : IRestClient
{
    private Utils _utils = new(logger);
    public async Task<T?> Get<T>(string uri, CancellationToken cancellationToken = default)
    {
        return await Get<T, T>(uri, default, cancellationToken);
    }
    public async Task<TResponse?> Get<TRequest, TResponse>(string uri, TRequest? data, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
            return await _utils.HandleResponse<TResponse>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "[Get] => {0} not reachable: {1}", uri, ex.Message);
            return default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GET] => {0}: {1}", uri, ex.Message);
            return default;
        }
    }
    public async Task<TResponse?> Post<TRequest, TResponse>(string uri, TRequest data, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(uri, _utils.Serelize(data), cancellationToken);
            return await _utils.HandleResponse<TResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[POST] Error calling {0}: {1}", uri, ex.Message);
            return default;
        }
    }
    public async Task<TResponse?> Put<TRequest, TResponse>(string uri, TRequest data, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PutAsync(uri, _utils.Serelize(data), cancellationToken);
            return await _utils.HandleResponse<TResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PUT] Error calling {0}: {1}", uri, ex.Message);
            return default;
        }
    }
    public async Task<TResponse?> Delete<TRequest, TResponse>(string uri, TRequest? data, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await httpClient.DeleteAsync(uri, cancellationToken);
            return await _utils.HandleResponse<TResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DELETE] Error calling {0}: {1}", uri, ex.Message);
            return default;
        }
    }
    public string GetUrlName(Uri uri) => uri.Segments[^1].TrimEnd('/');
    public bool IsValidUrl(string? url)
    {
        if (url is null) return false;

        bool isValid = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        if (uriResult!.IsAbsoluteUri)
        {
            logger.LogDebug("[IsValidUrl] {0} => Valid absolute URL: {1}", url, uriResult.AbsoluteUri);
        }
        else
        {
            logger.LogDebug("[IsValidUrl] {0} => Valid relative URL: {1}", url, uriResult.OriginalString);
        }
            
        return isValid;
    }
    public Uri? GetUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
       
        if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? uri))
        {
            if (uri.IsAbsoluteUri)
            {
                return uri;
            }
        }
        return null;
    }
    public async Task<(string? Value, string? Name)> GetJson(string path)
    {
        Uri? url = GetUrl(path);
        if (url is not null && !url.IsFile)
        {
            logger.LogDebug("[GetJson] => Reading from service: {0}", url.AbsolutePath);

            return (await Get<string>(url.AbsoluteUri), url.AbsoluteUri);
        }
        else
        {
            logger.LogError("[GetJson] => Url is null or file: {0}", path);
        }

        return (null, null);
    }
}