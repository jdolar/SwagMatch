namespace SwagMatch.Core.Models.UserInput;
public sealed class AppSettings
{
    public int ApiTimeout { get; set; } = 500;
    public bool AutoClose { get; set; } = true;
    public string Path { get; set; } = string.Empty;
    public List<UserInputPath>? SwaggerDefinitions { get; set; } = new();
    public Report Report { get; set; } = new();
    public override string ToString()
    {
        return string.Format(
            "\n - {0}={1}\n - {2}={3}\n - {4}={5}\n - {6}:\n  * {7}\n - {8}:\n  * {9}",
            nameof(Path),
            Path,
            nameof(ApiTimeout),
            ApiTimeout,
            nameof(AutoClose),
            AutoClose,
            nameof(SwaggerDefinitions),
            SwaggerDefinitions is { Count: > 0 } ? string.Join("\n  * ", SwaggerDefinitions.Select((p, i) => p.ToString())) : "none",
            nameof(Report),
            Report?.ToString() ?? "null"
        );
    }
}
