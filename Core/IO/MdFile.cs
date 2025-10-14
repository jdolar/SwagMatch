using Microsoft.Extensions.Logging;
using Core.Models;
using System.Text;
using SwagMatch.Core.Models.UserInput;
namespace Core.IO;
public sealed class MdFile(ILogger logger, string filePath) : BaseFile(logger, filePath)
{
    public string GenerateContent(List<EndpointMatch>? matched, List<EndpointMatch>? matched2and3Only, List<EndpointMatch>? notMatched, List<string> swaggersName, Report config)
    {
        if (!config.GenerateMd) return string.Empty;

        StringBuilder ret = new();
        if (matched?.Count > 0)
            ret.AppendLine(GenerateSimplifiedMarkdown(matched, swaggersName, config.IncludeName, config.IncludeMisc, config.GenerateVerticalLayout, "Matched", 1));

        if (matched2and3Only?.Count > 0)
            ret.AppendLine(GenerateSimplifiedMarkdown(matched2and3Only, swaggersName, config.IncludeName, config.IncludeMisc, config.GenerateVerticalLayout, "Matched in 2 and 3", 2));

        if (notMatched?.Count > 0)
            ret.AppendLine(GenerateSimplifiedMarkdown(notMatched, swaggersName, config.IncludeName, config.IncludeMisc, config.GenerateVerticalLayout, "Not matched", 3));

        return ret.ToString();
    }
    public string GenerateSimplifiedMarkdown(
    List<EndpointMatch>? matches,
    List<string> swaggersName,
    bool includeName,
    bool includeMisc,
    bool layoutVertical = false,
    string? groupTitle = null,
    int groupIndex = 1)
    {
        if (matches == null || matches.Count == 0)
            return string.Empty;

        StringBuilder sb = new();

        if (!string.IsNullOrWhiteSpace(groupTitle))
        {
            sb.AppendLine($"## {groupTitle}\n");
        }

        int matchIndex = 1;

        foreach (var match in matches)
        {
            var endpoints = match.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(Endpoint))
                .OrderBy(p => p.Name)
                .ToList();

            var endpointValues = endpoints
                .Select(p => p.GetValue(match) as Endpoint)
                .ToList();

            string matchName = endpointValues.FirstOrDefault(e => e != null)?.Name ?? "Unnamed";

            if (layoutVertical)
            {
                sb.AppendLine($"### Match {groupIndex}.{matchIndex++}: `{matchName}`\n");

                // Header
                sb.Append("| Source | Path");
                if (includeName) sb.Append(" | Name");
                sb.Append(" | Method | Parameters | Request Body");
                if (includeMisc) sb.Append(" | Misc");
                sb.AppendLine(" |");

                sb.Append("|--------|------");
                if (includeName) sb.Append("|------");
                sb.Append("|--------|------------|--------------");
                if (includeMisc) sb.Append("|------");
                sb.AppendLine("|");

                for (int i = 0; i < endpointValues.Count; i++)
                {
                    var ep = endpointValues[i];
                    string sourceName = swaggersName.ElementAtOrDefault(i) ?? $"Swagger_{i + 1}";

                    if (ep == null)
                    {
                        var empty = new List<string> { sourceName, "-" };
                        if (includeName) empty.Add("-");
                        empty.AddRange(["-", "-", "-"]);
                        if (includeMisc) empty.Add("-");
                        sb.AppendLine("| " + string.Join(" | ", empty) + " |");
                    }
                    else
                    {
                        string parameters = FormatParameters(ep.Parameters, true);
                        string requestBody = FormatRequestBody(ep.RequestBody, true);
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

                        var cells = new List<string>
                    {
                        sourceName,
                        Escape(ep.Path)
                    };
                        if (includeName) cells.Add(Escape(ep.Name));
                        cells.Add(Escape(ep.Method));
                        cells.Add(parameters);
                        cells.Add(requestBody);
                        if (includeMisc) cells.Add(Escape(miscParts.Trim()));

                        sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                    }
                }

                sb.AppendLine(); // space between match blocks
            }
            else
            {
                // Horizontal layout - one table for all rows
                if (matchIndex == 1 && !layoutVertical)
                {
                    foreach (var name in swaggersName)
                    {
                        sb.Append($"| {name}");
                        if (includeName) sb.Append(" | Name");
                        sb.Append(" | Method | Parameters | Request Body");
                        if (includeMisc) sb.Append(" | Misc");
                    }
                    sb.AppendLine("|");

                    foreach (var _ in swaggersName)
                    {
                        sb.Append("|------------------");
                        if (includeName) sb.Append("|------");
                        sb.Append("|--------|------------|--------------");
                        if (includeMisc) sb.Append("|------");
                    }
                    sb.AppendLine("|");
                }

                List<string> rowColumns = new();
                foreach (var ep in endpointValues)
                {
                    if (ep == null)
                    {
                        var emptyCells = new List<string> { "-" };
                        if (includeName) emptyCells.Add("-");
                        emptyCells.AddRange(["-", "-", "-"]);
                        if (includeMisc) emptyCells.Add("-");
                        rowColumns.Add(string.Join(" | ", emptyCells));
                    }
                    else
                    {
                        string parameters = FormatParameters(ep.Parameters, true);
                        string requestBody = FormatRequestBody(ep.RequestBody, true);
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

                        var values = new List<string> { Escape(ep.Path) };
                        if (includeName) values.Add(Escape(ep.Name));
                        values.Add(Escape(ep.Method));
                        values.Add(parameters);
                        values.Add(requestBody);
                        if (includeMisc) values.Add(Escape(miscParts.Trim()));

                        rowColumns.Add(string.Join(" | ", values));
                    }
                }

                while (rowColumns.Count < swaggersName.Count)
                {
                    var emptyCells = new List<string> { "-" };
                    if (includeName) emptyCells.Add("-");
                    emptyCells.AddRange(["-", "-", "-"]);
                    if (includeMisc) emptyCells.Add("-");
                    rowColumns.Add(string.Join(" | ", emptyCells));
                }

                sb.AppendLine("| " + string.Join(" | ", rowColumns) + " |");
                matchIndex++;
            }
        }

        return sb.ToString();
    }
    public async Task<int> WriteAsync(string fileName, string? content)
    {
        return await base.WriteAsync(fileName, "md", content);
    }
}
