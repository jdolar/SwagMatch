namespace SwagMatch.Core.Data.Models.UserInput;
public sealed class Report
{
    public bool GenerateVerticalLayout { get; set; } = false;
    public bool GenerateMd { get; set; } = false;
    public bool GenerateCsv { get; set; } = false;
    public bool IncludeName { get; set; } = false;
    public bool IncludeMisc { get; set; } = false;
    public override string ToString()
    {
        return string.Format(
            "{0}={1}\n  * {2}={3}\n  * {4}={5}\n  * {6}={7}\n  * {8}={9}",
            nameof(GenerateVerticalLayout),
            GenerateVerticalLayout,
            nameof(GenerateMd),
            GenerateMd,
            nameof(GenerateCsv),
            GenerateCsv,
            nameof(IncludeName),
            IncludeName,
            nameof(IncludeMisc),
            IncludeMisc
        );
    }
}
