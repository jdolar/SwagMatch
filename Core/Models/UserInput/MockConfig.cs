namespace SwagMatch.Core.Models.UserInput;

public sealed class MockConfig
{
    public int AppCnt { get; set; } = 5;
    public int PathPerAppCnt { get; set; } = 10;
    public int OppPerPathPerAppCnt { get; set; } = 1;
    public int ReqBodyOrParamCnt { get; set; } = 6;
    public bool IsEnabled { get; set; }
    public int Type { get; set; } = 0;
}
