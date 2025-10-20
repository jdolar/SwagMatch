using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace SwagMatch.Core.IO;
public sealed class JsonFile(ILogger logger, string filePath) : BaseFile(logger, filePath)
{
    public JsonSerializerOptions SerelizerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public async Task<int> WriteAsync(string fileName, string? content)
    {
        return await WriteAsync(fileName, "json", content);
    }
    public async Task<string> ReadAsync(string fileName)
    {
        return await ReadAsync(fileName, "json");
    }
    public async Task<(string? Value, string? Name)> GetJson(string fileName)
    {
        logger.LogDebug("[GetJson] => Reading from file: {0}", fileName);
        try
        {
            return (await ReadAsync(fileName), fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GetJson] => error reading from file[{0}]: {1}", fileName, ex.Message);
        }

        return (null, null);
    }
    public string GetPath(string path) => NormalizeFilePath(path, "json");
}
