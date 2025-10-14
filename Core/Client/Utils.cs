using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
namespace Core.Client;
public sealed class Utils
{
    private readonly Encoding _encoding = Encoding.UTF8;
    private const string _mediaType = "application/json";
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger _logger;
    public Utils(ILogger logger, JsonSerializerOptions? serializerOptions = null)
    {
        _logger = logger;
        _serializerOptions = serializerOptions is not null ? serializerOptions : new(JsonSerializerDefaults.Web);
    }
    public StringContent? Serelize<TRequest>(TRequest data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, _serializerOptions);
            return new StringContent(json, _encoding, _mediaType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize request data");
            return default;
        }
    }
    public async Task<TResponse?> HandleResponse<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (typeof(TResponse) == typeof(string))
            {
                return (TResponse)(object)content;
            }

            return JsonSerializer.Deserialize<TResponse>(content, _serializerOptions);
        }
        else
        {
            _logger.LogError("Failed to handle response: {0} - {1}", response.StatusCode, response.ReasonPhrase);
            return default;
        }
    }
}