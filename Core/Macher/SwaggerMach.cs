using System.Diagnostics;
using F23.StringSimilarity;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.IO;
using SwagMatch.Core.Client;
using Endpoint = SwagMatch.Core.Data.Models.Match.Endpoint;
using SwagMatch.Core.Data;
using SwagMatch.Core.Data.Models.Match;
using SwagMatch.Core.Data.Models.UserInput;

namespace SwagMatch.Core.Macher;
public sealed class SwaggerMach(IRestClient client, ILogger<SwaggerMach> logger, AppSettings config)
{
    private readonly Grabber _swagGet = new(logger, client, config.Path);
    public async Task<(string, int)> CompareAsync()
    {
        logger.LogDebug("[CompareAsync] Configuration:{0}", config.ToString());

        if (config.SwaggerDefinitions is null || config.SwaggerDefinitions.Count < 2 || config.SwaggerDefinitions.Count > 3)
        {
            logger.LogError("[CompareAsync] Comparison is supported for 2 or 3 Swagger instances only. Actual count: {0}", config.SwaggerDefinitions?.Count ?? 0);
            return (string.Empty, 0);
        }

        string resultsFileName = $"{nameof(SwaggerMach)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

        Stopwatch stopwatch = new();
        stopwatch.Start();
       
        var results = await GatherInfo();

        int mdContentBytesCount = 0;
        string? mdContent = null;
        if (config.Report.GenerateMd)
        {
            // Save results to MD file
            MdFile mdFile = new(logger, config.Path);
            mdContent = mdFile.GenerateContent(results.Matched, results.Matched2and3Only, results.NotMatched, results.SwaggersName, config.Report);
            mdContentBytesCount = await mdFile.WriteAsync(resultsFileName, mdContent);
        }

        int csvContentBytesCount = 0;
        string? csvContent = null;
        if (config.Report.GenerateCsv)
        {
            // Save results to Csv file
            CsvFile csvFile = new(logger, config.Path);
            csvContent = csvFile.GenerateContent(results.Matched, results.Matched2and3Only, results.NotMatched, results.SwaggersName, config.Report);
            csvContentBytesCount = await csvFile.WriteAsync(resultsFileName, csvContent);
        }

        stopwatch.Stop();

        int totalByes = mdContentBytesCount + csvContentBytesCount;
        logger.LogInformation("[CompareAsync] Swagger comparison completed in {0} ms. File(s) saved as: {1}\n - {2}(bytes) MD\n - {3}(bytes) CSV\n - {4}(bytes) Total)",
                stopwatch.ElapsedMilliseconds,
                resultsFileName,
                mdContentBytesCount,
                csvContentBytesCount,
                totalByes);

        if (!config.AutoClose) Console.ReadLine();

        return (resultsFileName, totalByes);
    }
    private (List<EndpointMatch>? Matched, List<EndpointMatch>? Matched2and3Only, List<EndpointMatch>? NotMatched) MatchEndpointsV2(List<List<Endpoint>> swaggers, double threshold = 0.8)
    {
        if (swaggers.Count < 2 || swaggers.Count > 3)
        {
            logger.LogError("Currently comparison is possible for only 2 or 3 swagger instances, actual number: {0}", swaggers.Count);
            return (null, null, null);
        }

        int swagger1 = 0;
        int swagger2 = 1;
        int swagger3 = 2;

        List<EndpointMatch> matched = new();
        List<EndpointMatch> matched2and3Only = new();
        List<EndpointMatch> notMatched = new();

        HashSet<string> usedB = new();
        HashSet<string> usedC = new();
        JaroWinkler winky = new();

        // Match based on A endpoints
        foreach (Endpoint epA in swaggers[swagger1].OrderBy(x => x.Name))
        {
            Endpoint? bestB = null, bestC = null;
            double bestScoreB = 0, bestScoreC = 0;

            // Find best match in B
            foreach (Endpoint epB in swaggers[swagger2])
            {
                if (usedB.Contains(epB.Name)) continue;

                double score = winky.Similarity(Normalize(epA.Name), Normalize(epB.Name));
                if (score > bestScoreB && score >= threshold)
                {
                    bestB = epB;
                    bestScoreB = score;
                }
            }
            if (bestB != null) usedB.Add(bestB.Name);

            // Find best match in C if 3 swaggers
            if (swaggers.Count == 3)
            {
                foreach (Endpoint epC in swaggers[swagger3])
                {
                    if (usedC.Contains(epC.Name)) continue;

                    double score = winky.Similarity(Normalize(epA.Name), Normalize(epC.Name));
                    if (score > bestScoreC && score >= threshold)
                    {
                        bestC = epC;
                        bestScoreC = score;
                    }
                }
                if (bestC != null) usedC.Add(bestC.Name);
            }

            EndpointMatch match = new()
            {
                A = epA,
                B = bestB,
                C = bestC,
                ScoreB = bestB != null ? bestScoreB : null,
                ScoreC = bestC != null ? bestScoreC : null
            };

            // Check if match: A must be not null, and at least one other is not null
            if (bestB != null || bestC != null)
            {
                matched.Add(match);
            }
            else
            {
                // A is not null, but no B or C match => NotMatched
                notMatched.Add(match);
            }

            logger.LogDebug("Created match: A={0}, B={1}, C={2}", match.A?.Path, match.B?.Path, match.C?.Path);
        }

        // Handle unmatched endpoints in B
        foreach (Endpoint sw2 in swaggers[swagger2].Where(b => !usedB.Contains(b.Name)))
        {
            Endpoint? bestCForB = null;
            double bestScoreCForB = 0;

            if (swaggers.Count == 3)
            {
                foreach (var epC in swaggers[swagger3].Where(c => !usedC.Contains(c.Name)))
                {
                    double score = winky.Similarity(Normalize(sw2.Name), Normalize(epC.Name));
                    if (score > bestScoreCForB && score >= threshold)
                    {
                        bestCForB = epC;
                        bestScoreCForB = score;
                    }
                }
            }

            if (bestCForB != null)
            {
                usedC.Add(bestCForB.Name);

                var match2and3 = new EndpointMatch
                {
                    A = null,
                    B = sw2,
                    C = bestCForB,
                    ScoreB = null,
                    ScoreC = bestScoreCForB
                };

                // This is a 2 & 3 only match
                matched2and3Only.Add(match2and3);
            }
            else
            {
                // No A, no C match => NotMatched
                notMatched.Add(new EndpointMatch
                {
                    A = null,
                    B = sw2,
                    C = null,
                    ScoreB = null
                });
            }
        }

        // Handle unmatched endpoints in C (when 3 swagger instances)
        if (swaggers.Count == 3)
        {
            foreach (Endpoint sw3 in swaggers[swagger3].Where(c => !usedC.Contains(c.Name)))
            {
                // No A or B match => NotMatched
                notMatched.Add(new EndpointMatch
                {
                    A = null,
                    B = null,
                    C = sw3,
                    ScoreC = null
                });
            }
        }

        logger.LogDebug("Swagger endopoits found:\n - Matched: {0}\n - Matched2And3: {1}\n - NotMatched: {2}", matched?.Count ?? 0, matched2and3Only?.Count ?? 0, notMatched?.Count ?? 0);
        return (matched, matched2and3Only, notMatched);
    }
    private async Task<(List<string> SwaggersName, List<EndpointMatch>? Matched, List<EndpointMatch>? Matched2and3Only, List<EndpointMatch>? NotMatched)> GatherInfo()
    {
        (List<List<Endpoint>> swaggers, List<string> swaggersName) = await _swagGet.GatherInfo(config.SwaggerDefinitions!);
        (List<EndpointMatch>? matched, List<EndpointMatch>? matched2and3Only, List<EndpointMatch>? notMatched) = MatchEndpointsV2(swaggers);

        return (swaggersName, matched, matched2and3Only, notMatched);
    }
    private string Normalize(string input)
    {
        return input.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");
    }
}
