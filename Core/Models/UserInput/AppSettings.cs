using SwagMatch.Core.Domain;

namespace SwagMatch.Core.Models.UserInput;
public sealed class AppSettings
{
    public int ApiTimeout { get; set; } = 500;
    public bool AutoClose { get; set; } = true;
    public string Path { get; set; } = string.Empty;

    // Mock Settings
    public MockConfig MockConfig { get; set; } = new();

    // Report Settings
    public Report Report { get; set; } = new();

    // App: Compare Swaggers
    public List<string>? CompareSwaggerUrls { get; set; }

    // App: Compare EndPoints
    public string SwaggerPath { get; set; } = string.Empty;
    public List<string[]>? CompareEndPointUrls { get; set; }

    public override string ToString()
    {
        return string.Format(
            "\n - {0}={1}\n - {2}={3}\n - {4}={5}\n - {6}:\n  * {7}\n - {8}:\n  * {9}\n  * {10}",
            nameof(Path),
            Path,
            nameof(ApiTimeout),
            ApiTimeout,
            nameof(AutoClose),
            AutoClose,
            nameof(CompareSwaggerUrls),
            CompareSwaggerUrls is { Count: > 0 } ? string.Join("\n  * ", CompareSwaggerUrls.Select((p, i) => p.ToString())) : "none",
            nameof(CompareEndPointUrls),
            CompareEndPointUrls is { Count: > 0 } ? string.Join("\n  * ", CompareEndPointUrls.Select((p, i) => p.ToString())) : "none",
            nameof(Report),
            Report?.ToString() ?? "null"
        );
    }
}
