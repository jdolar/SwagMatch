namespace SwagMatch.Core.Data.Models.UserInput;
public sealed class UserInputPath
{
    public string Path { get; set; } = string.Empty;
    public bool GenerateMock { get; set; } = false;
    public override string ToString()
    {
        return string.Format(
            "{0}{1}",
            Path,
            GenerateMock ? string.Format(", {0}={1}", nameof(GenerateMock), GenerateMock) : string.Empty
        );
    }
}
