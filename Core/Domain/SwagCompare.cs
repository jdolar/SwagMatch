using Core.Client;
using Core.IO;
using Core.Models;
using F23.StringSimilarity;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.UserInput;
using System.Diagnostics;
using Endpoint = Core.Models.Endpoint;

namespace SwagMatch.Core.Domain
{
    public sealed class SwagCompare(IRestClient client, ILogger<SwagMatch> logger, AppSettings config)
    {
        private readonly SwagGet _swagGet = new(logger, client, config.Path);
        private async Task<int> FindData()
        {
            (List<List<Endpoint>> swaggers, List<string> swaggersName) = await _swagGet.GatherInfo(config.SwaggerDefinitions!);

            return 0;
        }
    }
}
