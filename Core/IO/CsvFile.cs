using System.Text;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Data.Models.Match;
using SwagMatch.Core.Data.Models.UserInput;

namespace SwagMatch.Core.IO;
public sealed class CsvFile(ILogger logger, string filePath) : BaseFile(logger, filePath)
{
    private readonly ILogger _logger = logger;
    public string GenerateContent(
    List<EndpointMatch>? matched,
    List<EndpointMatch>? matched2and3Only,
    List<EndpointMatch>? notMatched,
    List<string> swaggersName,
    Report config)
    {
        if (!config.GenerateCsv) return string.Empty;

        StringBuilder sb = new();

        if (matched?.Count > 0)
            sb.AppendLine(GenerateSimplifiedCsv(matched, swaggersName, config.IncludeName, config.IncludeMisc, 1));
        if (matched2and3Only?.Count > 0)
            sb.AppendLine(GenerateSimplifiedCsv(matched2and3Only, swaggersName, config.IncludeName, config.IncludeMisc, 2));
        if (notMatched?.Count > 0)
            sb.AppendLine(GenerateSimplifiedCsv(notMatched, swaggersName, config.IncludeName, config.IncludeMisc, 3));

        return sb.ToString();
    }
    public string? GenerateSimplifiedCsv(
        List<EndpointMatch>? matches,
        List<string> swaggersName,
        bool includeName,
        bool includeMisc,
        int groupStartIndex = 1)
    {
        if (matches == null || matches.Count == 0)
            return string.Empty;

        StringBuilder sb = new();

        // Header row with GroupLabel as first column
        sb.Append("GroupLabel,");
        foreach (var name in swaggersName)
        {
            sb.Append($"{Quote(name)},");
            if (includeName) sb.Append("\"Name\",");
            sb.Append("\"Method\",\"Parameters\",\"Request Body\",");
            if (includeMisc) sb.Append("\"Misc\",");
        }
        if (sb.Length > 0)
            sb.Length--; // Remove trailing comma
        sb.AppendLine();

        int groupIndex = groupStartIndex;

        foreach (var match in matches)
        {
            string groupLabel = GetGroupLabel(match, groupIndex);
            groupIndex++;

            var endpoints = match.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(Endpoint))
                .OrderBy(p => p.Name)
                .ToList();

            List<string> rowColumns = new();

            // Local function for formatting an Endpoint to CSV columns
            string FormatEndpointCsv(Endpoint? ep)
            {
                if (ep == null)
                {
                    var emptyCells = new List<string> { Quote("-") };
                    if (includeName) emptyCells.Add(Quote("-"));
                    emptyCells.AddRange(new[] { Quote("-"), Quote("-"), Quote("-") });
                    if (includeMisc) emptyCells.Add(Quote("-"));
                    return string.Join(",", emptyCells);
                }

                string parameters = FormatParameters(ep.Parameters, false);
                string requestBody = FormatRequestBody(ep.RequestBody, false);
                string miscParts = "";

                if (includeMisc)
                {
                    if (!string.IsNullOrWhiteSpace(ep.Operation))
                        miscParts += $"[Operation={Escape(ep.Operation)}]";
                    if (!string.IsNullOrWhiteSpace(ep.Tags))
                        miscParts += $" [Tags={Escape(ep.Tags)}]";
                    if (!string.IsNullOrWhiteSpace(ep.Title))
                        miscParts += $" [Title={Escape(ep.Title)}]";
                }

                var values = new List<string> { Quote(ep.Path) };
                if (includeName) values.Add(Quote(ep.Name));
                values.AddRange(new[]
                {
                Quote(ep.Method),
                Quote(parameters),
                Quote(requestBody)
            });
                if (includeMisc) values.Add(Quote(miscParts.Trim()));

                return string.Join(",", values);
            }

            foreach (var epProperty in endpoints)
            {
                var ep = epProperty.GetValue(match) as Endpoint;
                rowColumns.Add(FormatEndpointCsv(ep));
            }

            while (rowColumns.Count < swaggersName.Count)
            {
                var emptyCells = new List<string> { Quote("-") };
                if (includeName) emptyCells.Add(Quote("-"));
                emptyCells.AddRange(new[] { Quote("-"), Quote("-"), Quote("-") });
                if (includeMisc) emptyCells.Add(Quote("-"));
                rowColumns.Add(string.Join(",", emptyCells));
            }

            sb.AppendLine($"{Quote(groupLabel)},{string.Join(",", rowColumns)}");
        }

        return sb.ToString();

        // Helper to get label for grouping
        string GetGroupLabel(EndpointMatch match, int idx)
        {
            var endpoints = match.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(Endpoint))
                .Select(p => p.GetValue(match) as Endpoint)
                .Where(ep => ep != null && !string.IsNullOrWhiteSpace(ep.Name))
                .ToList();

            string name = endpoints.FirstOrDefault()?.Name ?? "NoName";

            return $"Match {idx}: {name}";
        }
    }
    public async Task<int> WriteAsync(string fileName, string? content)
    {
        return await base.WriteAsync(fileName, "csv", content);
    }
}
