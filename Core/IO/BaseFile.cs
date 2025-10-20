using System.Text;
using Microsoft.Extensions.Logging;
using SwagMatch.Core.Data.Models.SwaggerDocument;

namespace SwagMatch.Core.IO;
public class BaseFile(ILogger? logger, string filePath)
{
    #region Protected Methods
    protected async Task<string> ReadAsync(string fileName, string fileExtension)
    {
        string file = NormalizeFilePath(fileName, fileExtension);
        if(!ValidateFileExists(file)) return string.Empty;

        try
        {
            return await File.ReadAllTextAsync(file);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[ReadAsync] Failed to read file at {0}", file);
            return string.Empty;
        }
    }
    protected async Task<int> WriteAsync(string fileName, string fileExtension, string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            logger?.LogError("[WriteAsync] Won't save file {0}.{1}, reason: Content is null or empty.", fileName, fileExtension);
            return 0;
        }

        string file = NormalizeFilePath(fileName, fileExtension);
        try
        {
            using FileStream fileStream = new(file, FileMode.Create, FileAccess.Write);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            await fileStream.WriteAsync(bytes);
            return bytes.Length;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[WriteAsync] Failed to write file at {0}", file);
        }

        return 0;
    }
    #endregion

    #region Protected Helper Methods
    protected string Quote(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "\"-\"";

        input = input.Replace("\"", "\"\""); // Escape quotes
        return $"\"{input.Trim()}\"";
    }
    protected static string Escape(string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? "-"
            : input.Replace("|", "\\|").Replace("\n", "").Replace("\r", "").Trim();
    }
    protected string FormatParameters(List<Parameter>? parameters, bool markdown = true)
    {
        if (parameters == null || parameters.Count == 0)
            return "-";

        string separator = markdown ? "<br>" : " | ";

        return string.Join(separator, parameters.Select(p =>
        {
            string? name = markdown ? $"**{p.Name}**" : p.Name;
            string formatted = $"{name} ({p.Type}) [{p.In}]{(p.Required ? " (required)" : "")}";
            return CleanText(formatted, markdown);
        }));
    }
    protected string FormatRequestBody(List<Parameter>? requestBody, bool markdown = true)
    {
        if (requestBody == null || requestBody.Count == 0)
            return "-";

        string separator = markdown ? "<br>" : " | ";

        return string.Join(separator, requestBody.Select(p =>
        {
            string? name = markdown ? $"**{p.Name}**" : p.Name;
            string formatted = $"{name} ({p.Type}) [{p.In}]{(p.Required ? " (required)" : "")}";
            return CleanText(formatted, markdown);
        }));
    }
    protected string NormalizeFilePath(string fileName, string extension)
    {
        string file = string.Empty;

        Uri uri = new(fileName, UriKind.RelativeOrAbsolute);
        if (uri.IsAbsoluteUri)
        {
            file = uri.AbsolutePath;
        }
        else
        {
            file = fileName.ToLower().Contains(extension.ToLower()) ?
                Path.Combine(filePath, fileName) :
                Path.Combine(filePath, string.Format("{0}.{1}", fileName, extension));
        }

        return file;
    }
    protected bool ValidateFileExists(string file)
    {
        if (!File.Exists(file))
        {
            logger?.LogError("[ValidateFileExists] File does not exist at {0}", file);
            return false;
        }

        return true;
    }
    #endregion

    #region Private Helper Methods
    private static string CleanText(string input, bool markdown)
    {
        return markdown
            ? input.Replace("\r", "").Replace("\n", "<br>")
            : input.Replace("\r", "").Replace("\n", " | ");
    }
    #endregion
}