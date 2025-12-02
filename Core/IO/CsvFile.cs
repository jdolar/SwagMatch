using Core.IO;
using Core.Models;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Models.Swagger;
using SwagMatch.Core.Models.UserInput;
using System.Text;

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

        // --------------------
        // HEADER ROW
        // --------------------
        sb.Append("GroupLabel,");
        foreach (var name in swaggersName)
        {
            sb.Append($"{Quote(name)},");
            if (includeName) sb.Append("\"Name\",");
            sb.Append("\"Method\",\"Parameters\",\"Request Body\",\"Responses\",");
            if (includeMisc) sb.Append("\"Misc\",");
        }

        if (sb.Length > 0)
            sb.Length--; // remove last comma

        sb.AppendLine();

        // --------------------
        // ROWS
        // --------------------
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

            foreach (var epProp in endpoints)
            {
                var ep = epProp.GetValue(match) as Endpoint;
                rowColumns.Add(FormatEndpointCsv(ep, includeName, includeMisc));
            }

            // fill missing swagger slots
            while (rowColumns.Count < swaggersName.Count)
            {
                rowColumns.Add(FormatEmptyEndpoint(includeName, includeMisc));
            }

            sb.AppendLine($"{Quote(groupLabel)},{string.Join(",", rowColumns)}");
        }

        return sb.ToString();
    }

    // --------------------
    // FORMAT ONE ENDPOINT TO CSV
    // --------------------
    private string FormatEndpointCsv(Endpoint? ep, bool includeName, bool includeMisc)
    {
        if (ep == null)
            return FormatEmptyEndpoint(includeName, includeMisc);

        string parameters = FormatParameters(ep.Parameters, false);
        string requestBody = FormatRequestBody(ep.RequestBody, false);
        string responses = FormatResponsesCsv(ep.Responses);

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

        List<string> cells = new()
        {
            Quote(ep.Path)
        };

        if (includeName)
            cells.Add(Quote(ep.Name));

        cells.Add(Quote(ep.Method));
        cells.Add(Quote(parameters));
        cells.Add(Quote(requestBody));
        cells.Add(Quote(responses));

        if (includeMisc)
            cells.Add(Quote(miscParts.Trim()));

        return string.Join(",", cells);
    }

    // --------------------
    // FORMAT EMPTY ENDPOINT PLACEHOLDER
    // --------------------
    private string FormatEmptyEndpoint(bool includeName, bool includeMisc)
    {
        var empty = new List<string> { Quote("-") };
        if (includeName) empty.Add(Quote("-"));

        // Method, Parameters, RequestBody, Responses
        empty.AddRange(new[]
        {
            Quote("-"), Quote("-"), Quote("-"), Quote("-")
        });

        if (includeMisc) empty.Add(Quote("-"));

        return string.Join(",", empty);
    }

    // --------------------
    // FORMAT RESPONSES FOR CSV
    // --------------------
    private string FormatResponsesCsv(Dictionary<int, List<Parameter>>? responses)
    {
        if (responses == null || responses.Count == 0)
            return "-";

        List<string> pieces = new();

        foreach (var kv in responses.OrderBy(k => k.Key))
        {
            int status = kv.Key;
            var list = kv.Value;

            if (list == null || list.Count == 0)
            {
                pieces.Add($"{status}: -");
                continue;
            }

            var parameters = list.Select(p =>
                $"{Escape(p.Name)}({Escape(p.Type)}{(p.Required ? ",required" : "")})");

            pieces.Add($"{status}: {string.Join("|", parameters)}");
        }

        // Example output:
        // 200: id(int,required)|balance(decimal); 400: error(string); 422: validation(object)
        return string.Join("; ", pieces);
    }

    // --------------------
    // GROUP LABEL HELPER
    // --------------------
    private string GetGroupLabel(EndpointMatch match, int idx)
    {
        var endpoints = match.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(Endpoint))
            .Select(p => p.GetValue(match) as Endpoint)
            .Where(ep => ep != null && !string.IsNullOrWhiteSpace(ep.Name))
            .ToList();

        string name = endpoints.FirstOrDefault()?.Name ?? "NoName";
        return $"Match {idx}: {name}";
    }

    // --------------------
    // CSV QUOTE WRAPPER
    // --------------------
    private string Quote(string? value)
    {
        if (value == null) return "\"\"";
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public async Task<int> WriteAsync(string fileName, string? content)
    {
        return await base.WriteAsync(fileName, "csv", content);
    }
}
